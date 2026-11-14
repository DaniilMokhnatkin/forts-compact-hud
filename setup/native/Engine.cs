using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace FortsSetup {
    public sealed class Options {
        public bool Hud;
        public bool ClickThrough=true;
        public string Layout="compact", Skin="dlc3", Crosshair, Color="white";
        public int Size=100;
        public string CrosshairChoice,ColorChoice;
        public HudCustomization Customization;
        public bool? SkipIntro;
        public byte[] MenuBackground, ScreensBackground;
        // UI-only originals and crop coordinates keep editing reversible across language switches.
        public byte[] MenuBackgroundSource, ScreensBackgroundSource;
        public int[] MenuCrop, ScreensCrop;
        public bool RestoreBackground;
        public bool BackgroundLogo;
        public Dictionary<string,int> Icons=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
    }
    public sealed class PackStatus {
        public bool HudCompatible=true, HudInstalled, HasBackup, SkipIntro;
        public bool ClickThrough=true;
        public bool MenuBackground, ScreensBackground;
        public string Layout, Skin, Crosshair, Color="white";
        public int Size=100;
        public HudCustomization Customization;
        public Dictionary<string,int> Icons=Engine.IconIds.ToDictionary(k=>k,k=>100);
    }
    // Lowercase property names keep backups compatible with the previous installer.
    public sealed class StateEntry {
        public string path {get;set;}
        public bool existed {get;set;}
        public string original {get;set;}
        public string installed {get;set;}
        public string owner {get;set;}
    }
    sealed class PlannedFile { public string Rel, Source, Owner; public byte[] Bytes; }
    public sealed class Engine {
        public const string BackupName="Forts-Setup-Backup", TextureDir=@"data\ui\textures\HUD\custom";
        public static readonly string[] LayoutIds={"compact","zors","minimalist","tactical","grib"}, SkinIds={"dlc3","base","dlc1","dlc2","pro","elite"};
        public static readonly string[] IconIds={"extinguisher","under_construction","repair","scrap"};
        public static readonly string[] MenuSkins={"base","classic","tog","dlc1","dlc2","dlc3"};
        public static void ValidateBackground(byte[] data) {
            if(data==null)return;
            if(data.Length<128)throw new ArgumentException("Некорректная текстура фона.");
            int width=BitConverter.ToInt32(data,16),height=BitConverter.ToInt32(data,12);
            if(width<1||height<1||width>2048||height>2048||data.Length!=128+width*height*4||BitConverter.ToInt32(data,0)!=0x20534444||BitConverter.ToInt32(data,4)!=124||BitConverter.ToInt32(data,80)!=0x41||BitConverter.ToInt32(data,84)!=0||BitConverter.ToInt32(data,88)!=32||BitConverter.ToInt32(data,92)!=0x00FF0000||BitConverter.ToInt32(data,96)!=0x0000FF00||BitConverter.ToInt32(data,100)!=0x000000FF||BitConverter.ToUInt32(data,104)!=0xFF000000)throw new ArgumentException("Некорректная текстура фона.");
        }
        public static bool CanCustomize(string layout){return layout=="compact"||layout=="tactical"||layout=="zors"||layout=="grib";}
        public static bool PreservesCrosshairColours(string id){return id=="zors"||id=="grib"||(id!=null&&(id.StartsWith("duo_",StringComparison.Ordinal)||id.StartsWith("neo_",StringComparison.Ordinal)));}
        public static bool RequiresDefaultHudSkin(string layout){return layout=="zors"||layout=="grib"||layout=="minimalist"||layout=="tactical";}
        public static readonly Dictionary<string,string> Colors=new Dictionary<string,string>{{"white","FFFFFF"},{"green","5CFF6E"},{"yellow","FFE04A"},{"red","FF5A4E"},{"cyan","46E6FF"}};
        public static readonly string[] CrossFiles={@"data\ui\textures\mouse_target.dds",@"data\ui\textures\mouse_target_painted.dds"};
        static readonly Dictionary<string,string> HudFiles=new Dictionary<string,string>{
            {@"data\ui\screens\hud.lua","1E0004F7EF8BBA8F8C4CA4BD897D32EB229A0CF1F06905A30BABC3359ED9893C"},
            {@"data\ui\uisprites.lua","45A88437D1A0FD7F18131BAE62A6BB098EDF639B8B331610A7DC1B1CF6FB4E49"},
            {@"data\ui\screens\transaction.lua","7D31332AEEE2FB7F0929D3D4E044590013BA8CF0DA28FE225C0FB5B8AC63FBBB"}};
        static readonly string[] OldHudHashes={"93BD9A12D9EF1D53F454DEF18D3611FB7BA0E9A5E30D9C7E1CCAF733789D743D","08ED3730AB95598A279FB85F594E45C29232704A20B5F8986010260A92B0653E","EC8BF7E1BCB5923343A3C265DB7B0769A297A814472F482E1CD4F281F802105B"};
        const string CursorBlock=@"(?s)\r?\n-- BEGIN FORTS SETUP CURSOR SIZE\r?\n.*?-- END FORTS SETUP CURSOR SIZE\r?\n";
        const string IntroBlock=@"(?s)\r?\n-- BEGIN FORTS SETUP SKIP INTRO\r?\n.*?-- END FORTS SETUP SKIP INTRO\r?\n";
        readonly string root;
        readonly JavaScriptSerializer json=new JavaScriptSerializer { MaxJsonLength=16*1024*1024 };
        public Engine(string packRoot) { root=Path.GetFullPath(packRoot); }
        public string PackRoot { get {return root;} }
        public string Payload(string rel) { return SafePath(Path.Combine(root,"payload"),rel); }
        public string[] Crosshairs { get {return Directory.GetDirectories(Payload("crosshairs")).Select(Path.GetFileName).OrderBy(k=>k).ToArray();} }
        public static string SafePath(string dir,string rel) {
            if (String.IsNullOrWhiteSpace(rel) || Path.IsPathRooted(rel) || rel.Contains(':')) throw new IOException("Недопустимый путь файла: "+rel);
            string baseDir=Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            string path=Path.GetFullPath(Path.Combine(baseDir,rel));
            if (!path.StartsWith(baseDir,StringComparison.OrdinalIgnoreCase)) throw new IOException("Путь выходит за пределы папки: "+rel);
            // Do not follow junctions/symlinks out of the target directory.
            for(string part=path;part!=null && part.StartsWith(baseDir,StringComparison.OrdinalIgnoreCase);part=Path.GetDirectoryName(part))
                if ((File.Exists(part)||Directory.Exists(part)) && (File.GetAttributes(part)&FileAttributes.ReparsePoint)!=0) throw new IOException("Ссылки в пути установки не поддерживаются: "+part);
            return path;
        }
        public static string Hash(string path) { if (!File.Exists(path)) return null; using(var s=File.OpenRead(path)) using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(s)).Replace("-",""); }
        static string HashBytes(byte[] bytes) {using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");}
        public static bool IsGame(string game) {return !String.IsNullOrEmpty(game) && File.Exists(Path.Combine(game,"Forts.exe")) && Directory.Exists(Path.Combine(game,@"data\ui\screens"));}
        static void CheckGame(string game) {
            if(!IsGame(game)) throw new IOException("В этой папке нет Forts.exe.");
            foreach(var p in Process.GetProcessesByName("Forts")) using(p) {
                try { if(String.Equals(p.MainModule.FileName,Path.Combine(game,"Forts.exe"),StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Закрой Forts и попробуй ещё раз."); }
                catch(System.ComponentModel.Win32Exception) {throw new InvalidOperationException("Не удалось проверить запущенный Forts. Закрой игру и повтори.");}
            }
        }
        public static string FindGame() {
            var libs=new List<string>();
            foreach(var key in new[]{@"HKEY_CURRENT_USER\Software\Valve\Steam",@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam"})
                foreach(var name in new[]{"SteamPath","InstallPath"}) {var value=Registry.GetValue(key,name,null) as string; if(value!=null) libs.Add(value);}
            libs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"Steam"));
            foreach(string lib in libs.ToArray()) {string vdf=Path.Combine(lib,@"steamapps\libraryfolders.vdf"); if(File.Exists(vdf)) foreach(Match m in Regex.Matches(File.ReadAllText(vdf),"\"path\"\\s+\"([^\"]+)\"")) libs.Add(m.Groups[1].Value.Replace("\\\\","\\"));}
            return libs.Distinct(StringComparer.OrdinalIgnoreCase).Select(p=>Path.Combine(p,@"steamapps\common\Forts")).FirstOrDefault(IsGame);
        }
        public static string Settings(string game,string name) {
            if(String.IsNullOrEmpty(game)||!Directory.Exists(Path.Combine(game,"users"))) return null;
            return Directory.GetDirectories(Path.Combine(game,"users")).Select(p=>Path.Combine(p,name)).Where(File.Exists).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
        }
        public static string GameSkin(string game,bool menu) {
            string file=Settings(game,"settings.lua"); if(file==null) return null;
            var m=Regex.Match(File.ReadAllText(file),(menu?"CurrentMenuSkin":"CurrentHUDSkin")+"\\s*=\\s*\"(?:db/skins_"+(menu?"menu":"hud")+"_?([a-z0-9_]*)[^\"]*)?\"");
            if(!m.Success)return null; string key=m.Groups[1].Value.Replace("_tester",""); return String.IsNullOrEmpty(key)?"base":key;
        }
        public static string DefaultLanguage(string game) {
            string f=Settings(game,"settings.lua"); if(f!=null) {var m=Regex.Match(File.ReadAllText(f),"Language2\\s*=\\s*\"([^\"]+)\""); if(m.Success)return new[]{"Russian","Ukrainian"}.Contains(m.Groups[1].Value)?"ru":"en";}
            return new[]{"ru","uk","be","kk"}.Contains(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)?"ru":"en";
        }
        Dictionary<string,StateEntry> ReadState(string game) {
            string path=Path.Combine(game,BackupName,"state.json"); var state=new Dictionary<string,StateEntry>(StringComparer.OrdinalIgnoreCase);
            if(!File.Exists(path))return state;
            string text=File.ReadAllText(path).Trim();
            var entries=text.StartsWith("[")?json.Deserialize<StateEntry[]>(text):new[]{json.Deserialize<StateEntry>(text)};
            foreach(var e in entries) {SafePath(game,e.path);SafePath(Path.Combine(game,BackupName),e.path);if(String.IsNullOrEmpty(e.owner))e.owner=CrossFiles.Contains(e.path)||e.path==@"data\db\constants_user.lua"?"crosshair":IconIds.Any(k=>Path.GetFileNameWithoutExtension(e.path)==k||Path.GetFileNameWithoutExtension(e.path)==k+"_grey")?"icons":"hud"; state.Add(e.path,e);}
            return state;
        }
        public PackStatus Status(string game) {
            var s=new PackStatus(); if(!IsGame(game)){s.HudCompatible=false;return s;}
            var state=ReadState(game);s.HasBackup=state.Count>0;
            s.MenuBackground=state.Values.Any(e=>e.owner=="background"&&Path.GetFileName(e.path)=="FE-MainMenu-BG.dds"&&Hash(SafePath(game,e.path))==e.installed);
            s.ScreensBackground=state.Values.Any(e=>e.owner=="background"&&Path.GetFileName(e.path)=="FE-AllScreens-BG.dds"&&Hash(SafePath(game,e.path))==e.installed);
            string hudNow=Hash(SafePath(game,@"data\ui\screens\hud.lua"));
            foreach(string layout in LayoutIds) if(hudNow!=null && hudNow==Hash(LayoutHud(layout)))s.Layout=layout;
            string profileFile=Path.Combine(game,BackupName,"hud-profile.json");
            if(File.Exists(profileFile)){
                var profile=json.Deserialize<HudProfile>(File.ReadAllText(profileFile));
                if(profile!=null&&profile.hash==hudNow&&LayoutIds.Contains(profile.layout)){
                    if(profile.customization!=null)profile.customization.Validate();
                    s.Layout=profile.layout;s.Customization=profile.customization;s.ClickThrough=profile.clickThrough;
                }
            }
            foreach(var f in HudFiles) {
                string current=Hash(SafePath(game,f.Key)); StateEntry previous; state.TryGetValue(f.Key,out previous);
                bool known=current!=null&&(current==f.Value||current==Hash(Payload(@"hud\"+f.Key))||(previous!=null&&current==previous.installed));
                if(f.Key.EndsWith(@"screens\hud.lua")) known|=current!=null&&(OldHudHashes.Contains(current)||s.Layout!=null);
                if(f.Key==@"data\ui\uisprites.lua") known|=current=="324F43C990EC0757B921A641670FF66A20CDE22BE9E5EBD1DBA8A61BB13F6FC4";
                if(!known)s.HudCompatible=false;
            }
            s.HudInstalled=s.Layout!=null;
            if(s.Layout!="minimalist") foreach(string rel in HudFiles.Keys.Skip(1)) if(Hash(SafePath(game,rel))!=Hash(Payload(@"hud\"+rel)))s.HudInstalled=false;
            string pause=Hash(SafePath(game,TextureDir+@"\Pause-A.dds"));
            foreach(string skin in SkinIds)if(pause!=null&&pause==Hash(Path.Combine(SkinDir(skin),"Pause-A.dds")))s.Skin=skin;
            string cross=Hash(SafePath(game,CrossFiles[0]));
            string info=Path.Combine(game,BackupName,"crosshair.json");
            if(File.Exists(info)) {
                var data=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(info));
                if(cross!=null&&Object.Equals(data["hash"],cross)&&Crosshairs.Contains((string)data["id"])&&Colors.ContainsKey((string)data["color"])) {s.Crosshair=(string)data["id"];s.Color=(string)data["color"];s.Size=Convert.ToInt32(data["size"]);}
            }
            if(s.Crosshair==null && cross!=null) foreach(string id in Crosshairs)foreach(int size in Enumerable.Range(10,13).Select(n=>n*5)) {
                string dir=CrossDir(id,size);
                if(cross==Hash(Path.Combine(dir,"mouse_target.dds"))&&Hash(SafePath(game,CrossFiles[1]))==Hash(Path.Combine(dir,"mouse_target_painted.dds"))){s.Crosshair=id;s.Size=size;}
            }
            string config=SafePath(game,@"data\db\constants_user.lua");
            if(File.Exists(config)) {string text=File.ReadAllText(config);s.SkipIntro=Regex.IsMatch(text,@"Game\.SkipIntro\s*=\s*true");var m=Regex.Match(Regex.Match(text,CursorBlock).Value,@"-- Forts Setup size: (\d+)");if(s.Crosshair!=null&&m.Success)s.Size=Int32.Parse(m.Groups[1].Value);}
            string icons=Path.Combine(game,BackupName,"icons.json");
            if(File.Exists(icons)) {var data=json.Deserialize<Dictionary<string,int>>(File.ReadAllText(icons));foreach(string id in IconIds){StateEntry e;string rel=@"data\ui\textures\"+id+".dds";if(data.ContainsKey(id)&&state.TryGetValue(rel,out e)&&e.owner=="icons"&&Hash(SafePath(game,rel))==e.installed)s.Icons[id]=data[id];}}
            return s;
        }
        string LayoutHud(string layout) {return layout=="compact"?Payload(@"hud\data\ui\screens\hud.lua"):Payload(@"layouts\"+layout+@"\hud.lua");}
        // Not HUD chrome: the pause menu plate, end screens, achievements, fire alarms and the replay UI are never
        // recoloured, whatever colour or hue the HUD uses.
        public static bool KeepsOriginalColours(string rel){string r=rel.ToLowerInvariant();return r.Contains("pausebg")||r.Contains("ui-defeat")||r.Contains("ui-victory")||r.Contains("achievementspanel")||r.Contains("firealarm")||r.Contains(@"\replay\");}
        string SkinDir(string skin) {return skin=="dlc3"?Payload(@"hud\"+TextureDir):Payload(@"skins\"+skin);}
        string CrossDir(string id,int size) {return Payload(@"crosshairs\"+id+(size!=100&&size<=110?@"\sizes\"+size:""));}
        public static byte[] Tint(byte[] dds,string hex,int opacity) {
            int start=HudCustomization.BgraPixelOffset(dds);
            byte[] b=(byte[])dds.Clone();int r=Convert.ToInt32(hex.Substring(0,2),16),g=Convert.ToInt32(hex.Substring(2,2),16),blue=Convert.ToInt32(hex.Substring(4,2),16);
            for(int i=start;i+3<b.Length;i+=4){b[i]=(byte)(b[i]*blue/255);b[i+1]=(byte)(b[i+1]*g/255);b[i+2]=(byte)(b[i+2]*r/255);b[i+3]=(byte)(b[i+3]*opacity/100);}return b;
        }
        public static string ConfigText(string before,int? percent,bool? intro) {
            string text=before;
            if(intro.HasValue){text=Regex.Replace(Regex.Replace(text,IntroBlock,""),@"(?s)\r?\n-- BEGIN SKIP INTRO\b.*?-- END SKIP INTRO\r?\n","");if(intro.Value)text+="\n-- BEGIN FORTS SETUP SKIP INTRO\n-- the engine reads Game.SkipIntro at startup (like -novid)\nGame = Game or {}\nGame.SkipIntro = true\n-- END FORTS SETUP SKIP INTRO\n";}
            if(percent.HasValue){text=Regex.Replace(text,CursorBlock,"");if(percent.Value>110){string width=(32*percent.Value/100.0).ToString("0.###",CultureInfo.InvariantCulture),offset=(-16*percent.Value/100.0).ToString("0.###",CultureInfo.InvariantCulture);text+="\n-- BEGIN FORTS SETUP CURSOR SIZE\n-- Forts Setup size: "+percent+"\nif Input and Input.Mouse and Input.Mouse.Cursors then\n    for _, cursor in ipairs(Input.Mouse.Cursors) do\n        if cursor.Sprite == \"ui/textures/mouse_target.dds\" or cursor.Sprite == \"ui/textures/mouse_target_painted.dds\" then\n            cursor.Size = { "+width+", "+width+" }\n            cursor.Offset = { "+offset+", "+offset+" }\n        end\n    end\nend\n-- END FORTS SETUP CURSOR SIZE\n";}}
            return text;
        }
        public void Install(string game,Options o,Action<int,int,string> progress) {
            CheckGame(game);
            if(!o.Hud&&o.Crosshair==null&&o.Icons.Count==0&&!o.SkipIntro.HasValue&&o.MenuBackground==null&&o.ScreensBackground==null&&!o.RestoreBackground)throw new ArgumentException("Ничего не выбрано");
            ValidateBackground(o.MenuBackground);ValidateBackground(o.ScreensBackground);
            if(o.RestoreBackground&&(o.MenuBackground!=null||o.ScreensBackground!=null))throw new ArgumentException("Выбери установку или возврат фона.");
            if(o.Hud&&!LayoutIds.Contains(o.Layout))throw new ArgumentException("Неизвестный HUD.");
            var customization=CanCustomize(o.Layout)&&o.Customization!=null?o.Customization.For(o.Layout):null;
            if(customization!=null)customization.Validate();
            if(!SkinIds.Contains(o.Skin)||!Colors.ContainsKey(o.Color)||o.Size<50||o.Size>200||o.Size%5!=0)throw new ArgumentException("Недопустимые настройки прицела или скина.");
            if(o.Icons.Any(p=>!IconIds.Contains(p.Key)||p.Value<10||p.Value>100))throw new ArgumentException("Прозрачность иконок: от 10 до 100%.");
            if(o.Crosshair!=null&&!Crosshairs.Contains(o.Crosshair))throw new ArgumentException("Неизвестный прицел.");
            byte[] menuTexture=BackgroundTexture.Prepare(o.MenuBackground),screensTexture=BackgroundTexture.Prepare(o.ScreensBackground);
            string crossColor=PreservesCrosshairColours(o.Crosshair)?"white":o.Color;
            var plan=new Dictionary<string,PlannedFile>(StringComparer.OrdinalIgnoreCase);
            Action<string,string,string> add=(rel,source,owner)=>plan[rel]=new PlannedFile{Rel=rel,Source=source,Owner=owner};
            var status=Status(game);int? cursor=null;
            if(o.Hud){
                if(!status.HudCompatible)throw new InvalidOperationException("HUD сделан для Forts 1.39.0 r22745, а файлы интерфейса у тебя другие (другая версия игры или чужой мод).");
                string selectedHud=customization==null?LayoutHud(o.Layout):Payload(@"customization\variants\"+o.Layout+"\\"+customization.Key+".lua");
                if(!o.ClickThrough){string payloadRoot=Payload("mouse-blocking");string baseRoot=Payload("hud");string relative=selectedHud.Substring(Path.GetDirectoryName(baseRoot).Length+1);selectedHud=SafePath(payloadRoot,relative);}
                if(customization!=null&&o.Layout=="tactical"){
                    string chrome=Payload(@"customization\textures");
                    if(!Directory.Exists(chrome))throw new IOException("В установщике отсутствуют текстуры настроенного HUD.");
                    foreach(string file in Directory.GetFiles(chrome,"*",SearchOption.AllDirectories))add(@"data\ui\textures\"+file.Substring(chrome.Length+1),file,"hud");
                }
                if(o.Layout=="minimalist")add(@"data\ui\screens\hud.lua",selectedHud,"hud");
                else {foreach(string rel in HudFiles.Keys)add(rel,rel==@"data\ui\screens\hud.lua"?selectedHud:Payload(@"hud\"+rel),"hud");foreach(string tex in Directory.GetFiles(SkinDir(o.Skin)))add(TextureDir+"\\"+Path.GetFileName(tex),tex,"hud");}
                foreach(string sub in new[]{"textures","ui"}){
                    string from=Payload(@"layouts\"+o.Layout+"\\"+sub);if(!Directory.Exists(from))continue;
                    foreach(string file in Directory.GetFiles(from,"*",SearchOption.AllDirectories)){
                        string rel=(sub=="textures"?@"data\ui\textures\":@"data\ui\")+file.Substring(from.Length+1);add(rel,file,"hud");
                        if(rel.StartsWith(@"data\ui\textures\menu_backgrounds\base\"))foreach(string skin in new[]{"classic","tog"})add(rel.Replace(@"\menu_backgrounds\base\",@"\menu_backgrounds\"+skin+"\\"),file,"hud");
                    }
                }
                if(o.Crosshair==null&&File.Exists(Payload(@"layouts\"+o.Layout+@"\textures\mouse_target.dds")))cursor=100;
                if(customization!=null&&(customization.Hue!=0||customization.White)){
                    string extras=Payload(@"customization\skins\"+o.Skin);
                    if(Directory.Exists(extras))foreach(string file in Directory.GetFiles(extras,"*.dds",SearchOption.AllDirectories)){
                        string rel=@"data\ui\textures\"+file.Substring(extras.Length+1);if(!plan.ContainsKey(rel)&&!KeepsOriginalColours(rel))add(rel,file,"hud");
                    }
                    foreach(var p in plan.Values.Where(p=>p.Rel.StartsWith(@"data\ui\textures\HUD\",StringComparison.OrdinalIgnoreCase)&&p.Rel.EndsWith(".dds",StringComparison.OrdinalIgnoreCase))){
                        string name=Path.GetFileName(p.Rel).ToLowerInvariant();if((name.Contains("reactor")&&!customization.White)||name.Contains("radial")||name.Contains("star"))continue;
                        if(customization.White){
                            // Icon buttons ship as ready-made white art (all four states); only the rest is recoloured.
                            string art=Payload(@"customization\white\"+p.Rel.Substring(@"data\ui\textures\".Length));
                            if(File.Exists(art)){p.Source=art;p.Bytes=null;continue;}
                        }
                        byte[] source=File.ReadAllBytes(p.Source);
                        p.Bytes=customization.White?HudCustomization.Whiten(source,p.Rel):HudCustomization.ShiftHue(source,customization.Hue);
                    }
                }
            }
            string iconLayout=o.Hud?o.Layout:status.Layout??"compact";
            foreach(var icon in o.Icons)foreach(string file in new[]{icon.Key,icon.Key+"_grey"}){
                string rel=@"data\ui\textures\"+file+".dds";
                if(icon.Value>=100){string own=Payload(@"layouts\"+iconLayout+@"\textures\"+file+".dds");if(File.Exists(own))add(rel,own,"hud");}
                else {string source=Payload(@"icons\"+iconLayout+"\\"+file+".dds");if(File.Exists(source))plan[rel]=new PlannedFile{Rel=rel,Bytes=Tint(File.ReadAllBytes(source),"FFFFFF",icon.Value),Owner="icons"};}
            }
            if(o.Crosshair!=null){string dir=CrossDir(o.Crosshair,o.Size);foreach(string rel in CrossFiles){string src=Path.Combine(dir,Path.GetFileName(rel));if(crossColor=="white")add(rel,src,"crosshair");else{string tint=Path.Combine(dir,"tint.dds");plan[rel]=new PlannedFile{Rel=rel,Bytes=Tint(File.ReadAllBytes(File.Exists(tint)?tint:Path.Combine(dir,"mouse_target.dds")),Colors[crossColor],100),Owner="crosshair"};}}cursor=o.Size;}
            const string configRel=@"data\db\constants_user.lua";string config=SafePath(game,configRel),before=File.Exists(config)?File.ReadAllText(config):"",after=ConfigText(before,cursor,o.SkipIntro);
            if(after!=before)plan[configRel]=new PlannedFile{Rel=configRel,Bytes=new UTF8Encoding(false).GetBytes(after),Owner="config"};
            foreach(var p in plan.Values){SafePath(game,p.Rel);if(p.Bytes==null&&!File.Exists(p.Source))throw new FileNotFoundException("Не хватает файла установщика.",p.Source);}
            var state=ReadState(game);
            // A HUD may bundle menu art. Keep explicitly installed player backgrounds.
            foreach(var e in state.Values.Where(e=>e.owner=="background"))plan.Remove(e.path);
            foreach(string skin in MenuSkins)foreach(var item in new[]{new {Name="FE-MainMenu-BG.dds",Data=menuTexture},new {Name="FE-AllScreens-BG.dds",Data=screensTexture}}){
                if(item.Data!=null){string rel=@"data\ui\textures\menu_backgrounds\"+skin+"\\"+item.Name;plan[rel]=new PlannedFile{Rel=rel,Bytes=item.Data,Owner="background"};}
            }
            var reverting=state.Values.Where(e=>((o.Hud&&e.owner=="hud")||(o.Icons.Count>0&&e.owner=="icons")||(o.RestoreBackground&&e.owner=="background"))&&!plan.ContainsKey(e.path)).ToArray();
            foreach(var e in reverting.Where(e=>e.owner=="background"))if(Hash(SafePath(game,e.path))!=e.installed)throw new IOException("Файл изменился после установки: "+e.path+". Проверь файлы через Steam.");
            string backup=SafePath(game,BackupName);Directory.CreateDirectory(backup);
            var written=new List<KeyValuePair<string,byte[]>>();int step=0,total=plan.Count+reverting.Length+1;
            Action<string,byte[]> write=(path,data)=>{written.Add(new KeyValuePair<string,byte[]>(path,File.Exists(path)?File.ReadAllBytes(path):null));WriteBytes(path,data);};
            try {
                // Check every backup needed by a layout switch before making any changes.
                foreach(var e in reverting)if(Hash(SafePath(game,e.path))==e.installed&&e.existed&&Hash(SafePath(backup,e.path))!=e.original)throw new IOException("Резервная копия повреждена: "+e.path);
                foreach(var e in reverting){if(progress!=null)progress(++step,total,"Возвращаю "+Path.GetFileName(e.path));string target=SafePath(game,e.path);if(Hash(target)==e.installed)write(target,e.existed?File.ReadAllBytes(SafePath(backup,e.path)):null);state.Remove(e.path);}
                foreach(var p in plan.Values){
                    if(progress!=null)progress(++step,total,"Копирую "+Path.GetFileName(p.Rel));string target=SafePath(game,p.Rel);StateEntry e;
                    if(!state.TryGetValue(p.Rel,out e)){
                        e=new StateEntry{path=p.Rel,existed=File.Exists(target),original=Hash(target)};
                        if(e.existed)write(SafePath(backup,p.Rel),File.ReadAllBytes(target));state.Add(p.Rel,e);
                    } else if(Hash(target)!=e.installed) {throw new IOException("Файл изменился после установки: "+p.Rel+". Сначала верни оригинал или проверь файлы через Steam.");}
                    byte[] data=p.Bytes??File.ReadAllBytes(p.Source);write(target,data);e.installed=Hash(target);e.owner=p.Owner;
                    if(e.installed!=HashBytes(data))throw new IOException("Не удалось записать "+p.Rel);
                }
                if(progress!=null)progress(++step,total,"Сохраняю резервную копию");
                write(Path.Combine(backup,"state.json"),Encoding.UTF8.GetBytes(json.Serialize(state.Values.OrderBy(e=>e.path).ToArray())));
                if(o.Hud)write(Path.Combine(backup,"hud-profile.json"),Encoding.UTF8.GetBytes(json.Serialize(new HudProfile{layout=o.Layout,hash=Hash(SafePath(game,@"data\ui\screens\hud.lua")),customization=customization,clickThrough=o.ClickThrough})));
                if(o.Crosshair!=null)write(Path.Combine(backup,"crosshair.json"),Encoding.UTF8.GetBytes(json.Serialize(new{id=o.Crosshair,size=o.Size,color=crossColor,hash=Hash(SafePath(game,CrossFiles[0]))})));
                if(o.Icons.Count>0)write(Path.Combine(backup,"icons.json"),Encoding.UTF8.GetBytes(json.Serialize(o.Icons)));
            } catch(Exception failure) {
                var errors=new List<Exception>{failure};
                foreach(var w in written.AsEnumerable().Reverse())try{WriteBytes(w.Key,w.Value);}catch(Exception e){errors.Add(e);}
                if(errors.Count>1)throw new AggregateException("Ошибка установки и отката. Резервная копия сохранена.",errors);
                throw;
            }
        }
        static void WriteBytes(string path,byte[] bytes) {if(bytes==null){if(File.Exists(path))File.Delete(path);}else{Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,bytes);}}
        public List<string> Restore(string game,Action<int,int,string> progress) {
            CheckGame(game);var state=ReadState(game);if(state.Count==0)throw new IOException("Резервной копии нет: пак сюда не ставился.");
            string backup=SafePath(game,BackupName);var skipped=new List<string>();
            var restore=new List<StateEntry>();
            foreach(var e in state.Values){if(Hash(SafePath(game,e.path))!=e.installed){skipped.Add(e.path);continue;}if(e.existed&&Hash(SafePath(backup,e.path))!=e.original)throw new IOException("Копия "+e.path+" повреждена, остановился.");restore.Add(e);}
            var written=new List<KeyValuePair<string,byte[]>>();int step=0;
            try {
                foreach(var e in restore){if(progress!=null)progress(++step,restore.Count+1,"Возвращаю "+Path.GetFileName(e.path));string target=SafePath(game,e.path);written.Add(new KeyValuePair<string,byte[]>(target,File.Exists(target)?File.ReadAllBytes(target):null));WriteBytes(target,e.existed?File.ReadAllBytes(SafePath(backup,e.path)):null);}
                if(progress!=null)progress(++step,restore.Count+1,"Сохраняю архив резервной копии");
                Directory.Move(backup,backup+"-restored-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
            } catch {foreach(var w in written.AsEnumerable().Reverse())WriteBytes(w.Key,w.Value);throw;}
            string custom=SafePath(game,TextureDir);if(Directory.Exists(custom)&&!Directory.EnumerateFileSystemEntries(custom).Any())Directory.Delete(custom);
            return skipped;
        }
    }
}
