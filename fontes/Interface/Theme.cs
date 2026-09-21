using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

static class Theme
{
    public static readonly Color Background = Color.FromArgb(237, 241, 249);
    public static readonly Color Ink = Color.FromArgb(32, 43, 70);
    public static readonly Color Muted = Color.FromArgb(97, 111, 140);
    public static readonly Color Purple = Color.FromArgb(111, 76, 235);
    public static readonly Color Teal = Color.FromArgb(0, 160, 156);
    public static readonly Color Navy = Color.FromArgb(29, 37, 68);

    public static GraphicsPath Shape(Rectangle bounds, int radius)
    {
        int diameter = Math.Max(1, Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height)));
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void Round(Control control, int radius)
    {
        if (control.Width < 2 || control.Height < 2)
        {
            return;
        }
        using (var path = Shape(control.ClientRectangle, radius))
        {
            Region previous = control.Region;
            control.Region = new Region(path);
            if (previous != null)
            {
                previous.Dispose();
            }
        }
    }

    public static void StyleInputs(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            if (control is TextBox || control is ComboBox || control is NumericUpDown
                || control is CheckedListBox)
            {
                control.BackColor = Color.FromArgb(247, 249, 253);
                control.ForeColor = Ink;
            }
            StyleInputs(control);
        }
    }
}

class RoundedPanel : Panel
{
    public RoundedPanel()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
        Padding = new Padding(16);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Theme.Round(this, 18);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent == null ? Theme.Background : Parent.BackColor);
        if (Width < 2 || Height < 2)
        {
            return;
        }
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using (var path = Theme.Shape(new Rectangle(0, 0, Width - 1, Height - 1), 18))
        using (var brush = new SolidBrush(BackColor))
        {
            e.Graphics.FillPath(brush, path);
        }
    }
}

class RoundedButton : Button
{
    public RoundedButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Theme.Purple;
        ForeColor = Color.White;
        UseVisualStyleBackColor = false;
        Cursor = Cursors.Hand;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Theme.Round(this, 11);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent == null ? Theme.Background : Parent.BackColor);
        if (Width < 2 || Height < 2)
        {
            return;
        }
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using (var path = Theme.Shape(new Rectangle(0, 0, Width - 1, Height - 1), 11))
        using (var brush = new SolidBrush(Enabled ? BackColor : Color.FromArgb(174, 182, 202)))
        {
            e.Graphics.FillPath(brush, path);
        }
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        if (Focused && ShowFocusCues)
        {
            var bounds = ClientRectangle;
            bounds.Inflate(-5, -5);
            ControlPaint.DrawFocusRectangle(e.Graphics, bounds);
        }
    }
}

class RoundedProgress : Control
{
    int value;
    public Color FillColor = Theme.Purple;

    public int Value
    {
        get { return value; }
        set
        {
            this.value = Math.Max(0, Math.Min(100, value));
            Invalidate();
        }
    }

    public RoundedProgress()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
        Height = 12;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Width < 2 || Height < 2)
        {
            return;
        }
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(0, 0, Width - 1, Math.Min(12, Height - 1));
        using (var track = Theme.Shape(bounds, 6))
        using (var brush = new SolidBrush(Theme.Background))
        {
            e.Graphics.FillPath(brush, track);
        }
        bounds.Width = (int)(bounds.Width * value / 100.0);
        if (bounds.Width < 2)
        {
            return;
        }
        using (var fill = Theme.Shape(bounds, 6))
        using (var brush = new SolidBrush(FillColor))
        {
            e.Graphics.FillPath(brush, fill);
        }
    }
}
