using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Automation;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Web.Script.Serialization;

namespace FortsSetup {
    public sealed class ThemeAnimator {
        readonly Window window;
        readonly Border tint,card;
        readonly Image art;
        readonly bool noMotion;
        public string Current {get;private set;}
        static readonly string[] Keys={"Accent","Group","Separator","Tint","Card"};
        static readonly string[] Normal={"#0A7AFF","#F5F5F7","#E5E5EA","#B8F4F4F7","#FFFFFF"};
        static readonly string[] Purple={"#9D4DFF","#F1E9FD","#E1D3F5","#A6EDE3FB","#FDFBFF"};
        public ThemeAnimator(Window w,Border t,Border c,Image a,bool reduced) {window=w;tint=t;card=c;art=a;noMotion=reduced;Current="default";}
        public void Apply(bool purple) {
            string name=purple?"zors":"default";if(Current==name)return;Current=name;
            var colors=purple?Purple:Normal;
            for(int i=0;i<Keys.Length;i++) {
                var old=i<3?(SolidColorBrush)window.Resources[Keys[i]]:(SolidColorBrush)(i==3?tint.Background:card.Background);
                Color from=old.Color,to=(Color)ColorConverter.ConvertFromString(colors[i]);
                var brush=new SolidColorBrush(noMotion?to:from);
                if(!noMotion) {
                    var anim=new ColorAnimation(from,to,TimeSpan.FromMilliseconds(420)) {EasingFunction=new CubicEase{EasingMode=EasingMode.EaseInOut}};
                    // Attach the clock before assigning: template optimization cannot freeze an animated brush.
                    brush.BeginAnimation(SolidColorBrush.ColorProperty,anim,HandoffBehavior.SnapshotAndReplace);
                }
                if(i<3)window.Resources[Keys[i]]=brush;else if(i==3)tint.Background=brush;else card.Background=brush;
            }
            art.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(purple?0.30:0,TimeSpan.FromMilliseconds(noMotion?0:480)),HandoffBehavior.SnapshotAndReplace);
        }
    }

    public sealed class WindowController {
        public readonly Window Window;
        public readonly ThemeAnimator Theme;
        public Task Ready {get;private set;}
        public string Layout {get;private set;}
        public string Page {get;private set;}
        public PackStatus Installed {get;private set;}
        readonly Engine engine;
        readonly string assets,lang;
        string game,skin="dlc3",cross="cross",color="white",updateUrl,lastPage="HudPage";
        readonly bool noMotion;
        bool initializing=true,busy,statusReady;
        int refreshVersion,galleryVersion;
        readonly Dictionary<string,string> english;
        readonly Dictionary<string,RadioButton> layouts=new Dictionary<string,RadioButton>(),skins=new Dictionary<string,RadioButton>(),tiles=new Dictionary<string,RadioButton>(),swatches=new Dictionary<string,RadioButton>();
        readonly Dictionary<string,Slider> iconSliders=new Dictionary<string,Slider>();
        readonly Dictionary<string,Image> iconImages=new Dictionary<string,Image>(),iconPreviews=new Dictionary<string,Image>();
        readonly Dictionary<string,TextBlock> iconValues=new Dictionary<string,TextBlock>();
        static readonly ConcurrentDictionary<string,BitmapSource> Bitmaps=new ConcurrentDictionary<string,BitmapSource>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string,string> layoutNames=new Dictionary<string,string>{{"compact","Компактный"},{"zors","Zors.o"},{"minimalist","Minimalist"}};
        readonly Dictionary<string,string> crossNames=new Dictionary<string,string>{{"cross","Крест"},{"gap_cross","Разрыв"},{"fine_cross","Тонкий"},{"dot_ring","Кольцо"},{"brackets","Рамка"},{"scope","Оптика"},{"zors","Zors.o"}};
        readonly Dictionary<string,string> skinNames=new Dictionary<string,string>{{"dlc3","Firebird"},{"base","По умолчанию"},{"dlc1","High Seas"},{"dlc2","Луноход"},{"pro","PRO"},{"elite","Elite"}};
        readonly Dictionary<string,string> iconNames=new Dictionary<string,string>{{"extinguisher","Тушение"},{"under_construction","Строительство"},{"repair","Ремонт"}};
        readonly Dictionary<string,string> colorNames=new Dictionary<string,string>{{"white","Белый"},{"green","Зелёный"},{"yellow","Жёлтый"},{"red","Красный"},{"cyan","Бирюзовый"}};
        string[] gallery=new string[0];int shotIndex;
        public T UI<T>(string name) where T:class {return (T)Window.FindName(name);}
        string L(string text) {string value;return lang=="en"&&english.TryGetValue(text,out value)?value:text;}
        void Text(string name,string value) {UI<TextBlock>(name).Text=value;}
        void Visible(string name,bool value) {UI<UIElement>(name).Visibility=value?Visibility.Visible:Visibility.Collapsed;}
        bool On(string name) {return UI<ToggleButton>(name).IsChecked==true;}
        public static BitmapSource Bitmap(string path) {return Bitmaps.GetOrAdd(path,p=>{var b=new BitmapImage();b.BeginInit();b.CacheOption=BitmapCacheOption.OnLoad;b.UriSource=new Uri(p);b.EndInit();b.Freeze();return b;});}
        BitmapSource Image(string name) {return Bitmap(Path.Combine(assets,name));}
        void Animate(DependencyObject target,DependencyProperty property,double to,int ms) {
            var animation=new DoubleAnimation(to,TimeSpan.FromMilliseconds(noMotion?0:ms)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}};
            var element=target as UIElement;if(element!=null)element.BeginAnimation(property,animation,HandoffBehavior.SnapshotAndReplace);
            else ((Animatable)target).BeginAnimation(property,animation,HandoffBehavior.SnapshotAndReplace);
        }
        RadioButton Choice(Panel parent,string style,string title,string group,string id,Action select) {
            var b=new RadioButton {Style=(Style)Window.FindResource(style),Content=title,GroupName=group,Tag=id};
            AutomationProperties.SetName(b,group+": "+title);AutomationProperties.SetAutomationId(b,group+"-"+id);
            b.Checked+=(s,e)=>select();parent.Children.Add(b);return b;
        }
        public WindowController(string packRoot,string gamePath,string language,bool reduced,Options selection=null) {
            engine=new Engine(packRoot);assets=Path.Combine(packRoot,"app","assets");game=gamePath;lang=language;noMotion=reduced||!SystemParameters.ClientAreaAnimation;
            string strings=Path.Combine(packRoot,"app","en.json");english=File.Exists(strings)?new JavaScriptSerializer().Deserialize<Dictionary<string,string>>(File.ReadAllText(strings)):new Dictionary<string,string>();
            string xaml=String.Join("\n",Directory.GetFiles(Path.Combine(packRoot,"app","xaml"),"*.xaml").OrderBy(p=>p).Select(File.ReadAllText)).Replace("{SPRING}",noMotion?"0:0:0":"0:0:0.32");
            var xml=new XmlDocument();xml.LoadXml(xaml);
            if(lang=="en")foreach(XmlElement element in xml.SelectNodes("//*"))foreach(XmlAttribute a in element.Attributes)if(english.ContainsKey(a.Value))a.Value=L(a.Value);
            using(var reader=new XmlNodeReader(xml))Window=(Window)XamlReader.Load(reader);
            // The controller is constructed before Application.Run, which normally installs
            // this context. Early asynchronous gallery loads must also resume on the UI thread.
            if(!(SynchronizationContext.Current is DispatcherSynchronizationContext))
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Window.Dispatcher));
            Theme=new ThemeAnimator(Window,UI<Border>("Tint"),UI<Border>("Card"),UI<Image>("ThemeArt"),noMotion);
            Window.SourceInitialized+=(s,e)=>Glass.Apply(Window);
            UI<Image>("ThemeArt").Source=Image("theme_zors.jpg");UI<Image>("BeforeImage").Source=Image("crosshair_vanilla.png");UI<Image>("IconScene").Source=Image("scene.png");
            foreach(string id in Engine.LayoutIds){string key=id;layouts[id]=Choice(UI<StackPanel>("LayoutPicker"),"LayoutSeg",L(layoutNames[id]),"HUD",id,()=>SelectLayout(key));}
            foreach(string id in Engine.SkinIds){string key=id;var b=Choice(UI<StackPanel>("Skins"),"SkinChip",L(skinNames[id]),"Skin",id,()=>{skin=key;UpdateSkinNote();});b.Tag=Image("skin_"+id+".png");b.ToolTip=L(skinNames[id]);skins[id]=b;}
            foreach(var pair in new Dictionary<string,string>{{"scene","Бой"},{"bg_sky","Небо"},{"bg_rocks","Скалы"},{"bg_fire","Огонь"}}){string key=pair.Key;var b=Choice(UI<StackPanel>("Scenes"),"SceneSeg",L(pair.Value),"Scene",key,()=>{UI<Image>("SceneBefore").Source=Image(key+".png");UI<Image>("SceneAfter").Source=Image(key+".png");});if(key=="scene")b.IsChecked=true;}
            foreach(var pair in crossNames){if(!engine.Crosshairs.Contains(pair.Key))continue;string key=pair.Key;var b=Choice(UI<WrapPanel>("Tiles"),"Tile",L(pair.Value),"Crosshair",key,()=>{cross=key;UpdateCross();UpdateSummary();});b.Tag=Image("crosshair_"+key+".png");tiles[key]=b;}
            foreach(var pair in Engine.Colors){string key=pair.Key;var b=Choice(UI<StackPanel>("Swatches"),"Swatch",null,"Colour",key,()=>{color=key;UpdateCross();});b.Background=(Brush)new BrushConverter().ConvertFrom("#"+pair.Value);b.ToolTip=L(colorNames[key]);AutomationProperties.SetName(b,L("Цвет: ")+L(colorNames[key]));swatches[key]=b;}
            MakeIcons();
            foreach(string nav in new[]{"NavHud","NavCross","NavIcons","NavBackup"}) {var b=UI<RadioButton>(nav);b.Checked+=(s,e)=>ShowPage((string)b.Tag);}
            UI<Slider>("SizeSlider").ValueChanged+=(s,e)=>UpdateCross();
            UI<CheckBox>("HudSwitch").Click+=(s,e)=>UpdateSummary();UI<CheckBox>("CrossSwitch").Click+=(s,e)=>UpdateCross();UI<CheckBox>("IntroSwitch").Click+=(s,e)=>UpdateSummary();
            UI<Button>("IconsReset").Click+=(s,e)=>{foreach(var slider in iconSliders.Values)slider.Value=100;};
            UI<Button>("InstallBtn").Click+=async(s,e)=>{if(Page=="HudPage")UI<RadioButton>("NavCross").IsChecked=true;else if(Page=="CrossPage")UI<RadioButton>("NavIcons").IsChecked=true;else await RunInstall(false);};
            UI<Button>("RestoreBtn").Click+=async(s,e)=>await RunInstall(true);
            UI<Button>("DoneBtn").Click+=(s,e)=>Window.Close();UI<Button>("CloseLight").Click+=(s,e)=>Window.Close();UI<Button>("MinLight").Click+=(s,e)=>Window.WindowState=WindowState.Minimized;
            UI<Button>("BackBtn").Click+=async(s,e)=>{ShowPage(lastPage);Visible("BackBtn",false);Visible("DoneBtn",false);UI<StackPanel>("Nav").IsEnabled=true;UI<Button>("ChangeFolder").IsEnabled=true;Ready=Refresh();await Ready;};
            UI<Button>("ChangeFolder").Click+=async(s,e)=>{using(var dialog=new System.Windows.Forms.FolderBrowserDialog{SelectedPath=game??"",Description=L("Выбрать папку игры")})if(dialog.ShowDialog()==System.Windows.Forms.DialogResult.OK){game=dialog.SelectedPath;Ready=Refresh();await Ready;}};
            UI<Button>("FullScreenBtn").Click+=(s,e)=>OpenViewer();UI<Border>("HudShot").MouseLeftButtonUp+=(s,e)=>OpenViewer();
            UI<Button>("LayoutLink").Click+=(s,e)=>OpenUrl(Layout=="zors"?"https://steamcommunity.com/profiles/76561199099877561":"https://steamcommunity.com/sharedfiles/filedetails/?id=3750502564");
            UI<Button>("UpdateLink").Click+=(s,e)=>{if(updateUrl!=null)OpenUrl(updateUrl);};
            UI<RadioButton>(lang=="en"?"LangEn":"LangRu").IsChecked=true;
            UI<RadioButton>("LangEn").Click+=(s,e)=>SwitchLanguage("en");UI<RadioButton>("LangRu").Click+=(s,e)=>SwitchLanguage("ru");
            Window.KeyDown+=(s,e)=>{if(e.Key==Key.Escape)Window.Close();};
            Window.Closing+=(s,e)=>{if(busy){e.Cancel=true;Text("WorkText",L("Устанавливаю…"));}};
            swatches[selection==null?"white":selection.Color].IsChecked=true;
            tiles[selection==null?"cross":selection.Crosshair??"cross"].IsChecked=true;
            UI<Slider>("SizeSlider").Value=selection==null?100:selection.Size;
            layouts[selection==null?"compact":selection.Layout].IsChecked=true;
            UI<RadioButton>("NavHud").IsChecked=true;
            initializing=false;
            Window.Loaded+=async(s,e)=>{Animate(UI<UIElement>("Shell"),UIElement.OpacityProperty,1,220);Ready=Refresh();await Ready;if(selection!=null){UI<CheckBox>("HudSwitch").IsChecked=selection.Hud;UI<CheckBox>("CrossSwitch").IsChecked=selection.Crosshair!=null;UI<CheckBox>("IntroSwitch").IsChecked=selection.SkipIntro;foreach(var p in selection.Icons)iconSliders[p.Key].Value=p.Value;UpdateCross();}CheckUpdates();};
        }
        void MakeIcons() {
            foreach(var pair in iconNames){string id=pair.Key;var row=new Grid{Margin=new Thickness(16,6,16,6)};foreach(double width in new[]{40,120,0,46})row.ColumnDefinitions.Add(new ColumnDefinition{Width=width==0?new GridLength(1,GridUnitType.Star):new GridLength(width)});
                var image=new Image{Width=30,Height=30};row.Children.Add(image);iconImages[id]=image;
                var label=new TextBlock{Text=L(pair.Value),FontWeight=FontWeights.Medium,VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(label,1);row.Children.Add(label);
                var slider=new Slider{Minimum=10,Maximum=100,Value=100,TickFrequency=5,IsSnapToTickEnabled=true,Height=36,Style=(Style)Window.FindResource("SizeRange")};Grid.SetColumn(slider,2);row.Children.Add(slider);AutomationProperties.SetName(slider,L(pair.Value));AutomationProperties.SetAutomationId(slider,"Opacity-"+id);iconSliders[id]=slider;
                var value=new TextBlock{Text="100%",TextAlignment=TextAlignment.Right,VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(value,3);row.Children.Add(value);iconValues[id]=value;
                UI<StackPanel>("IconRows").Children.Add(row);var preview=new Image{Width=54,Height=54,Margin=new Thickness(14,0,14,0)};UI<StackPanel>("IconPreview").Children.Add(preview);iconPreviews[id]=preview;
                slider.ValueChanged+=(s,e)=>{value.Text=((int)slider.Value)+"%";preview.Opacity=slider.Value/100;image.Opacity=Math.Max(slider.Value/100,0.35);UpdateSummary();};
            }
        }
        public void SelectLayout(string key) {
            if(layouts[key].IsChecked!=true){layouts[key].IsChecked=true;return;}
            Layout=key;Theme.Apply(key=="zors");
            Text("LayoutTitle",L(key=="compact"?"Компактный · наш":key=="zors"?"Zors.o · фиолетовая тема":"Minimalist · автор nutellafish"));
            Text("LayoutText",L(key=="compact"?"Ресурсы «запас / лимит» по центру, кнопки в один ряд, иконки на 20% меньше.":key=="zors"?"Наш компактный HUD в космическом фиолетовом: кнопки, вкладки, командир, реактор, иконки и прицел; фон меню и заставка от Zors.o.":"Прозрачный минимальный интерфейс; убранные кнопки нажимаются горячими клавишами."));
            Visible("SkinRow",key=="compact");Visible("SkinSep",key=="compact");Visible("ZorsWarn",key=="zors");Visible("MiniWarn",key=="minimalist");Visible("LayoutLink",key!="compact");
            UI<Button>("LayoutLink").Content=L(key=="zors"?"Профиль Steam":"Мастерская Steam");UpdateSkinNote();
            foreach(string id in Engine.IconIds){var bmp=Image("icon_"+key+"_"+id+".png");iconImages[id].Source=bmp;iconPreviews[id].Source=bmp;}
            if(!initializing&&key!="compact") {UI<CheckBox>("CrossSwitch").IsChecked=key=="zors";if(key=="zors")tiles["zors"].IsChecked=true;}
            Text("HudNote",L(Installed!=null&&Installed.Layout==key?"Уже стоит. Переустановка ничего не сломает":"Оригиналы сохраняются, вернуть можно одной кнопкой"));
            LoadGallery(key);UpdateCross();
        }
        void UpdateSkinNote() {
            Text("SkinNote",L(skinNames[skin]));
            string current=Engine.GameSkin(game,false),menu=Engine.GameSkin(game,true);
            Text("ZorsHudText",current!=null&&current!="base"?L("• Нужен скин HUD «По умолчанию» (Настройки → Игра → Скин HUD)."):L("• Скин HUD «По умолчанию» — как нужно: вкладки, командир и реактор будут фиолетовыми."));
            Text("ZorsMenuText",new[]{"dlc1","dlc2","dlc3"}.Contains(menu)?L("• Фон меню Zors.o виден только со скинами «По умолчанию», «Классика» и «Склад оружия». Выбери один из них в настройках игры."):L("• Фон меню виден со скинами меню «По умолчанию», «Классика» и «Склад оружия»."));
            Text("MiniSkin",L("• Нужен скин HUD «По умолчанию» (Настройки → Игра → Скин HUD)."));
        }
        async void LoadGallery(string key) {
            int version=++galleryVersion;
            string dir=Path.Combine(assets,"gallery",key);
            gallery=Directory.Exists(dir)?Directory.GetFiles(dir).Where(p=>new[]{".jpg",".png"}.Contains(Path.GetExtension(p).ToLowerInvariant())).OrderBy(p=>p).ToArray():new string[0];
            if(gallery.Length==0)gallery=(key=="compact"?new[]{"hud_after.png","hud_photo_2.png","hud_photo_3.png"}:new[]{key=="zors"?"preview_zors.png":"minimalist_preview.jpg"}).Select(p=>Path.Combine(assets,p)).ToArray();
            UI<StackPanel>("Photos").Children.Clear();Visible("PhotosBox",gallery.Length>1);
            for(int i=0;i<gallery.Length;i++){int n=i;Choice(UI<StackPanel>("Photos"),"SceneSeg",(i+1).ToString(),"Photo",i.ToString(),()=>ShowShot(n));}
            try {string path=gallery[0];await Task.Run(()=>Bitmap(path));if(version!=galleryVersion)return;UI<RadioButton>("NavHud").Dispatcher.VerifyAccess();((RadioButton)UI<StackPanel>("Photos").Children[0]).IsChecked=true;}
            catch(Exception e){if(version==galleryVersion)Text("ShotCaption",e.Message);}
        }
        async void ShowShot(int index) {
            if(index<0||index>=gallery.Length)return;shotIndex=index;int version=galleryVersion;string path=gallery[index];
            try {var bmp=await Task.Run(()=>Bitmap(path));if(version!=galleryVersion||shotIndex!=index)return;
                var brush=new ImageBrush(bmp){Stretch=Stretch.UniformToFill};if(bmp.PixelWidth/(double)bmp.PixelHeight<3)brush.Viewbox=new Rect(0,0.66,1,0.34);UI<Border>("HudShot").Background=brush;
                string caption=System.Text.RegularExpressions.Regex.Replace(Path.GetFileNameWithoutExtension(path),@"^\d+\s*","");Text("ShotCaption",caption);Visible("ShotCaptionBox",Path.GetDirectoryName(path)!=assets);
            }catch(Exception e){Text("ShotCaption",e.Message);}
        }
        void OpenViewer() {
            if(gallery.Length==0)return;int index=shotIndex;var image=new Image{Stretch=Stretch.Uniform};var viewer=new Window{Owner=Window,Title="Forts Setup — "+L(layoutNames[Layout]),WindowState=WindowState.Maximized,WindowStyle=WindowStyle.None,Background=Brushes.Black,Content=image,ShowInTaskbar=false};
            Action show=()=>image.Source=Bitmap(gallery[index]);show();viewer.KeyDown+=(s,e)=>{if(e.Key==Key.Escape)viewer.Close();else if(e.Key==Key.Left||e.Key==Key.Right){index=(index+(e.Key==Key.Left?-1:1)+gallery.Length)%gallery.Length;show();}};image.MouseLeftButtonUp+=(s,e)=>{index=(index+1)%gallery.Length;show();};viewer.ShowDialog();
        }
        BitmapSource Tinted(BitmapSource bmp,string hex) {
            if(hex=="FFFFFF")return bmp;var src=new FormatConvertedBitmap(bmp,PixelFormats.Bgra32,null,0);int stride=src.PixelWidth*4;byte[] px=new byte[stride*src.PixelHeight];src.CopyPixels(px,stride,0);
            int r=Convert.ToInt32(hex.Substring(0,2),16),g=Convert.ToInt32(hex.Substring(2,2),16),b=Convert.ToInt32(hex.Substring(4,2),16);for(int i=0;i<px.Length;i+=4){px[i]=(byte)(px[i]*b/255);px[i+1]=(byte)(px[i+1]*g/255);px[i+2]=(byte)(px[i+2]*r/255);}var result=BitmapSource.Create(src.PixelWidth,src.PixelHeight,96,96,PixelFormats.Bgra32,null,px,stride);result.Freeze();return result;
        }
        void UpdateCross() {
            if(cross==null||Layout==null)return;int size=(int)UI<Slider>("SizeSlider").Value;Text("SizeValue",size+"%");
            var image=UI<Image>("AfterImage");bool enabled=On("CrossSwitch");image.Width=image.Height=enabled?128:64;
            image.Source=enabled?Tinted(Image("crosshair_"+cross+"_"+size+".png"),Engine.Colors[cross=="zors"?"white":color]):Image("crosshair_vanilla.png");
            Text("AfterLabel",enabled?L(crossNames[cross])+" · "+size+"%":L("Не меняется"));UpdateSummary();
        }
        public Options Selection() {return new Options{Hud=On("HudSwitch"),Layout=Layout,Skin=skin,Crosshair=On("CrossSwitch")?cross:null,Color=cross=="zors"?"white":color,Size=(int)UI<Slider>("SizeSlider").Value,Icons=iconSliders.ToDictionary(p=>p.Key,p=>(int)p.Value.Value),SkipIntro=On("IntroSwitch")};}
        public void ShowPage(string name) {
            Page=name;foreach(string page in new[]{"HudPage","CrossPage","IconsPage","BackupPage","WorkPage"})Visible(page,page==name);
            if(name!="WorkPage"){
                bool backup=name=="BackupPage";UI<Button>("InstallBtn").Content=L(name=="HudPage"||name=="CrossPage"?"Далее":"Установить");Visible("InstallBtn",!backup);Visible("RestoreBtn",backup);UI<Button>("InstallBtn").IsDefault=!backup;UI<Button>("RestoreBtn").IsDefault=backup;UI<TextBlock>("Summary").Opacity=backup?0:1;
                foreach(string nav in new[]{"NavHud","NavCross","NavIcons","NavBackup"}){var item=UI<RadioButton>(nav);if((string)item.Tag==name){Window.UpdateLayout();Animate(UI<TranslateTransform>("NavPillY"),TranslateTransform.YProperty,item.TranslatePoint(new Point(0,0),UI<StackPanel>("Nav")).Y,220);break;}}
            }
            UpdateSummary();
        }
        void UpdateSummary() {
            if(Layout==null||iconSliders.Count!=3)return;
            var o=Selection();var parts=new List<string>();if(o.Hud)parts.Add("HUD «"+L(layoutNames[Layout])+"»");if(o.Crosshair!=null)parts.Add(L("Прицел")+" «"+L(crossNames[cross])+"» "+o.Size+"%");
            foreach(var p in o.Icons)if(p.Value<100)parts.Add(L(iconNames[p.Key])+" "+p.Value+"%");if(Installed!=null&&o.SkipIntro!=Installed.SkipIntro)parts.Add(L(o.SkipIntro==true?"без заставки":"с заставкой"));
            Text("Summary",parts.Count>0?L("Будет установлено: ")+String.Join("; ",parts):L("Ничего не выбрано"));Text("NavHudValue",o.Hud?L(layoutNames[Layout]):L("Выкл."));Text("NavCrossValue",o.Crosshair==null?L("Выкл."):L(crossNames[cross]));Text("NavIconsValue",o.Icons.Count(p=>p.Value<100)>0?o.Icons.Count(p=>p.Value<100)+" изм.":"");
            UI<WrapPanel>("Tiles").IsEnabled=o.Crosshair!=null;UI<Grid>("SizeControls").IsEnabled=o.Crosshair!=null;UI<Grid>("ColourControls").IsEnabled=o.Crosshair!=null&&cross!="zors";UI<StackPanel>("Skins").IsEnabled=o.Hud;
            bool changes=Installed!=null&&(o.Icons.Any(p=>p.Value!=Installed.Icons[p.Key])||o.SkipIntro!=Installed.SkipIntro);
            UI<Button>("InstallBtn").IsEnabled=!busy&&statusReady&&Engine.IsGame(game)&&(o.Hud||o.Crosshair!=null||changes||Page!="IconsPage");
        }
        async Task Refresh() {
            int version=++refreshVersion;statusReady=false;UpdateSummary();
            Text("GamePath",game??L("Forts не найден"));UI<TextBlock>("GamePath").ToolTip=game;
            if(!Engine.IsGame(game)){Visible("Status",true);Text("Status",L("Нажми «Изменить…» слева и выбери папку, где лежит Forts.exe."));UI<Button>("RestoreBtn").IsEnabled=false;return;}
            try {string selectedGame=game;var status=await Task.Run(()=>engine.Status(selectedGame));if(version!=refreshVersion)return;Installed=status;statusReady=true;
                UI<CheckBox>("HudSwitch").IsEnabled=status.HudCompatible;if(!status.HudCompatible)UI<CheckBox>("HudSwitch").IsChecked=false;
                Visible("Status",!status.HudCompatible);Text("Status",status.HudCompatible?"":L("Не подходит к этой версии игры"));
                string detected=Engine.GameSkin(game,false)??status.Skin??"dlc3";if(skins.ContainsKey(detected))skins[detected].IsChecked=true;
                // Installed state is informational; never change the user's selected layout/theme here.
                foreach(var p in status.Icons)iconSliders[p.Key].Value=p.Value;UI<CheckBox>("IntroSwitch").IsChecked=status.SkipIntro;
                if(initializing)return;
                Text("HudNote",L(status.Layout==Layout?"Уже стоит. Переустановка ничего не сломает":"Оригиналы сохраняются, вернуть можно одной кнопкой"));
                Text("BackupState",L(status.HasBackup?"есть, файлы игры сохранены":"нет — пак сюда ещё не ставился"));Text("BackupPath",Path.Combine(game,Engine.BackupName));Text("NavBackupValue",status.HasBackup?L("Есть"):"");UI<Button>("RestoreBtn").IsEnabled=status.HasBackup&&!busy;
                string hardware=Engine.Settings(game,"settings_hardware.lua");string hardwareText=hardware==null?"":File.ReadAllText(hardware);var w=System.Text.RegularExpressions.Regex.Match(hardwareText,@"ViewWidth\s*=\s*(\d+)");var h=System.Text.RegularExpressions.Regex.Match(hardwareText,@"ViewHeight\s*=\s*(\d+)");
                int width,height;bool known=Int32.TryParse(w.Groups[1].Value,out width)&Int32.TryParse(h.Groups[1].Value,out height);
                if(hardware!=null&&System.Text.RegularExpressions.Regex.IsMatch(hardwareText,@"AutoResolution\s*=\s*true")){var bounds=System.Windows.Forms.Screen.PrimaryScreen.Bounds;width=bounds.Width;height=bounds.Height;known=true;}
                bool wide=known&&height>0&&Math.Abs(width/(double)height-16.0/9)<0.02;
                Text("ScreenText",known&&height>0?String.Format(L(wide?"{0}×{1} · 16:9, подходит":"{0}×{1} — не 16:9, ресурсы сместятся"),width,height):L("не удалось узнать — HUD рассчитан на 16:9"));
                UI<System.Windows.Shapes.Ellipse>("ScreenDot").Fill=new SolidColorBrush(!known?Color.FromRgb(199,199,204):wide?Color.FromRgb(52,199,89):Color.FromRgb(255,159,10));UpdateSkinNote();UpdateSummary();
            } catch(Exception e){if(version==refreshVersion){Visible("Status",true);Text("Status",e.Message);}}
        }
        async Task RunInstall(bool restore) {
            if(busy)return;Options options=Selection();lastPage=Page;busy=true;ShowPage("WorkPage");Visible("InstallBtn",false);Visible("RestoreBtn",false);Visible("DoneBtn",false);Visible("BackBtn",false);Visible("Bar",true);Visible("ErrorCircle",false);Visible("ErrorMark",false);
            UI<StackPanel>("Nav").IsEnabled=false;UI<Button>("ChangeFolder").IsEnabled=false;UI<RadioButton>("LangRu").IsEnabled=false;UI<RadioButton>("LangEn").IsEnabled=false;
            UI<UIElement>("DoneCircle").BeginAnimation(UIElement.OpacityProperty,null);UI<UIElement>("DoneCircle").Opacity=0;UI<System.Windows.Shapes.Path>("DoneCheck").BeginAnimation(System.Windows.Shapes.Shape.StrokeDashOffsetProperty,null);UI<System.Windows.Shapes.Path>("DoneCheck").StrokeDashOffset=11;
            Text("WorkTitle",L(restore?"Возвращаю оригинал…":"Устанавливаю…"));Text("WorkText",L("Сохраняю оригинальные файлы игры"));UI<Border>("BarFill").Width=0;
            // Progress<T> captures WPF's SynchronizationContext; file IO stays on a worker thread.
            var progress=new Progress<Tuple<int,int,string>>(p=>{Text("WorkText",p.Item3);Animate(UI<Border>("BarFill"),FrameworkElement.WidthProperty,400.0*p.Item1/p.Item2,90);});
            Action<int,int,string> report=(step,total,text)=>((IProgress<Tuple<int,int,string>>)progress).Report(Tuple.Create(step,total,text));
            try {
                var skipped=await Task.Run(()=>{if(restore)return engine.Restore(game,report);engine.Install(game,options,report);return new List<string>();});
                Visible("Bar",false);Text("WorkTitle",L("Готово"));
                string message=L(restore?"Файлы игры снова такие, какими были до установки.":"Готово. Запускай Forts.");if(skipped.Count>0)message+="\n"+L("Не тронул изменившиеся после установки: ")+String.Join(", ",skipped);
                Text("WorkText",message);Animate(UI<UIElement>("DoneCircle"),UIElement.OpacityProperty,1,180);Animate(UI<System.Windows.Shapes.Path>("DoneCheck"),System.Windows.Shapes.Shape.StrokeDashOffsetProperty,0,260);
            } catch(Exception e){Visible("Bar",false);Visible("ErrorCircle",true);Visible("ErrorMark",true);Text("WorkTitle",L("Не получилось"));Text("WorkText",L(e.Message));Visible("BackBtn",true);}
            finally {busy=false;UI<RadioButton>("LangRu").IsEnabled=true;UI<RadioButton>("LangEn").IsEnabled=true;Visible("DoneBtn",true);UI<Button>("DoneBtn").Focus();}
        }
        void SwitchLanguage(string to) {if(lang==to||busy)return;var next=new WindowController(engine.PackRoot,game,to,noMotion,Selection());Application.Current.MainWindow=next.Window;next.Window.Show();Window.Close();}
        static void OpenUrl(string url) {Process.Start(new ProcessStartInfo(url){UseShellExecute=true});}
        async void CheckUpdates() {
            try {var data=new JavaScriptSerializer().Deserialize<Dictionary<string,string>>(File.ReadAllText(Path.Combine(engine.PackRoot,"app","version.json")));if(!data.ContainsKey("repo"))return;
                using(var http=new System.Net.Http.HttpClient{Timeout=TimeSpan.FromSeconds(6)}){http.DefaultRequestHeaders.UserAgent.ParseAdd("FortsSetup/"+data["version"]);string text=await http.GetStringAsync("https://api.github.com/repos/"+data["repo"]+"/releases/latest");var release=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(text);Version latest;string url=release["html_url"] as string;if(Version.TryParse(((string)release["tag_name"]).TrimStart('v'),out latest)&&latest>new Version(data["version"])&&url.StartsWith("https://github.com/",StringComparison.Ordinal)){updateUrl=url;Text("UpdateText",String.Format(L("Доступна версия {0}"),latest));Visible("Update",true);}}
            } catch(Exception e) {Trace.WriteLine("Update check: "+e.Message);}
        }
    }
}
