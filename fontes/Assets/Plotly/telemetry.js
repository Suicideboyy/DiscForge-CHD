'use strict';

// Telemetry sparklines only: no conversion logic, no file access, no network.
// C# owns all values; this module owns traces, theme colors and resize.

const MAX_POINTS = 90;
const RESIZE_DEBOUNCE_MS = 150;

const FallbackColors = {
  accent: '#A78BFA',
  amber: '#FBBF24',
  teal: '#5EEAD4',
  ink: '#E2E8F0',
  muted: '#94A3B8',
  surface: '#1E1C35'
};

let colors = { ...FallbackColors };
let highContrast = false;
let plotted = false;
let resizeTimer = 0;

function num(value) {
  return typeof value === 'number' && Number.isFinite(value) ? value : null;
}

function sparkLayout(yRange) {
  const layout = {
    margin: { l: 4, r: 4, t: 4, b: 4 },
    paper_bgcolor: 'rgba(0,0,0,0)',
    plot_bgcolor: 'rgba(0,0,0,0)',
    showlegend: false,
    xaxis: { visible: false, showgrid: false, zeroline: false, fixedrange: true },
    yaxis: { visible: false, showgrid: false, zeroline: false, fixedrange: true }
  };
  if (yRange) layout.yaxis.range = yRange;
  return layout;
}

function sparkConfig() {
  return { displayModeBar: false, responsive: true, staticPlot: true, doubleClick: false };
}

function trace(color, fill) {
  const t = { y: [], type: 'scatter', mode: 'lines', hoverinfo: 'skip', line: { width: 1.5, color } };
  if (fill) t.fill = 'tozeroy', t.fillcolor = fill;
  return t;
}

function translucent(hex, alpha) {
  const m = /^#([0-9a-fA-F]{6})$/.exec(hex || '');
  if (!m) return hex;
  const v = parseInt(m[1], 16);
  return 'rgba(' + ((v >> 16) & 255) + ',' + ((v >> 8) & 255) + ',' + (v & 255) + ',' + alpha + ')';
}

function diskColors() {
  return highContrast ? [colors.ink, colors.ink] : [colors.amber, colors.muted];
}

function paintBackground() {
  try { document.body.style.background = colors.surface; } catch (e) { /* keep CSS fallback */ }
}

function initPlots() {
  if (plotted || typeof Plotly === 'undefined') return;
  paintBackground();
  const cpuLine = highContrast ? colors.ink : colors.accent;
  const [readLine, writeLine] = diskColors();
  const stageLine = highContrast ? colors.ink : colors.teal;
  Plotly.newPlot('cpu-chart', [trace(cpuLine, translucent(cpuLine, 0.18))], sparkLayout([0, 100]), sparkConfig());
  const read = trace(readLine, translucent(readLine, 0.15));
  const write = trace(writeLine, null);
  write.line.dash = 'dot';
  Plotly.newPlot('disk-chart', [read, write], sparkLayout(null), sparkConfig());
  Plotly.newPlot('stage-chart', [trace(stageLine, translucent(stageLine, 0.15))], sparkLayout([0, 100]), sparkConfig());
  plotted = true;
}

function applyTheme(msg) {
  if (typeof msg.Accent === 'string') colors.accent = msg.Accent;
  if (typeof msg.Amber === 'string') colors.amber = msg.Amber;
  if (typeof msg.Teal === 'string') colors.teal = msg.Teal;
  if (typeof msg.Ink === 'string') colors.ink = msg.Ink;
  if (typeof msg.Muted === 'string') colors.muted = msg.Muted;
  if (typeof msg.Surface === 'string') colors.surface = msg.Surface;
  highContrast = msg.HighContrast === true;
  paintBackground();
  if (!plotted) return;
  try {
    const cpuLine = highContrast ? colors.ink : colors.accent;
    Plotly.restyle('cpu-chart', { 'line.color': cpuLine, fillcolor: translucent(cpuLine, 0.18) }, [0]);
    const [readLine, writeLine] = diskColors();
    Plotly.restyle('disk-chart', { 'line.color': [readLine, writeLine], fillcolor: [translucent(readLine, 0.15), 'rgba(0,0,0,0)'] }, [0, 1]);
    const stageLine = highContrast ? colors.ink : colors.teal;
    Plotly.restyle('stage-chart', { 'line.color': stageLine, fillcolor: translucent(stageLine, 0.15) }, [0]);
  } catch (e) { /* keep previous colors on transient failure */ }
}

function pushSample(msg) {
  if (!plotted) initPlots();
  if (!plotted) return;
  try {
    Plotly.extendTraces('cpu-chart', { y: [[num(msg.CpuPercent)]] }, [0], MAX_POINTS);
    Plotly.extendTraces('disk-chart', { y: [[num(msg.ReadMiBps)], [num(msg.WriteMiBps)]] }, [0, 1], MAX_POINTS);
    Plotly.extendTraces('stage-chart', { y: [[num(msg.StagePercent)]] }, [0], MAX_POINTS);
  } catch (e) { /* drop one point rather than breaking the timer */ }
}

function onMessage(event) {
  const msg = event && event.data;
  if (!msg || typeof msg !== 'object' || typeof msg.Type !== 'string') return;
  if (msg.Type === 'theme') applyTheme(msg);
  else if (msg.Type === 'sample') pushSample(msg);
}

function scheduleResize() {
  if (resizeTimer) clearTimeout(resizeTimer);
  resizeTimer = setTimeout(() => {
    resizeTimer = 0;
    if (!plotted) return;
    try {
      Plotly.Plots.resize('cpu-chart');
      Plotly.Plots.resize('disk-chart');
      Plotly.Plots.resize('stage-chart');
    } catch (e) { /* ignore resize races during navigation */ }
  }, RESIZE_DEBOUNCE_MS);
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initPlots);
} else {
  initPlots();
}

if (window.chrome && window.chrome.webview) {
  window.chrome.webview.addEventListener('message', onMessage);
}

if (typeof ResizeObserver !== 'undefined') {
  new ResizeObserver(scheduleResize).observe(document.getElementById('telemetry-root'));
} else {
  window.addEventListener('resize', scheduleResize);
}
