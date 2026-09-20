using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Web.Script.Serialization;

[assembly: AssemblyVersion("1.2.0.0")]
[assembly: AssemblyFileVersion("1.2.0.0")]
[assembly: AssemblyTitle("CHD Optimizer")]

static class Covers {
    public static async Task<Image> Load(string serial){
        if(!Regex.IsMatch(serial??"",@"^[A-Z]{4}-\d{5}$"))return null;
        return await Task.Run(()=>{
            string root=Environment.GetEnvironmentVariable("CHDOPT_TEST_CACHE");if(String.IsNullOrEmpty(root))root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CHD Optimizer");
            string folder=Path.Combine(root,"covers"),file=Path.Combine(folder,serial+".jpg");
            try{
                byte[] data=null;
                if(File.Exists(file))try{data=File.ReadAllBytes(file);using(var ms=new MemoryStream(data))using(var img=Image.FromStream(ms))return (Image)new Bitmap(img);}catch{data=null;}
                ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;
                var request=(HttpWebRequest)WebRequest.Create("https://raw.githubusercontent.com/xlenore/ps2-covers/main/covers/default/"+serial+".jpg");
                request.Timeout=12000;request.ReadWriteTimeout=12000;request.UserAgent="CHD-Optimizer/1.2.0";
                using(var response=request.GetResponse())using(var stream=response.GetResponseStream())using(var ms=new MemoryStream()){
                    byte[] buffer=new byte[8192];int n;while((n=stream.Read(buffer,0,buffer.Length))>0){ms.Write(buffer,0,n);if(ms.Length>10000000)throw new IOException("Capa muito grande");}data=ms.ToArray();
                }
                using(var ms=new MemoryStream(data))using(var img=Image.FromStream(ms)){
                    var copy=new Bitmap(img);try{Directory.CreateDirectory(folder);File.WriteAllBytes(file,data);}catch{}return (Image)copy;
                }
            }catch{return null;}
        }).ConfigureAwait(false);
    }
}

class Settings {
    public string Input, Output, Platform="PS2", Cd="cdlz,cdzs,cdzl,cdfl", Dvd="lzma,zstd,zlib,flac";
    public int CdHunk=2448, DvdHunk=2048, Threads=Math.Max(1,Environment.ProcessorCount-2);
    public bool Delete=false, Online=true;
    public void Validate() {
        Input=Path.GetFullPath(Input).TrimEnd('\\'); Output=Path.GetFullPath(Output).TrimEnd('\\');
        if(!Directory.Exists(Input)) throw new Exception("Escolha uma pasta de entrada existente.");
        if(Input.Length<3 || Output.Length<3) throw new Exception("Use uma pasta, não a raiz de uma unidade.");
        if(Input.Equals(Output,StringComparison.OrdinalIgnoreCase) || Input.StartsWith(Output+"\\",StringComparison.OrdinalIgnoreCase)) throw new Exception("A saída deve ser diferente da entrada e não pode conter a pasta de entrada.");
        string temp=Input+"\\temp";
        if(Output.Equals(temp,StringComparison.OrdinalIgnoreCase)||Output.StartsWith(temp+"\\",StringComparison.OrdinalIgnoreCase)) throw new Exception("A saída não pode ficar na pasta temporária da entrada.");
        foreach(string path in new[]{Input,Output}) for(DirectoryInfo d=new DirectoryInfo(path);d!=null;d=d.Parent)
            if(d.Exists && (d.Attributes & FileAttributes.ReparsePoint)!=0) throw new Exception("Escolha pastas sem links ou junctions.");
        if(Platform!="PS1" && Platform!="PS2") throw new Exception("Plataforma inválida.");
        CheckCodecs(Cd,new[]{"cdlz","cdzs","cdzl","cdfl"}); CheckCodecs(Dvd,new[]{"lzma","zstd","zlib","flac","huff"});
        if(CdHunk<2448 || CdHunk>1048576 || CdHunk%2448!=0 || DvdHunk<2048 || DvdHunk>1048576 || DvdHunk%2048!=0) throw new Exception("Hunk inválido para a mídia.");
        if(Threads<1 || Threads>Environment.ProcessorCount) throw new Exception("Quantidade de threads inválida.");
    }
    static void CheckCodecs(string codecs,string[] allowed){var set=new HashSet<string>();foreach(string c in codecs.Split(','))if(Array.IndexOf(allowed,c)<0||!set.Add(c))throw new Exception("Selecione codecs válidos, sem repetição.");if(set.Count>4)throw new Exception("Selecione no máximo quatro codecs por mídia.");}
}
class MainForm:Form {
    TabControl tabs=new TabControl(); PictureBox cover=new PictureBox();Label coverStatus=new Label();TextBox gameInfo=new TextBox();
    int coverGeneration;string coverSerial="";
    TextBox input=new TextBox(),output=new TextBox(),log=new TextBox();
    ComboBox platform=new ComboBox(),cdh=new ComboBox(),dvdh=new ComboBox();
    CheckedListBox cd=new CheckedListBox(),dvd=new CheckedListBox();
    NumericUpDown threads=new NumericUpDown(); CheckBox delete=new CheckBox(),online=new CheckBox();
    Button start=new Button(),stop=new Button();ProgressBar batch=new ProgressBar(),stage=new ProgressBar();
    Label batchText=new Label(),stageText=new Label(),hint=new Label();TableLayoutPanel options;
    bool running;string activity="Aguardando";
    public MainForm(){
        Text="CHD Optimizer 1.2.0 • PS1 e PS2";Font=new Font("Segoe UI",10);ClientSize=new Size(1190,780);MinimumSize=new Size(1110,760);StartPosition=FormStartPosition.CenterScreen;BackColor=Color.FromArgb(246,248,251);
        tabs.Dock=DockStyle.Fill;Controls.Add(tabs);var conversion=new TabPage("Conversão");tabs.TabPages.Add(conversion);
        var columns=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));columns.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,285));conversion.Controls.Add(columns);
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(16),ColumnCount=1,RowCount=9};columns.Controls.Add(layout,0,0);
        var game=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(8,20,12,12),RowCount=4,ColumnCount=1};game.RowStyles.Add(new RowStyle(SizeType.Absolute,32));game.RowStyles.Add(new RowStyle(SizeType.Absolute,280));game.RowStyles.Add(new RowStyle(SizeType.Absolute,38));game.RowStyles.Add(new RowStyle(SizeType.Percent,100));columns.Controls.Add(game,1,0);
        game.Controls.Add(new Label{Text="Jogo atual",AutoSize=true,Font=new Font("Segoe UI",15,FontStyle.Bold)});
        cover.Dock=DockStyle.Fill;cover.SizeMode=PictureBoxSizeMode.Zoom;cover.BackColor=Color.WhiteSmoke;game.Controls.Add(cover);
        coverStatus.Dock=DockStyle.Fill;coverStatus.Text="Aguardando identificação";game.Controls.Add(coverStatus);
        gameInfo.Multiline=true;gameInfo.ReadOnly=true;gameInfo.Dock=DockStyle.Fill;gameInfo.ScrollBars=ScrollBars.Vertical;gameInfo.BackColor=Color.White;gameInfo.Text="Os dados consultados aparecerão aqui.";game.Controls.Add(gameInfo);
        BuildAbout();
        foreach(int h in new[]{48,40,40,250,28,48,48,48})layout.RowStyles.Add(new RowStyle(SizeType.Absolute,h));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        layout.Controls.Add(new Label{Text="CHD Optimizer",Font=new Font("Segoe UI",22,FontStyle.Bold),AutoSize=true});
        layout.Controls.Add(PathRow("Entrada",input));layout.Controls.Add(PathRow("Saída",output));
        options=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,RowCount=5};layout.Controls.Add(options);
        foreach(int w in new[]{140,270,145,260})options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,w));
        options.RowStyles.Add(new RowStyle(SizeType.Absolute,36));options.RowStyles.Add(new RowStyle(SizeType.Absolute,36));options.RowStyles.Add(new RowStyle(SizeType.Absolute,125));options.RowStyles.Add(new RowStyle(SizeType.Absolute,30));
        platform.DropDownStyle=ComboBoxStyle.DropDownList;platform.Items.AddRange(new object[]{"PS1","PS2"});platform.SelectedIndex=1;
        threads.Minimum=1;threads.Maximum=Environment.ProcessorCount;threads.Value=Math.Max(1,Environment.ProcessorCount-2);
        AddOption("Plataforma",platform,0,0);AddOption("Threads",threads,2,0);
        cdh.DropDownStyle=dvdh.DropDownStyle=ComboBoxStyle.DropDownList;
        cdh.Items.AddRange(new object[]{"2448","19584","78336","1047744"});dvdh.Items.AddRange(new object[]{"2048","4096","32768","262144","1048576"});cdh.SelectedIndex=dvdh.SelectedIndex=0;
        AddOption("Hunk CD (bytes)",cdh,0,1);AddOption("Hunk DVD (bytes)",dvdh,2,1);
        cd.CheckOnClick=dvd.CheckOnClick=true;foreach(string c in new[]{"cdlz","cdzs","cdzl","cdfl"})cd.Items.Add(c,true);foreach(string c in new[]{"lzma","zstd","zlib","flac","huff"})dvd.Items.Add(c,c!="huff");
        AddOption("Codecs de CD",cd,0,2);AddOption("Codecs de DVD",dvd,2,2);
        online.Text="Consultar serial e nome (PS2)";online.Checked=true;online.AutoSize=true;delete.Text="Apagar compactado após sucesso (PS2)";delete.AutoSize=true;delete.Checked=true;
        options.Controls.Add(online,0,3);options.SetColumnSpan(online,2);options.Controls.Add(delete,2,3);options.SetColumnSpan(delete,2);
        hint.AutoSize=true;hint.Text="PS2: compactados, ISO, BIN/CUE e CHD. Até 4 codecs por mídia.";layout.Controls.Add(hint);
        layout.Controls.Add(ProgressRow(batchText,batch,"Lote: 0%"));layout.Controls.Add(ProgressRow(stageText,stage,"Etapa: aguardando"));
        var actions=new FlowLayoutPanel{Dock=DockStyle.Fill};start.Text="Iniciar";start.Width=130;start.Height=34;stop.Text="Parar após atual";stop.Width=170;stop.Height=34;stop.Enabled=false;
        var open=new Button{Text="Abrir saída",Width=120,Height=34};open.Click+=delegate{if(Directory.Exists(output.Text))Process.Start("explorer.exe",output.Text);};actions.Controls.AddRange(new Control[]{start,stop,open});layout.Controls.Add(actions);
        log.Multiline=true;log.ReadOnly=true;log.ScrollBars=ScrollBars.Vertical;log.Dock=DockStyle.Fill;log.BackColor=Color.White;layout.Controls.Add(log);
        start.Click+=async delegate{await Start();};stop.Click+=delegate{if(Engine.StopFile!=null){File.WriteAllText(Engine.StopFile,"");stop.Enabled=false;Append("Parada solicitada: a entrada atual será concluída antes de parar.");}};
        platform.SelectedIndexChanged+=delegate{bool ps2=platform.Text=="PS2";dvd.Enabled=dvdh.Enabled=online.Enabled=delete.Enabled=ps2;hint.Text=ps2?"PS2: compactados, ISO, BIN/CUE e CHD. Até 4 codecs por mídia.":"PS1: otimiza arquivos CHD na pasta de entrada, conforme o BAT original.";};
        FormClosing+=delegate(object sender,FormClosingEventArgs e){if(running){e.Cancel=true;MessageBox.Show(this,"Use ‘Parar após atual’ e aguarde a conclusão para fechar.");}};
    }
    void BuildAbout(){
        var page=new TabPage("Sobre");tabs.TabPages.Add(page);var sub=new TabControl{Dock=DockStyle.Fill};page.Controls.Add(sub);
        var info=new TabPage("Informações");var changes=new TabPage("Changelog");sub.TabPages.Add(info);sub.TabPages.Add(changes);
        info.Controls.Add(new TextBox{Multiline=true,ReadOnly=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Vertical,Font=new Font("Segoe UI",12),Text=
            "CHD Optimizer 1.2.0\r\n\r\nCompilação do aplicativo: "+BuildInfo.Date+"\r\nArquitetura: Windows 64 bits\r\nLinguagem da interface: C# / Windows Forms / .NET Framework\r\nMotor: C# nativo / .NET Framework\r\n\r\nCHDman incluído: MAME 0.289 (unknown)\r\nCompilação do CHDman: 14/09/2026\r\nLinguagem do CHDman: C++20\r\nGCC 16.2 / Zen 3 / LTO / LZMA aprimorado\r\nA data e versão acima referem-se ao binário incluído; não houve recompilação do CHDman nesta atualização.\r\n\r\nCapas PS2: xlenore/ps2-covers (GitHub)\r\nCache: pasta covers em %LOCALAPPDATA%\\CHD Optimizer\r\nA falta de capa ou de conexão não interrompe o processamento.\r\n\r\nComponentes: chdman/MAME e 7-Zip, dos respectivos autores."});
        changes.Controls.Add(new TextBox{Multiline=true,ReadOnly=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Vertical,Font=new Font("Segoe UI",12),Text=
            "1.2.0 — 20/09/2026\r\n\r\n• Motor convertido integralmente para C#; sem BAT, CMD ou PowerShell durante a conversão.\r\n• Execução direta do chdman e 7-Zip, verificação antes de publicar e remoção segura do compactado.\r\n• Preservados painel, capas, consultas, retomada, codecs e seleção de plataforma.\r\n\r\n1.1.0 — "+BuildInfo.Date+"\r\n\r\n• Painel do jogo: título, serial, mídia, identificação, consulta, origem e status.\r\n• Download de capas PS2 por serial, com cache e atualização sem bloquear a interface.\r\n• BINs numerados como Track são faixas do mesmo disco, não discos distintos.\r\n• Faixas sem CUE são bloqueadas com uma mensagem clara; compactados são preservados.\r\n• CHD parcial antigo não autoriza ignorar um conjunto de faixas sem CUE.\r\n• Aba Sobre com versões, datas, linguagens e este changelog.\r\n\r\n1.0.0 — 20/09/2026\r\n\r\n• Aplicativo portátil com BAT, chdman e 7-Zip incluídos.\r\n• Seleção de pastas, plataforma, codecs, hunk e threads.\r\n• Progresso por lote e etapa; retomada e parada após a entrada atual."});
    }
    async void UpdateGame(Dictionary<string,object> data){
        Func<string,string> get=key=>data.ContainsKey(key)&&data[key]!=null?Convert.ToString(data[key]):"-";
        gameInfo.Text=get("Title")+"\r\n\r\nSerial: "+get("Serial")+"\r\nMídia: "+get("Type")+"\r\nStatus: "+get("Status")+"\r\n\r\nIdentificação: "+get("Detection")+"\r\nConsulta: "+get("Lookup")+"\r\nFonte: "+get("DatabaseUrl")+"\r\n\r\nEntrada: "+get("Source")+"\r\nImagem: "+get("Image")+"\r\nDetalhe: "+get("Detail");
        string serial=platform.Text=="PS2"?get("Serial"):"";if(serial==coverSerial)return;coverSerial=serial;int generation=++coverGeneration;
        var previous=cover.Image;cover.Image=null;if(previous!=null)previous.Dispose();
        if(!Regex.IsMatch(serial,@"^[A-Z]{4}-\d{5}$")){coverStatus.Text="Capa indisponível: sem serial PS2";return;}
        coverStatus.Text="Carregando capa…";Image image=await Covers.Load(serial);
        if(IsDisposed||generation!=coverGeneration){if(image!=null)image.Dispose();return;}
        cover.Image=image;coverStatus.Text=image==null?"Capa indisponível (rede ou base)":"Capa: "+serial+" • xlenore";
    }
    Control PathRow(string title,TextBox box){var row=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3};row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,85));row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,105));row.Controls.Add(new Label{Text=title,AutoSize=true,Padding=new Padding(0,5,0,0)});box.Dock=DockStyle.Fill;row.Controls.Add(box);var b=new Button{Text="Escolher…",Dock=DockStyle.Fill};b.Click+=delegate{if(running)return;using(var dialog=new FolderBrowserDialog{Description="Selecione a pasta de "+title.ToLower(),SelectedPath=box.Text})if(dialog.ShowDialog(this)==DialogResult.OK){box.Text=dialog.SelectedPath;if(box==input && output.Text.Length==0)output.Text=Path.Combine(box.Text,"otimizados");}};row.Controls.Add(b);return row;}
    void AddOption(string title,Control control,int x,int y){options.Controls.Add(new Label{Text=title,AutoSize=true,Padding=new Padding(0,4,0,0)},x,y);control.Dock=DockStyle.Fill;options.Controls.Add(control,x+1,y);}
    Control ProgressRow(Label text,ProgressBar bar,string initial){var p=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=2};text.Text=initial;text.AutoSize=true;bar.Dock=DockStyle.Fill;p.Controls.Add(text);p.Controls.Add(bar);return p;}
    static string Codecs(CheckedListBox list){var names=new List<string>();foreach(var item in list.CheckedItems)names.Add(item.ToString());return String.Join(",",names);}
    void Append(string text){if(log.TextLength>100000)log.Text=log.Text.Substring(log.TextLength-70000);log.AppendText(text+Environment.NewLine);}
    void Report(string line){if(IsDisposed)return;BeginInvoke(new Action(delegate{
        if(line.StartsWith("GUI_GAME:")){try{var json=Encoding.UTF8.GetString(Convert.FromBase64String(line.Substring(9)));UpdateGame(new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(json));}catch{Append("Não foi possível exibir os dados desta entrada.");}return;}
        var total=Regex.Match(line,@"Lote PS[12]:\s*([\d,.]+)%.*\((\d+)/(\d+)");
        var ps1=Regex.Match(line,@"GUI_ITEM=(\d+)/(\d+)");
        if(total.Success){batch.Value=Math.Min(100,(int)(100.0*Int32.Parse(total.Groups[2].Value)/Math.Max(1,Int32.Parse(total.Groups[3].Value))));batchText.Text=line;return;}
        if(ps1.Success){batch.Value=Math.Min(100,(int)(100.0*Int32.Parse(ps1.Groups[1].Value)/Math.Max(1,Int32.Parse(ps1.Groups[2].Value))));batchText.Text="Lote: "+batch.Value+"%";return;}
        var progress=Regex.Match(line,@"(?:Etapa:\s*|(?:Compressing|Extracting|Verifying),\s*)(\d+(?:[.,]\d+)?)%");
        if(progress.Success){double p=Double.Parse(progress.Groups[1].Value.Replace(',','.'),System.Globalization.CultureInfo.InvariantCulture);stage.Value=Math.Max(0,Math.Min(100,(int)p));stageText.Text=activity+" — "+p.ToString("N1")+"%";return;}
        if(line.StartsWith("Comprimindo")||line.StartsWith("Descompactando")||line.StartsWith("Extraindo")||line.StartsWith("Verificando")){activity=line;stage.Value=0;stageText.Text=line;}
        Append(line);
    }));}
    async Task Start(){
        try{
            var s=new Settings{Input=input.Text,Output=output.Text,Platform=platform.Text,Cd=Codecs(cd),Dvd=Codecs(dvd),CdHunk=Int32.Parse(cdh.Text),DvdHunk=Int32.Parse(dvdh.Text),Threads=(int)threads.Value,Delete=delete.Checked,Online=online.Checked};s.Validate();
            running=true;options.Enabled=input.Enabled=output.Enabled=start.Enabled=false;stop.Enabled=true;batch.Value=stage.Value=0;log.Clear();Append("Preparando ferramentas incluídas…");
            int code=await Task.Run(()=>Engine.Run(s,Report));
            if(code==0){batch.Value=100;batchText.Text="Lote: 100% concluído";Append("Concluído. Consulte os detalhes acima e o log em "+Path.Combine(s.Input,"temp","log.txt"));}
            else Append(code==2?"Interrompido após concluir a entrada atual.":"Execução terminou com erros. Consulte o log acima.");
        }catch(Exception ex){Append("ERRO: "+ex.Message);MessageBox.Show(this,ex.Message,"CHD Optimizer",MessageBoxButtons.OK,MessageBoxIcon.Error);}
        finally{running=false;options.Enabled=input.Enabled=output.Enabled=start.Enabled=true;stop.Enabled=false;}
    }
    [STAThread] static int Main(string[] args){
        try{
            var inherited=Environment.GetEnvironmentVariables();var canonical=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            foreach(string key in inherited.Keys){if(canonical.ContainsKey(key)){string value=canonical[key];Environment.SetEnvironmentVariable(key,null);Environment.SetEnvironmentVariable(key,value);}else canonical[key]=(string)inherited[key];}
            if(args.Length==3 && args[0]=="--cover-test"){using(var image=Covers.Load(args[1]).GetAwaiter().GetResult()){if(image==null)return 3;image.Save(args[2]);}return 0;}
            if(args.Length==4 && (args[0]=="--run-test"||args[0]=="--stop-test"||args[0]=="--delete-test")){
                var s=new Settings{Input=args[1],Output=args[2],Platform=args[3],Online=false,Delete=args[0]=="--delete-test",Threads=Math.Min(4,Environment.ProcessorCount)};
                var lines=new List<string>();int code=Engine.Run(s,delegate(string t){lock(lines)lines.Add(t);if(args[0]=="--stop-test" && t.StartsWith("Tipo:") && Engine.StopFile!=null)File.WriteAllText(Engine.StopFile,"");}).GetAwaiter().GetResult();File.WriteAllLines(Path.Combine(s.Input,"app-test.log"),lines,Encoding.UTF8);return code;
            }
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            if((args.Length==2||args.Length==3) && args[0]=="--ui-test"){using(var f=new MainForm()){f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-10000,-10000);f.ShowInTaskbar=false;f.Show();Application.DoEvents();
                if(args.Length==3){if(args[2]=="game"){f.UpdateGame(new Dictionary<string,object>{{"Title","Dance Factory"},{"Serial","SLUS-21296"},{"Type","CD"},{"Status","CUE AUSENTE"},{"Detection","Seis faixas BIN; sem descritor CUE"},{"Source","Dance Factory (USA).7z"},{"Detail","Prévia do painel — CUE original necessário."}});var watch=Stopwatch.StartNew();while(f.coverStatus.Text.StartsWith("Carregando") && watch.ElapsedMilliseconds<15000){Application.DoEvents();Thread.Sleep(30);}}else{f.tabs.SelectedIndex=1;if(args[2]=="changelog")((TabControl)f.tabs.TabPages[1].Controls[0]).SelectedIndex=1;}Application.DoEvents();}
                using(var bitmap=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(bitmap,new Rectangle(0,0,f.Width,f.Height));bitmap.Save(args[1]);}f.Hide();}return 0;}
            bool created;using(var mutex=new Mutex(true,"Local\\CHDOptimizerDesktop",out created)){if(!created){MessageBox.Show("O CHD Optimizer já está aberto.");return 1;}Application.Run(new MainForm());}return 0;
        }catch(Exception ex){if(args.Length>0){File.WriteAllText(Path.Combine(Path.GetTempPath(),"chd-optimizer-error.txt"),ex.ToString());return 1;}MessageBox.Show(ex.Message,"CHD Optimizer");return 1;}
    }
}
