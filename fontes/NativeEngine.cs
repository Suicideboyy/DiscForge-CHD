using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Net;
using System.Web.Script.Serialization;

// Native processing engine. Only chdman and 7-Zip are started as child processes.
static class Engine {
    public static string Tools, StopFile;
    public static string Stage() {
        var asm=Assembly.GetExecutingAssembly();string id;
        using(var f=File.OpenRead(asm.Location))using(var sha=SHA256.Create())id=BitConverter.ToString(sha.ComputeHash(f)).Replace("-","").Substring(0,16);
        string root=Environment.GetEnvironmentVariable("CHDOPT_TEST_CACHE");
        if(String.IsNullOrEmpty(root))root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CHD Optimizer","tools");
        Tools=Path.Combine(root,id);Directory.CreateDirectory(Tools);NativeRun.NoLinks(Tools);
        foreach(string name in new[]{"chdman.exe","7z.exe","7z.dll"}) {
            string file=Path.Combine(Tools,name);NativeRun.NoLinks(file);
            using(var resource=asm.GetManifestResourceStream(name))using(var memory=new MemoryStream()) {
                if(resource==null)throw new IOException("Componente ausente: "+name);
                resource.CopyTo(memory);byte[] data=memory.ToArray();bool same=false;
                if(File.Exists(file))using(var sha=SHA256.Create())using(var old=File.OpenRead(file))same=System.Convert.ToBase64String(sha.ComputeHash(old))==System.Convert.ToBase64String(sha.ComputeHash(data));
                if(!same)File.WriteAllBytes(file,data);
            }
        }
        return Tools;
    }
    public static async Task<int> Run(Settings settings,Action<string> report) {
        settings.Validate();Stage();Directory.CreateDirectory(settings.Output);
        string temp=Path.Combine(settings.Input,"temp");Directory.CreateDirectory(temp);NativeRun.NoLinks(temp);
        string lockFile=Path.Combine(temp,"optimizer-app.lock");NativeRun.NoLinks(lockFile);
        using(var guard=new FileStream(lockFile,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)) {
            StopFile=Path.Combine(Tools,"stop-"+Guid.NewGuid().ToString("N"));
            try {return await new NativeRun(settings,report,temp).Run().ConfigureAwait(false);}
            finally {if(File.Exists(StopFile))File.Delete(StopFile);StopFile=null;}
        }
    }
}

sealed class NativeRun {
    sealed class Result {public int Code;public string Text;}
    sealed class Media {public string Path, CueText;}
    sealed class Entry {public string Path;public long Size;}
    sealed class Snapshot {
        public string Path;public long Size,Ticks;
        public Snapshot(string path){Path=path;var f=new FileInfo(path);Size=f.Length;Ticks=f.LastWriteTimeUtc.Ticks;}
        public bool Unchanged(){NoLinks(Path);var f=new FileInfo(Path);return f.Exists&&f.Length==Size&&f.LastWriteTimeUtc.Ticks==Ticks;}
    }
    public sealed class DiscRecord {public string Serial,Type,Title,Provider,Url,Checked;}
    sealed class Game {
        public string Title="",Serial="",Type="",Status="PROCESSANDO",Detection="",Lookup="",DatabaseUrl="",Source="",Image="",Detail="";
    }
    readonly Settings s;readonly Action<string> report;readonly string temp;
    readonly JavaScriptSerializer json=new JavaScriptSerializer();readonly object logLock=new object();
    readonly Dictionary<string,DiscRecord> cache=new Dictionary<string,DiscRecord>();
    readonly HashSet<string> misses=new HashSet<string>();readonly Dictionary<string,int> failures=new Dictionary<string,int>();
    int success,existing,errors;string work;
    static readonly StringComparer Paths=StringComparer.OrdinalIgnoreCase;
    public NativeRun(Settings settings,Action<string> output,string temporary){s=settings;report=output;temp=temporary;LoadCache();}
    void Say(string text){lock(logLock){report(text);if(!text.StartsWith("GUI_")&&!text.StartsWith("Etapa:"))File.AppendAllText(Path.Combine(temp,"log.txt"),text+Environment.NewLine,Encoding.UTF8);}}
    void Show(Game game){Say("GUI_GAME:"+System.Convert.ToBase64String(Encoding.UTF8.GetBytes(json.Serialize(game))));}
    public static void NoLinks(string path){for(var d=new FileInfo(Path.GetFullPath(path));d!=null;d=d.Directory==null?null:new FileInfo(d.Directory.FullName))if((File.Exists(d.FullName)||Directory.Exists(d.FullName))&&(File.GetAttributes(d.FullName)&FileAttributes.ReparsePoint)!=0)throw new IOException("Link/junction não permitido: "+d.FullName);}
    static bool Inside(string path,string root){return Path.GetFullPath(path).StartsWith(Path.GetFullPath(root).TrimEnd('\\')+"\\",StringComparison.OrdinalIgnoreCase);}
    static string Target(string root,string relative){
        if(String.IsNullOrWhiteSpace(relative)||Path.IsPathRooted(relative)||relative.Contains(':'))throw new IOException("Caminho absoluto ou inválido no arquivo: "+relative);
        string p=Path.GetFullPath(Path.Combine(root,relative.Replace('/', '\\')));
        if(!Inside(p,root))throw new IOException("Caminho fora da pasta permitida: "+relative);NoLinks(p);return p;
    }
    static IEnumerable<string> Files(string root){
        NoLinks(root);foreach(string f in Directory.GetFiles(root)){NoLinks(f);yield return f;}
        foreach(string d in Directory.GetDirectories(root)){NoLinks(d);foreach(string f in Files(d))yield return f;}
    }
    IEnumerable<string> Sources(string root){
        foreach(string f in Directory.GetFiles(root)){NoLinks(f);yield return f;}
        if(s.Platform=="PS1")yield break;
        foreach(string d in Directory.GetDirectories(root)){
            if(Paths.Equals(d,s.Output)||Inside(d,s.Output)||Paths.Equals(d,temp))continue;
            NoLinks(d);foreach(string f in Sources(d))yield return f;
        }
    }
    static void Clean(string directory,string parent){
        if(!Inside(directory,parent))throw new IOException("Pasta temporária fora do local esperado.");
        NoLinks(directory);if(!Directory.Exists(directory))return;
        foreach(string f in Directory.GetFiles(directory)){NoLinks(f);File.Delete(f);}
        foreach(string d in Directory.GetDirectories(directory))Clean(d,parent);
        Directory.Delete(directory,false);
    }
    static string Quote(string value){return "\""+Regex.Replace(value,@"(\\*)""","$1$1\\\"")+Regex.Match(value,@"\\+$").Value+"\"";}
    async Task<Result> Tool(string name,string[] args,bool progress){
        var start=new ProcessStartInfo(Path.Combine(Engine.Tools,name),String.Join(" ",args.Select(Quote))){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,RedirectStandardInput=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8,WorkingDirectory=work??temp};
        using(var process=new Process{StartInfo=start}){
            process.Start();process.StandardInput.Close();var text=new StringBuilder();object gate=new object();bool truncated=false;
            Action<string> line=value=>{lock(gate){if(text.Length+value.Length<16000000)text.AppendLine(value);else truncated=true;}if(progress){var m=Regex.Match(value,@"(?<!\d)(\d{1,3}(?:[.,]\d+)?)\s*%");if(m.Success)Say("Etapa: "+m.Groups[1].Value+"%");}};
            await Task.WhenAll(Pump(process.StandardOutput,line),Pump(process.StandardError,line)).ConfigureAwait(false);process.WaitForExit();
            if(truncated)throw new IOException("Saída da ferramenta excedeu o limite; operação cancelada para evitar listagem incompleta.");
            if(progress&&process.ExitCode==0)Say("Etapa: 100%");
            return new Result{Code=process.ExitCode,Text=text.ToString()};
        }
    }
    static async Task Pump(StreamReader reader,Action<string> emit){char[] chars=new char[1024];var line=new StringBuilder();int n;while((n=await reader.ReadAsync(chars,0,chars.Length).ConfigureAwait(false))>0)for(int i=0;i<n;i++){char c=chars[i];if(c=='\r'||c=='\n'||c=='\b'){if(line.Length>0){emit(line.ToString());line.Clear();}}else if(line.Length<1000000)line.Append(c);}if(line.Length>0)emit(line.ToString());}
    async Task<string> Must(string name,bool progress,params string[] args){var r=await Tool(name,args,progress).ConfigureAwait(false);if(r.Code!=0)throw new IOException(name+" (código "+r.Code+"): "+r.Text.Substring(Math.Max(0,r.Text.Length-3000)));return r.Text;}
    static string Ext(string path){return Path.GetExtension(path).ToLowerInvariant();}
    static bool Archive(string p){return Regex.IsMatch(p,@"(?i)\.(zip|rar|7z|tar|gz|bz2|xz|tgz|tbz2|txz|7z\.001|zip\.001)$")&&!Regex.IsMatch(p,@"(?i)\.part0*(?:[2-9]|[1-9]\d+)\.rar$");}
    static bool ImageFile(string p){return new[]{".chd",".cue",".iso",".bin",".img"}.Contains(Ext(p));}
    static string Stem(string p){return Archive(p)?Regex.Replace(Path.GetFileName(p),@"(?i)(\.part0*1\.rar|\.(7z|zip)\.001|\.tar\.(gz|bz2|xz)|\.(zip|rar|7z|tar|gz|bz2|xz|tgz|tbz2|txz))$",""):Path.GetFileNameWithoutExtension(p);}
    static string Natural(string p){return Regex.Replace(p,@"\d+",m=>m.Value.PadLeft(16,'0'));}
    static string TrackGroup(string p){var m=Regex.Match(p,@"(?i)^(.*)\(Track\s+\d+\)\.bin$");return m.Success?m.Groups[1].Value:null;}
    static List<string> CueRefs(string text,string cue,string root){
        var refs=new List<string>();foreach(Match m in Regex.Matches(text,@"(?im)^\s*FILE\s+(?:""([^""]+)""|(\S+))\s+\S+")){
            string relative=m.Groups[1].Success?m.Groups[1].Value:m.Groups[2].Value;
            if(Path.IsPathRooted(relative)||relative.Contains(':'))throw new IOException("Referência CUE absoluta não suportada.");
            string p=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(cue),relative));if(!Inside(p,root))throw new IOException("Referência CUE fora da entrada.");NoLinks(p);if(!refs.Contains(p,Paths))refs.Add(p);
        }
        if(refs.Count==0)throw new IOException("CUE sem arquivos FILE.");return refs;
    }
    static List<Media> SelectMedia(IEnumerable<string> files,string root,Dictionary<string,string> cueTexts){
        var list=files.Where(ImageFile).OrderBy(Natural,Paths).ToList();var companions=new HashSet<string>(Paths);var result=new List<Media>();var tracks=new HashSet<string>(Paths);
        foreach(string cue in list.Where(p=>Ext(p)==".cue")){
            string text=cueTexts==null?File.ReadAllText(cue):cueTexts[cue];foreach(string p in CueRefs(text,cue,root)){if(!list.Contains(p,Paths))throw new IOException("Faixa referenciada não encontrada: "+Path.GetFileName(p));companions.Add(p);}result.Add(new Media{Path=cue,CueText=text});
        }
        foreach(string p in list.Where(p=>Ext(p)!=".cue"&&!companions.Contains(p))){string group=TrackGroup(p);if(group==null||tracks.Add(group))result.Add(new Media{Path=p});}
        return result.OrderBy(m=>Natural(m.Path),Paths).ToList();
    }
    async Task<List<Entry>> ListArchive(string archive,string destination){
        string listing=await Must("7z.exe",false,"l","-slt","-ba","-sccUTF-8","-p","--",archive).ConfigureAwait(false);
        var entries=new List<Entry>();var seen=new HashSet<string>(Paths);
        foreach(string block in Regex.Split(listing,@"\r?\n\s*\r?\n")){
            var p=Regex.Match(block,@"(?m)^Path = (.+)\r?$");if(!p.Success)continue;
            if(Regex.IsMatch(block,@"(?im)^(Symbolic Link|Hard Link) = .+|^Attributes = .*\bl[rwx-]{9}"))throw new IOException("Arquivo compactado contém links.");
            string target=Target(destination,p.Groups[1].Value.TrimEnd('\r'));
            if(!seen.Add(target))throw new IOException("Caminhos duplicados no compactado.");
            if(Regex.IsMatch(block,@"(?m)^Folder = \+|^Attributes = D"))continue;
            var size=Regex.Match(block,@"(?m)^Size = (\d+)");if(!size.Success)throw new IOException("Tamanho ausente na listagem do compactado.");
            entries.Add(new Entry{Path=target,Size=Int64.Parse(size.Groups[1].Value)});
        }
        if(entries.Count==0)throw new IOException("Compactado vazio ou listagem não reconhecida.");return entries;
    }
    async Task<List<Media>> Preview(string archive,string destination,List<Entry> entries){
        var cues=new Dictionary<string,string>(Paths);
        foreach(var e in entries.Where(e=>Ext(e.Path)==".cue")){
            if(e.Size>1048576)throw new IOException("Descritor CUE muito grande.");
            string relative=e.Path.Substring(destination.TrimEnd('\\').Length+1);
            cues[e.Path]=await Must("7z.exe",false,"x","-so","-p","-sccUTF-8","-spd","--",archive,relative).ConfigureAwait(false);
        }
        return SelectMedia(entries.Select(e=>e.Path),destination,cues);
    }
    static IEnumerable<string> Parts(string path){
        string name=Path.GetFileName(path),pattern=null;var m=Regex.Match(name,@"(?i)^(.*)\.part0*1\.rar$");
        if(m.Success)pattern="^"+Regex.Escape(m.Groups[1].Value)+@"\.part\d+\.rar$";
        else if((m=Regex.Match(name,@"(?i)^(.*\.(?:7z|zip))\.001$")).Success)pattern="^"+Regex.Escape(m.Groups[1].Value)+@"\.\d{3}$";
        else if(Ext(path)==".rar")pattern="^"+Regex.Escape(Path.GetFileNameWithoutExtension(path))+@"\.(rar|r\d{2})$";
        return pattern==null?new[]{path}:Directory.GetFiles(Path.GetDirectoryName(path)).Where(p=>Regex.IsMatch(Path.GetFileName(p),pattern,RegexOptions.IgnoreCase));
    }
    static string Serial(string text){var set=new HashSet<string>();foreach(Match m in Regex.Matches((text??"").ToUpperInvariant(),@"(?<![A-Z0-9])([A-Z]{4})[-_ ]?([0-9]{3})[. _-]?([0-9]{2})(?![A-Z0-9])"))set.Add(m.Groups[1].Value+"-"+m.Groups[2].Value+m.Groups[3].Value);return set.Count==1?set.First():"";}
    Game Identify(string source,Media media){
        var g=new Game{Source=source,Image=media.Path,Title=Stem(source)};g.Serial=Serial(Path.GetFileName(media.Path));
        if(g.Serial.Length==0&&media.CueText!=null)g.Serial=Serial(media.CueText);
        if(g.Serial.Length==0)g.Serial=Serial(Path.GetFileName(source));
        if(s.Platform=="PS2"){
            DiscRecord hit=Lookup(g.Serial);g.Lookup=hit==null?(s.Online?"Base indisponível ou sem correspondência inequívoca":"Consulta online desativada"):"Serial confirmado / "+hit.Provider;
            if(hit!=null){if(!String.IsNullOrWhiteSpace(hit.Title))g.Title=hit.Title;g.Type=hit.Type;g.DatabaseUrl=hit.Url;}
        }else g.Lookup="Plataforma PS1";return g;
    }
    string Output(Game g,string source,Media media,int index,int total){
        string name=g.Title;if(name.Length>120)name=name.Substring(0,120).TrimEnd(' ','.');
        if(!String.IsNullOrEmpty(g.Serial)){name=Regex.Replace(name,@"\s*\["+Regex.Escape(g.Serial)+@"\]","");name+=" ["+g.Serial+"]";}
        if(total>1){name+=" - Disco "+(index+1).ToString("D2");if(String.IsNullOrEmpty(g.Serial)&&g.Title==Stem(source))name+=" - "+Path.GetFileNameWithoutExtension(media.Path);}
        name=Regex.Replace(Regex.Replace(name,@"[<>:""/\\|?*\x00-\x1F]"," - "),@"\s+"," ").Trim().TrimEnd('.');
        if(name.Length>180)name=name.Substring(0,180).TrimEnd(' ','.');if(Regex.IsMatch(name,@"(?i)^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)"))name="_"+name;
        return Path.Combine(s.Output,name+".chd");
    }
    async Task<bool> ValidExisting(string path){NoLinks(path);if(!File.Exists(path))return false;var r=await Tool("chdman.exe",new[]{"info","-i",path},false).ConfigureAwait(false);if(r.Code!=0)throw new IOException("CHD existente inválido; preservado para revisão: "+path);return true;}
    async Task<bool> Already(string source,List<Media> media){
        if(media.Count==0||media.Any(m=>TrackGroup(m.Path)!=null))return false;
        var used=new HashSet<string>(Paths);var games=new List<Game>();
        for(int i=0;i<media.Count;i++){
            var g=Identify(source,media[i]);string output=Output(g,source,media[i],i,media.Count);
            if(!File.Exists(output)&&g.Serial.Length>0){string suffix=" ["+g.Serial+"]"+(media.Count>1?" - Disco "+(i+1).ToString("D2"):"")+".chd";var matches=Directory.GetFiles(s.Output,"*.chd").Where(p=>p.EndsWith(suffix,StringComparison.OrdinalIgnoreCase)).ToArray();if(matches.Length==1)output=matches[0];}
            if(!used.Add(output)||!await ValidExisting(output).ConfigureAwait(false))return false;g.Status="JÁ EXISTENTE";games.Add(g);
        }
        foreach(var g in games)Show(g);Say("Já existente: "+Path.GetFileName(source)+" — extração dispensada.");return true;
    }
    public async Task<int> Run(){
        Say("CHD Optimizer 1.2.0 — motor C# — "+DateTime.Now);Say("Origem: "+s.Input+" | Saída: "+s.Output);
        var sources=Sources(s.Input).ToList();var images=SelectMedia(sources.Where(p=>s.Platform=="PS2"?ImageFile(p):Ext(p)==".chd"),s.Input,null);
        var entries=images.Select(m=>m.Path).Concat(s.Platform=="PS2"?sources.Where(Archive):Enumerable.Empty<string>()).OrderBy(Natural,Paths).ToList();
        for(int i=0;i<entries.Count;i++){
            if(File.Exists(Engine.StopFile))return 2;
            string source=entries[i];work=Path.Combine(temp,"native-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(work);
            Say("Entrada "+(i+1)+" / "+entries.Count+": "+Path.GetFileName(source));
            try{
                if(Archive(source)){
                    var snapshots=Parts(source).Select(p=>new Snapshot(p)).ToList();long packed=snapshots.Sum(p=>p.Size);
                    string unpacked=Path.Combine(work,"unpacked");var listed=await ListArchive(source,unpacked).ConfigureAwait(false);var preview=await Preview(source,unpacked,listed).ConfigureAwait(false);
                    if(await Already(source,preview).ConfigureAwait(false)){existing++;continue;}
                    // Reject orphan tracks before extraction, including old partial CHDs.
                    var orphan=preview.FirstOrDefault(m=>TrackGroup(m.Path)!=null);if(orphan!=null){var g=Identify(source,orphan);g.Type="CD";g.Status="CUE AUSENTE";g.Detail="Faixas BIN pertencem ao mesmo disco. Forneça o CUE original.";Show(g);throw new IOException(g.Detail);}
                    Say("Descompactando: "+Path.GetFileName(source));Directory.CreateDirectory(unpacked);
                    await Must("7z.exe",true,"x","-y","-p","-bsp1","-sccUTF-8","-o"+unpacked,"--",source).ConfigureAwait(false);
                    var files=Files(unpacked).ToList();
                    if(files.Count==1&&Ext(files[0])==".tar"){
                        string nested=Path.Combine(work,"tar");await ListArchive(files[0],nested).ConfigureAwait(false);Directory.CreateDirectory(nested);
                        await Must("7z.exe",true,"x","-y","-p","-bsp1","-o"+nested,"--",files[0]).ConfigureAwait(false);unpacked=nested;files=Files(unpacked).ToList();
                    }
                    var media=SelectMedia(files,unpacked,null);if(media.Count==0)throw new IOException("Nenhuma imagem de disco encontrada.");
                    bool allNew=true;var saved=new List<Snapshot>();var outputs=new HashSet<string>(Paths);
                    for(int d=0;d<media.Count;d++){
                        var result=await Convert(source,media[d],unpacked,d,media.Count,packed,outputs).ConfigureAwait(false);
                        if(result==null)allNew=false;else saved.Add(new Snapshot(result));
                    }
                    if(allNew)success++;else existing++;
                    if(s.Delete&&allNew&&saved.Count==media.Count){
                        if(!snapshots.All(p=>p.Unchanged())||!saved.All(p=>p.Unchanged()))throw new IOException("Arquivos mudaram durante o processamento; compactado preservado.");
                        foreach(var part in snapshots){File.Delete(part.Path);Say("Compactado removido após verificação: "+Path.GetFileName(part.Path));}
                    }
                }else{
                    var media=images.First(m=>Paths.Equals(m.Path,source));var output=await Convert(source,media,s.Input,0,1,0,new HashSet<string>(Paths)).ConfigureAwait(false);if(output==null)existing++;else success++;
                }
            }catch(Exception ex){errors++;Say("ERRO: "+ex.Message);}
            finally{
                try{Clean(work,temp);}catch(Exception ex){Say("Aviso: limpeza temporária: "+ex.Message);}
                work=null;Say("Lote "+s.Platform+": "+(100.0*(i+1)/Math.Max(1,entries.Count)).ToString("F1")+"% ("+(i+1)+"/"+entries.Count+") | Sucessos: "+success+" | Já existentes: "+existing+" | Erros: "+errors);
            }
        }
        return errors==0?0:1;
    }
    async Task<string> Convert(string source,Media media,string root,int index,int total,long packed,HashSet<string> outputs){
        var g=Identify(source,media);Show(g);
        try{
            if(TrackGroup(media.Path)!=null)throw new IOException("CUE AUSENTE: forneça o CUE original para o conjunto de faixas BIN.");
            string output=Output(g,source,media,index,total);if(!outputs.Add(output))throw new IOException("Dois discos resultaram no mesmo nome de saída.");
            if(await ValidExisting(output).ConfigureAwait(false)){g.Status="JÁ EXISTENTE";Show(g);Say("Já existente: "+Path.GetFileName(output));return null;}
            string input=media.Path;bool chd=Ext(input)==".chd";long original=new FileInfo(input).Length,oldHunk=0;
            string folder=Path.Combine(work,"disc-"+(index+1));Directory.CreateDirectory(folder);
            if(chd){
                string info=await Must("chdman.exe",false,"info","-i",input).ConfigureAwait(false);oldHunk=InfoNumber(info,"Hunk Size");long unit=InfoNumber(info,"Unit Size");
                g.Type=unit==2448||Regex.IsMatch(info,@"CHT2|CHTR|CHCD")?"CD":unit==2048||info.Contains("DVD ")?"DVD":"";
                if(g.Type.Length==0)throw new IOException("Tipo de CHD não reconhecido como CD/DVD.");g.Detection="Metadados CHD";Say("Extraindo: "+Path.GetFileName(input));
                if(g.Type=="CD"){input=Path.Combine(folder,"disc.cue");await Must("chdman.exe",true,"extractcd","-i",media.Path,"-o",input,"-ob",Path.Combine(folder,"disc.bin")).ConfigureAwait(false);}
                else{input=Path.Combine(folder,"disc.iso");await Must("chdman.exe",true,"extractdvd","-i",media.Path,"-o",input).ConfigureAwait(false);}
            }else if(Ext(input)==".cue"){
                var refs=CueRefs(media.CueText,input,root);original=0;foreach(string p in refs){NoLinks(p);var f=new FileInfo(p);if(!f.Exists||f.Length==0)throw new IOException("Faixa ausente/vazia: "+p);original+=f.Length;}
                g.Type="CD";g.Detection="Descritor CUE original";
            }else{
                byte[] header=new byte[16];using(var f=File.OpenRead(input))f.Read(header,0,16);
                bool raw=original>0&&original%2352==0&&header[0]==0&&header[11]==0&&header.Skip(1).Take(10).All(b=>b==255)&&(header[15]==1||header[15]==2);
                string mode;
                if(raw){g.Type="CD";mode="MODE"+header[15]+"/2352";g.Detection="Cabeçalho RAW de CD: "+mode;}
                else if(original>0&&original%2048==0){mode="MODE1/2048";if(g.Type.Length==0){g.Type=original<=400L*1024*1024?"CD":"DVD";g.Detection="Heurística: imagem de 2048 bytes/setor; limite de 400 MiB";}else g.Detection="Serial "+g.Serial+" / "+g.Lookup;}
                else throw new IOException("Imagem sem setores de 2048/2352 reconhecíveis. Para BIN multifaixa/áudio, forneça o CUE original.");
                if(g.Type=="CD"){
                    string relative=Uri.UnescapeDataString(new Uri(folder+"\\").MakeRelativeUri(new Uri(input)).ToString()).Replace('/', '\\');
                    input=Path.Combine(folder,"disc.cue");File.WriteAllText(input,"FILE \""+relative+"\" BINARY\r\n  TRACK 01 "+mode+"\r\n    INDEX 01 00:00:00\r\n",new UTF8Encoding(false));
                }
            }
            if(s.Platform=="PS1"&&g.Type!="CD")throw new IOException("A plataforma PS1 aceita apenas CHDs de CD.");
            int hunk=g.Type=="CD"?s.CdHunk:s.DvdHunk;string codecs=g.Type=="CD"?s.Cd:s.Dvd;
            Say("Tipo: "+g.Type+" | Hunk: "+hunk+" | Codecs: "+codecs);Show(g);
            string candidate=Path.Combine(folder,"encoded.chd");Say("Comprimindo: "+g.Title);
            await Must("chdman.exe",true,g.Type=="CD"?"createcd":"createdvd","-i",input,"-o",candidate,"-hs",hunk.ToString(),"-c",codecs,"-np",s.Threads.ToString()).ConfigureAwait(false);
            if(chd&&new FileInfo(media.Path).Length<=new FileInfo(candidate).Length&&(s.Platform=="PS1"||oldHunk==hunk)){candidate=media.Path;Say("Original CHD menor: mantido.");}
            // Copy into the destination volume, verify there, then publish without overwrite.
            string pending=Path.Combine(s.Output,".chdopt-"+Guid.NewGuid().ToString("N")+".tmp");
            try{File.Copy(candidate,pending,false);Say("Verificando: "+g.Title);await Must("chdman.exe",true,"verify","-i",pending).ConfigureAwait(false);NoLinks(output);File.Move(pending,output);}
            finally{if(File.Exists(pending))File.Delete(pending);}
            long final=new FileInfo(output).Length;Say("Salvo: "+output+" | Original: "+original+" bytes | CHD: "+final+" bytes"+(packed>0?" | Compactado: "+packed+" bytes":""));
            g.Status="CONCLUÍDO";g.Detail="CHD verificado; "+final+" bytes";Show(g);return output;
        }catch(Exception ex){g.Status="ERRO";g.Detail=ex.Message;Show(g);throw;}
    }
    static long InfoNumber(string info,string key){var m=Regex.Match(info,Regex.Escape(key)+@":\s*([0-9,. ]+)",RegexOptions.IgnoreCase);return m.Success?Int64.Parse(Regex.Replace(m.Groups[1].Value,@"\D","")):0;}
    void LoadCache(){try{string p=Path.Combine(temp,"ps2-media-cache.json");if(!File.Exists(p))return;foreach(var r in json.Deserialize<DiscRecord[]>(File.ReadAllText(p))){DateTime date;if(Regex.IsMatch(r.Serial??"",@"^[A-Z]{4}-\d{5}$")&&(r.Type=="CD"||r.Type=="DVD")&&!String.IsNullOrWhiteSpace(r.Title)&&(r.Provider=="Redump"||r.Provider=="PSX Data Center")&&DateTime.TryParse(r.Checked,out date)&&(DateTime.UtcNow-date.ToUniversalTime()).TotalDays<180)cache[r.Serial]=r;}}catch{}}
    void SaveCache(){string p=Path.Combine(temp,"ps2-media-cache.json"),tmp=p+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllText(tmp,json.Serialize(cache.Values.ToArray()),Encoding.UTF8);if(File.Exists(p))File.Replace(tmp,p,null);else File.Move(tmp,p);}catch(Exception ex){Say("Aviso: cache de mídia: "+ex.Message);}finally{if(File.Exists(tmp))File.Delete(tmp);}}
    static string Plain(string html){return Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(html,@"<[^>]*>"," ")),@"\s+"," ").Trim();}
    static List<string> Fields(string html,params string[] labels){var values=new List<string>();foreach(Match row in Regex.Matches(html,@"(?is)<tr\b[^>]*>((?:(?!<tr\b).)*?)</tr>")){var cells=Regex.Matches(row.Groups[1].Value,@"(?is)<t[dh]\b[^>]*>(.*?)</t[dh]>");if(cells.Count<2||!labels.Contains(Plain(cells[0].Groups[1].Value),StringComparer.OrdinalIgnoreCase))continue;for(int i=1;i<cells.Count;i++)values.Add(Plain(cells[i].Groups[1].Value));}return values;}
    DiscRecord Lookup(string serial){
        if(serial.Length==0)return null;DiscRecord hit;if(cache.TryGetValue(serial,out hit))return hit;if(!s.Online||misses.Contains(serial))return null;
        var providers=new[]{"Redump","PSX Data Center"};var urls=new[]{"https://redump.info/discs?system=PS2&q="+serial,"https://psxdatacenter.com/psx2/games2/"+serial+".html"};
        for(int i=0;i<urls.Length;i++){
            int failed;failures.TryGetValue(providers[i],out failed);if(failed>=2)continue;
            try{
                ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;var request=(HttpWebRequest)WebRequest.Create(urls[i]);request.Timeout=10000;request.ReadWriteTimeout=10000;request.MaximumAutomaticRedirections=4;request.UserAgent="CHD-Optimizer/1.2.0";
                string html;using(var response=request.GetResponse())using(var stream=response.GetResponseStream())using(var reader=new StreamReader(stream)){var buffer=new char[8192];var text=new StringBuilder();int n;while((n=reader.Read(buffer,0,buffer.Length))>0){text.Append(buffer,0,n);if(text.Length>8000000)throw new IOException("Página muito grande.");}html=text.ToString();}failures[providers[i]]=0;
                string serialFields=String.Join(" ",Fields(html,i==0?new[]{"Disc Serial","Disc Serials","Serial"}:new[]{"SERIAL NUMBER(S)"}));
                if(!Regex.IsMatch(serialFields.Replace('_','-'),Regex.Escape(serial),RegexOptions.IgnoreCase)&&Serial(serialFields)!=serial)continue;
                if(i==0&&!Fields(html,"System").Contains("Sony PlayStation 2"))continue;
                var types=new HashSet<string>();foreach(string value in Fields(html,"MEDIA"))types.Add(Regex.IsMatch(value,@"(?i)^CD(?:-ROM)?$")?"CD":Regex.IsMatch(value,@"(?i)^DVD(?:-[59]|-ROM)?$")?"DVD":"?");if(types.Count!=1||types.Contains("?"))continue;
                string title=i==1?String.Join(" ",Fields(html,"OFFICIAL TITLE")):Plain(Regex.Match(html,@"(?is)<div\b[^>]*class=""disc-title-box""[^>]*>\s*<h2\b[^>]*>(.*?)</h2>").Groups[1].Value);
                hit=new DiscRecord{Serial=serial,Type=types.First(),Title=title,Provider=providers[i],Url=urls[i],Checked=DateTime.UtcNow.ToString("o")};cache[serial]=hit;SaveCache();return hit;
            }catch{failures[providers[i]]=failed+1;}
        }
        misses.Add(serial);return null;
    }
}
