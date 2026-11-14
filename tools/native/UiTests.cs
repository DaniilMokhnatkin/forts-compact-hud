using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FortsSetup;

static class NativeUiTests {
    static int checks;
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
    static void Pump(int ms){var frame=new DispatcherFrame();var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(ms)};timer.Tick+=(s,e)=>{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);}
    static void Wait(Func<bool> predicate){var watch=Stopwatch.StartNew();while(!predicate()){if(watch.ElapsedMilliseconds>20000)throw new TimeoutException("UI task timed out");Pump(10);}}
    static void Render(Window window,string file){window.UpdateLayout();var b=new RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32);b.Render(window);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(b));using(var s=File.Create(file))png.Save(s);}
    [STAThread] public static int Main(string[] args){
        string root=Path.GetFullPath(args[0]),game=Path.Combine(root,"build","native-ui-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(game,@"data\ui\screens"));
        var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};WindowController c=null;
        try {
            File.WriteAllText(Path.Combine(game,"Forts.exe"),"fixture");
            foreach(string name in new[]{"hud.lua","transaction.lua"})File.Copy(Path.Combine(root,"original",name),Path.Combine(game,@"data\ui\screens",name));
            File.Copy(Path.Combine(root,"original","uisprites.lua"),Path.Combine(game,@"data\ui\uisprites.lua"));
            Directory.CreateDirectory(Path.Combine(game,@"data\ui\textures"));foreach(string name in new[]{"mouse_target.dds","mouse_target_painted.dds"})File.Copy(Path.Combine(root,@"textures\vanilla",name),Path.Combine(game,@"data\ui\textures",name));
            var engine=new Engine(Path.Combine(root,"setup"));engine.Install(game,new Options{Hud=true,Layout="zors"},null);
            c=new WindowController(Path.Combine(root,"setup"),game,"ru",false);c.Window.Show();Wait(()=>c.Ready!=null&&c.Ready.IsCompleted);Pump(100);
            Check(c.Installed.Layout=="zors","fixture really has Zors installed");
            Check(c.Layout=="compact"&&c.Theme.Current=="default","startup stays Compact/neutral even with Zors installed");
            Check(c.Window.OwnedWindows.Count==0,"startup does not open gallery");
            double normalWidth=c.Window.ActualWidth,normalHeight=c.Window.ActualHeight,normalLeft=c.Window.Left,normalTop=c.Window.Top;
            foreach(string nav in new[]{"NavHud","NavCross","NavIcons","NavBackground","NavBackup","NavAbout"}){c.UI<RadioButton>(nav).IsChecked=true;Pump(50);var scroll=(ScrollViewer)c.Window.FindName((string)c.UI<RadioButton>(nav).Tag);Check(scroll.ScrollableHeight<0.5,nav+" fits without unnecessary scrolling at the normal window size");Check(Math.Abs(c.Window.ActualWidth-normalWidth)<1&&Math.Abs(c.Window.ActualHeight-normalHeight)<1&&Math.Abs(c.Window.Left-normalLeft)<1&&Math.Abs(c.Window.Top-normalTop)<1,nav+" keeps the window size and position fixed");}
            c.UI<RadioButton>("NavHud").IsChecked=true;Pump(50);

            c.UI<RadioButton>("NavCross").IsChecked=true;Pump(400);c.Window.Height=600;Pump(100);c.UI<ScrollViewer>("CrossPage").ScrollToEnd();Pump(60);c.Window.UpdateLayout();
            var crossSize=c.UI<Grid>("SizeControls");var footer=c.UI<Border>("FooterRule");
            double controlsBottom=crossSize.TranslatePoint(new Point(0,crossSize.ActualHeight),c.Window).Y;
            double footerTop=footer.TranslatePoint(new Point(0,0),c.Window).Y;
            Check(controlsBottom<=footerTop,"crosshair size controls remain above the footer in a short window");
            c.Window.Height=780;c.UI<RadioButton>("NavHud").IsChecked=true;Pump(60);
            c.UI<RadioButton>("NavBackground").IsChecked=true;
            Check(c.UI<ScrollViewer>("BackgroundPage").Visibility==Visibility.Visible&&!c.UI<Button>("InstallBtn").IsEnabled,"background page starts without a pending operation");
            Check(c.UI<Panel>("BackgroundPresets").Children.Count==3,"three built-in background sets are available");
            foreach(string id in BackgroundImage.PresetIds){
                c.UI<Panel>("BackgroundPresets").Children.OfType<Button>().Single(button=>(string)button.Tag==id).RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));var choice=c.Selection();
                Check(choice.MenuBackground.SequenceEqual(BackgroundImage.Encode(BackgroundImage.PresetFile(Path.Combine(root,@"setup\app\assets"),id,false)))&&choice.ScreensBackground.SequenceEqual(BackgroundImage.Encode(BackgroundImage.PresetFile(Path.Combine(root,@"setup\app\assets"),id,true))),id+" selects the bundled main and other-screen art without a file picker");
            }
            Pump(300);Render(c.Window,Path.Combine(root,@"build\native-background-presets.png"));
            Check(!c.Selection().BackgroundLogo,"Forts logo is off by default so existing artwork is not duplicated");
            c.UI<CheckBox>("BackgroundLogoSwitch").IsChecked=true;c.UI<CheckBox>("BackgroundLogoSwitch").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            var selectedImage=(BitmapSource)c.UI<Image>("MenuBackgroundPreview").Source;
            var logoPreview=new byte[selectedImage.PixelWidth*selectedImage.PixelHeight*4];selectedImage.CopyPixels(logoPreview,selectedImage.PixelWidth*4,0);
            Check(!logoPreview.SequenceEqual(c.Selection().MenuBackground.Skip(128)),"main-menu preview includes the selected Forts logo");
            c.UI<CheckBox>("BackgroundLogoSwitch").IsChecked=false;c.UI<CheckBox>("BackgroundLogoSwitch").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            ((BitmapSource)c.UI<Image>("MenuBackgroundPreview").Source).CopyPixels(logoPreview,selectedImage.PixelWidth*4,0);
            Check(logoPreview.SequenceEqual(c.Selection().MenuBackground.Skip(128)),"disabling the logo returns the clean image without changing the source");
            c.Window.Height=600;c.Window.UpdateLayout();var backgroundPage=c.UI<ScrollViewer>("BackgroundPage");backgroundPage.ScrollToEnd();Pump(60);
            double resetBottom=c.UI<CheckBox>("RestoreBackgroundSwitch").TranslatePoint(new Point(0,c.UI<CheckBox>("RestoreBackgroundSwitch").ActualHeight),backgroundPage).Y;
            Check(backgroundPage.ScrollableHeight>0&&resetBottom>0&&resetBottom<=backgroundPage.ActualHeight,"background restore remains reachable in a short window");
            c.Window.Height=780;backgroundPage.ScrollToHome();Pump(60);
            var background=BackgroundImage.Encode(Path.Combine(root,@"setup\app\assets\scene.png"));
            c.SetBackground(false,background,"scene.png");
            Check(c.UI<Button>("InstallBtn").IsEnabled&&c.Selection().MenuBackground==background&&c.UI<Image>("MenuBackgroundPreview").Source!=null,"selected background enables installation and shows converted preview");
            Pump(300);Render(c.Window,Path.Combine(root,@"build\native-background.png"));
            c.SetBackground(true,background,"clean.png");
            var preserved=new WindowController(Path.Combine(root,"setup"),game,"en",true,c.Selection());
            preserved.Window.Show();Wait(()=>preserved.Ready!=null&&preserved.Ready.IsCompleted);
            preserved.UI<RadioButton>("NavBackground").IsChecked=true;
            Check(preserved.Selection().MenuBackground==background&&preserved.Selection().ScreensBackground==background&&preserved.UI<TextBlock>("MenuBackgroundName").Text=="Selected image","language change preserves both selected images");
            Check(!preserved.Selection().BackgroundLogo,"language change preserves the logo switch");
            preserved.UI<CheckBox>("BackgroundLogoSwitch").IsChecked=true;preserved.UI<CheckBox>("BackgroundLogoSwitch").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Pump(50);Render(preserved.Window,Path.Combine(root,@"build\native-background-en.png"));
            string hudBeforeBackground=Engine.Hash(Path.Combine(game,@"data\ui\screens\hud.lua"));
            preserved.UI<Button>("InstallBtn").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Wait(()=>preserved.UI<Button>("DoneBtn").Visibility==Visibility.Visible);
            Check(preserved.UI<TextBlock>("WorkTitle").Text=="Done"&&Engine.Hash(Path.Combine(game,@"data\ui\screens\hud.lua"))==hudBeforeBackground&&engine.Status(game).MenuBackground,"background page installs only art even with HUD selected elsewhere");
            Check(File.ReadAllBytes(Path.Combine(game,@"data\ui\textures\menu_backgrounds\base\FE-MainMenu-BG.dds")).SequenceEqual(BackgroundImage.AddLogo(background,Path.Combine(root,@"setup\payload\backgrounds\forts-logo.png")))&&File.ReadAllBytes(Path.Combine(game,@"data\ui\textures\menu_backgrounds\base\FE-AllScreens-BG.dds")).SequenceEqual(background),"installed main menu matches the logo preview while other screens stay clean");
            preserved.Window.Close();
            c.SetBackground(false,null,null);c.SetBackground(true,null,null);
            Check(!c.UI<Button>("InstallBtn").IsEnabled&&c.UI<Image>("MenuBackgroundPreview").Source==null,"clearing choices leaves installed files unchanged");
            c.UI<RadioButton>("NavHud").IsChecked=true;

            Wait(()=>c.UI<TextBlock>("PhotoCount").Text=="1 / 4");
            var firstPhoto=c.UI<System.Windows.Controls.Border>("HudShot").Background;
            c.UI<Button>("PhotoNext").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Wait(()=>c.UI<TextBlock>("PhotoCount").Text=="2 / 4"&&!Object.ReferenceEquals(firstPhoto,c.UI<System.Windows.Controls.Border>("HudShot").Background));
            Check(((SolidColorBrush)c.UI<Border>("PhotosBox").Background).Color.A==255,"gallery navigation has an opaque readable background");
            c.Window.UpdateLayout();
            Check(c.UI<Border>("PhotosBox").TranslatePoint(new Point(0,0),c.UI<Border>("HudFrame")).Y>=c.UI<Border>("HudShot").ActualHeight,"photo navigation sits outside the screenshot");
            Check(c.UI<TextBlock>("PhotoCount").Text=="2 / 4","photo arrows update image and visible counter");
            Check(((ImageBrush)c.UI<Border>("HudShot").Background).Stretch==Stretch.Uniform,"gallery preview does not crop screenshots into similar-looking strips");
            var full=new GalleryViewer(c.Window,new[]{Path.Combine(root,@"setup\app\assets\hud_after.png"),Path.Combine(root,@"setup\app\assets\hud_photo_2.png")},0,"Compact",false,true);
            full.Show();Wait(()=>full.Picture.Source!=null);full.Step(1);
            var secondPhoto=WindowController.Bitmap(Path.Combine(root,@"setup\app\assets\hud_photo_2.png"));
            Wait(()=>full.Index==1&&Object.ReferenceEquals(full.Picture.Source,secondPhoto));
            Check(full.Counter.Text=="2 / 2"&&full.Picture.Stretch==Stretch.Uniform,"large viewer browses full uncropped screenshots");full.Close();
            Check(c.Selection().Customization.Resources=="corner","corner placement default remains an explicit setting");
            Check(c.Selection().ClickThrough,"transparent HUD input defaults to the installed behaviour");
            c.UI<CheckBox>("ClickThroughSwitch").IsChecked=false;
            Check(!c.Selection().ClickThrough,"HUD input switch can restore blocking");
            c.UI<CheckBox>("ClickThroughSwitch").IsChecked=true;
            var dialog=new CustomizationDialog(c.Window,c.Selection().Customization,"tactical",false);
            Check(dialog.CardsChoice.Style==c.Window.FindResource("TahoeCombo"),"settings use the shared Tahoe control system");
            Check(Enumerable.Range(-12,25).All(n=>!CustomizationDialog.HueLabel("compact",n*15,false).Any(Char.IsDigit)),"hue is shown as a colour name instead of degrees");
            dialog.Show();Pump(60);Render(dialog,Path.Combine(root,@"build\native-customization.png"));
            dialog.ResourcesChoice.SelectedIndex=1;dialog.CardsChoice.SelectedIndex=0;dialog.BackgroundChoice.SelectedIndex=2;dialog.HueSlider.Value=90;
            Check(dialog.Value.Resources=="center"&&dialog.Value.Cards==65&&dialog.Value.Background==100&&dialog.Value.Hue==90,"settings controls update live customization independently");
            dialog.WhiteChoice.IsChecked=true;Pump(60);Check(dialog.Value.White&&CustomizationDialog.ColourLabel("tactical",dialog.Value,false)=="Белый"&&CustomizationDialog.ColourLabel("tactical",dialog.Value,true)=="White","white HUD choice is stored and named");
            Render(dialog,Path.Combine(root,@"build\native-customization-white.png"));
            dialog.HueSlider.Value=0;Check(!dialog.Value.White&&dialog.WhiteChoice.IsChecked!=true,"moving the hue slider replaces white");
            dialog.ResourcesChoice.SelectedIndex=0;Check(dialog.Value.Resources=="corner","resources can be switched back to the corner");dialog.Close();
            {var sd=new CustomizationDialog(c.Window,new HudCustomization{Background=100},"compact",false);sd.Show();Pump(60);Render(sd,Path.Combine(root,@"build\native-starkoff.png"));sd.Close();}
            {var c1=new HudCustomization{Resources="corner",Cards=65,Background=100,Hue=90,Indicators=false}.For("compact");var c2=new HudCustomization{Resources="center",Background=100}.For("compact");Check(c1.Cards==80&&c1.Hue==0&&c1.Indicators&&c1.Background==0&&c2.Background==0,"Starkoff only varies resource position");}
            Render(c.Window,Path.Combine(root,@"build\native-compact.png"));
            c.UI<RadioButton>("NavAbout").IsChecked=true;Pump(300);
            Check(c.UI<ScrollViewer>("AboutPage").Visibility==Visibility.Visible&&c.UI<Button>("InstallBtn").Visibility==Visibility.Collapsed&&c.UI<Button>("MatchroomDownload")!=null,"About page shows the developer and Matchroom download without the install button");
            Check(c.UI<Image>("AuthorAvatar").Stretch==Stretch.Uniform&&c.UI<Image>("AuthorAvatar").ActualHeight>=270,"author portrait displays the full original artwork including the collar and medal");
            Render(c.Window,Path.Combine(root,@"build\native-about.png"));
            {var pick=Matchroom.Pick("{\"tag_name\":\"v1.2.3\",\"assets\":[{\"name\":\"Matchroom.Next.v1.2.3.exe\",\"browser_download_url\":\"https://github.com/DaniilMokhnatkin/MatchroomNext/releases/download/v1.2.3/Matchroom.Next.v1.2.3.exe\"},{\"name\":\"x.sha256.txt\",\"browser_download_url\":\"https://github.com/DaniilMokhnatkin/MatchroomNext/releases/download/v1.2.3/x.sha256.txt\"},{\"name\":\"evil.exe\",\"browser_download_url\":\"https://evil.example/evil.exe\"}]}");
             {var kadr=Matchroom.Pick("{\"tag_name\":\"v1.0.15\",\"assets\":[{\"name\":\"Kadr-Setup.exe\",\"browser_download_url\":\"https://github.com/DaniilMokhnatkin/Kadr/releases/download/v1.0.15/Kadr-Setup.exe\"},{\"name\":\"SHA256SUMS.txt\",\"browser_download_url\":\"https://github.com/DaniilMokhnatkin/Kadr/releases/download/v1.0.15/SHA256SUMS.txt\"}]}",Matchroom.KadrRepo);Check(kadr.Name=="Kadr-Setup.exe"&&kadr.HashUrl!=null,"Kadr download picks its installer and SHA256SUMS");}
             Check(pick.Name=="Matchroom.Next.v1.2.3.exe"&&pick.HashUrl!=null&&pick.Url.StartsWith("https://github.com/DaniilMokhnatkin/MatchroomNext/releases/download/"),"Matchroom download picks the installer and its checksum from the official repository only");
             Check(Matchroom.ExpectedHash("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef  file.exe")!=null&&Matchroom.ExpectedHash("nonsense")==null,"checksum file is parsed strictly");}
            {string rel="https://github.com/DaniilMokhnatkin/forts-compact-hud/releases/download/v1.7.1/";var own=Matchroom.Pick("{\"tag_name\":\"v1.7.1\",\"assets\":[{\"name\":\"Forts-Setup.zip\",\"browser_download_url\":\""+rel+"Forts-Setup.zip\"},{\"name\":\"Forts.Setup.exe\",\"browser_download_url\":\""+rel+"Forts.Setup.exe\"},{\"name\":\"SHA256SUMS.txt\",\"browser_download_url\":\""+rel+"SHA256SUMS.txt\"}]}","DaniilMokhnatkin/forts-compact-hud");Check(own.Name=="Forts.Setup.exe"&&own.HashUrl!=null,"in-app update picks the installer exe and checksum, not the zip");}
            c.UI<RadioButton>("NavCross").IsChecked=true;Pump(400);c.UI<Slider>("SizeSlider").Value=75;
            Check(c.UI<TextBlock>("SizeValue").Text=="75%","native slider updates preview");
            c.Window.UpdateLayout();var picker=c.UI<Panel>("Tiles");
            int columnCount=c.UI<System.Windows.Controls.Primitives.UniformGrid>("Tiles").Columns;
            var first=(RadioButton)picker.Children[0];var sixth=(RadioButton)picker.Children[columnCount-1];
            var firstWell=(Border)first.Template.FindName("Well",first);var sixthWell=(Border)sixth.Template.FindName("Well",sixth);
            double leftGap=firstWell.TranslatePoint(new Point(),picker).X;
            double rightGap=picker.ActualWidth-sixthWell.TranslatePoint(new Point(sixthWell.ActualWidth,0),picker).X;
            Check(Math.Abs(leftGap-rightGap)<1&&rightGap<12,"crosshair columns fill the panel with balanced side gaps");
            var newTiles=c.UI<Panel>("Tiles").Children.OfType<RadioButton>().Where(t=>System.Windows.Automation.AutomationProperties.GetAutomationId(t).StartsWith("Crosshair-duo_")||System.Windows.Automation.AutomationProperties.GetAutomationId(t).StartsWith("Crosshair-neo_")).ToArray();
            Check(newTiles.Length==32,"all 32 Duo / Neo crosshairs appear in the picker");
            foreach(var tile in newTiles){
                tile.IsChecked=true;var selected=c.Selection();
                Check(selected.Color=="white"&&selected.Size==75&&!c.UI<Grid>("ColourControls").IsEnabled&&Object.ReferenceEquals(c.UI<Image>("AfterImage").Source,WindowController.Bitmap(Path.Combine(root,@"setup\app\assets\crosshair_"+selected.Crosshair+"_75.png"))),selected.Crosshair+" shows the actual multicolour size preview");
            }
            foreach(int size in new[]{50,100,150,200}){c.UI<Slider>("SizeSlider").Value=size;Check(c.Selection().Size==size,"accent cursor supports "+size+"% in the UI");}
            newTiles.First(t=>System.Windows.Automation.AutomationProperties.GetAutomationId(t)=="Crosshair-neo_prism").IsChecked=true;
            c.UI<Slider>("SizeSlider").Value=150;c.Window.UpdateLayout();newTiles[16].BringIntoView();Pump(400);Render(c.Window,Path.Combine(root,@"build\native-crosshair-packs.png"));
            c.UI<Panel>("Tiles").Children.OfType<RadioButton>().First(t=>System.Windows.Automation.AutomationProperties.GetAutomationId(t)=="Crosshair-cross").IsChecked=true;
            Check(c.UI<Grid>("ColourControls").IsEnabled,"classic cursor still enables colour selection after an accent cursor");
            c.UI<CheckBox>("CrossSwitch").IsChecked=false;
            c.UI<CheckBox>("CrossSwitch").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Check(!c.UI<Grid>("SizeControls").IsEnabled,"crosshair toggle disables size control");
            c.UI<CheckBox>("CrossSwitch").IsChecked=true;c.SelectLayout("zors");
            var brush=(SolidColorBrush)c.Window.Resources["Accent"];
            Check(brush.HasAnimatedProperties&&!brush.IsFrozen,"theme uses native WPF animation clocks");
            Pump(160);Color before=brush.Color;c.SelectLayout("compact");var reverse=(SolidColorBrush)c.Window.Resources["Accent"];
            Check(Math.Abs(reverse.Color.R-before.R)<25,"interrupted transition starts from current colour");
            Pump(600);
            Check(Object.ReferenceEquals(reverse,c.Window.Resources["Accent"]),"theme does not replace resources every frame");
            Check(reverse.Color.A==255&&reverse.Color.R==10&&reverse.Color.G==122&&reverse.Color.B==255,"reverse transition ends at neutral colour");
            c.SelectLayout("zors");c.UI<RadioButton>("NavHud").IsChecked=true;Pump(650);Render(c.Window,Path.Combine(root,@"build\native-zors.png"));
            Check(c.UI<TextBlock>("LayoutTitle").Text.Contains("Zors"),"Zors content updates");
            c.SelectLayout("tactical");Pump(100);
            Check(c.UI<TextBlock>("LayoutTitle").Text.Contains("RobberFox")&&c.UI<Button>("LayoutLink").Content.ToString().Contains("RobberFox"),"Tactical credits RobberFox in the UI");
            Wait(()=>c.UI<TextBlock>("PhotoCount").Text=="1 / 2");Check(c.UI<TextBlock>("ShotCaption").Text.Contains("центру"),"Tactical gallery uses both real gameplay screenshots");
            c.SelectLayout("minimalist");Check(c.UI<Button>("CustomizationBtn").Visibility==Visibility.Collapsed&&c.Selection().Customization==null,"Minimalist cannot inherit customization");
            c.SelectLayout("zors");
            Check(c.UI<Button>("CustomizationBtn").Visibility==Visibility.Visible&&c.Selection().Customization!=null,"Zors offers resource placement settings");
            app.Dispatcher.BeginInvoke(new Action(()=>{
                var zd=c.Window.OwnedWindows.OfType<CustomizationDialog>().Single();
                Check(zd.Title.Contains("Zors.o")&&zd.CardsChoice.Visibility==Visibility.Collapsed&&zd.BackgroundChoice.Visibility==Visibility.Collapsed,"Zors settings identify the theme and only expose resource placement");
                zd.ResourcesChoice.SelectedIndex=0;Render(zd,Path.Combine(root,@"build\native-zors-corner.png"));
                Check(zd.Value.Resources=="corner"&&zd.Value.Cards==80&&zd.Value.Hue==0&&!zd.Value.White,"Zors corner choice preserves the original styling");
                zd.ResourcesChoice.SelectedIndex=1;Render(zd,Path.Combine(root,@"build\native-zors-center.png"));zd.DialogResult=true;
            }));
            c.UI<Button>("CustomizationBtn").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Check(c.Selection().Customization.Resources=="center","Zors settings button applies the selected resource position");
            Check(!c.UI<Button>("CustomizationBtn").ToolTip.ToString().Contains("80%"),"Zors settings tooltip describes only the available resource option");
            c.SelectLayout("grib");Pump(600);
            Check(c.Theme.Current=="grib"&&c.Selection().Crosshair=="grib"&&c.Selection().Color=="white"&&!c.UI<Grid>("ColourControls").IsEnabled,"Grib selects its full-colour mushroom cursor and installer theme");
            Check(c.UI<TextBlock>("LayoutTitle").Text=="♂ True Grib Version ♂"&&c.UI<Button>("LayoutLink").Visibility==Visibility.Collapsed,"Grib has its own title and no unrelated author link");
            Wait(()=>c.UI<TextBlock>("ShotCaption").Text=="В игре");
            Check(c.UI<TextBlock>("PhotoCount").Text=="1 / 1"&&Directory.GetFiles(Path.Combine(root,@"setup\app\assets\gallery\grib")).Length==1,"Grib gallery contains only the user's gameplay screenshot");
            Render(c.Window,Path.Combine(root,@"build\native-grib.png"));
            var gd=new CustomizationDialog(c.Window,c.Selection().Customization,"grib",false,Path.Combine(root,@"setup\app\assets"));gd.Show();gd.ResourcesChoice.SelectedIndex=0;Pump(30);Render(gd,Path.Combine(root,@"build\native-grib-corner.png"));
            Check(gd.Title.Contains("♂ True Grib Version ♂")&&gd.Value.Resources=="corner"&&gd.CardsChoice.Visibility==Visibility.Collapsed,"Grib settings expose resource placement with the correct theme title");gd.ResourcesChoice.SelectedIndex=1;Check(gd.Value.Resources=="center","Grib resources can move to the centre");gd.Close();
            c.UI<RadioButton>("NavCross").IsChecked=true;Pump(30);Render(c.Window,Path.Combine(root,@"build\native-grib-cursor.png"));
            c.UI<CheckBox>("CrossSwitch").IsChecked=false;Check(c.Selection().Crosshair==null,"Grib cursor can be disabled independently");c.UI<CheckBox>("CrossSwitch").IsChecked=true;
            c.UI<RadioButton>("NavIcons").IsChecked=true;
            var scrapSlider=c.UI<StackPanel>("IconRows").Children.OfType<Grid>().SelectMany(row=>row.Children.OfType<Slider>()).Single(sl=>System.Windows.Automation.AutomationProperties.GetAutomationId(sl)=="Opacity-scrap");
            scrapSlider.Value=25;Check(c.Selection().Icons["scrap"]==25,"demolition has a working independent opacity slider");Pump(30);Render(c.Window,Path.Combine(root,@"build\native-grib-icons.png"));scrapSlider.Value=100;
            c.SelectLayout("zors");
            Check(c.UI<Button>("FeedbackBtn")!=null&&c.UI<Button>("CheckUpdateBtn")!=null,"feedback and manual update actions are present");
            Wait(()=>c.UI<Button>("CheckUpdateBtn").IsEnabled);
            c.UI<Button>("CheckUpdateBtn").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Check(c.UI<ProgressBar>("UpdateProgress").Visibility==Visibility.Visible&&!c.UI<Button>("CheckUpdateBtn").IsEnabled,"manual update check animates and prevents duplicate requests");
            Wait(()=>c.UI<Button>("CheckUpdateBtn").IsEnabled);Check(!String.IsNullOrWhiteSpace(c.UI<TextBlock>("UpdateStatus").Text)&&!c.UI<TextBlock>("UpdateStatus").Text.Contains("Не удалось")&&c.UI<ProgressBar>("UpdateProgress").Visibility==Visibility.Collapsed,"manual update check ends with an explicit result");
            var feedback=new FeedbackDialog(c.Window,Path.Combine(game,"unconfigured-feedback"),"version=test; layout=zors",false);feedback.Show();Pump(80);
            feedback.Description.Text="Ошибка при установке";Check(!feedback.SendButton.IsEnabled,"unconfigured feedback cannot claim to send");Render(feedback,Path.Combine(root,@"build\native-feedback.png"));feedback.Close();
            if(FeedbackClient.Endpoint(Path.Combine(root,"setup"))!=null){
                var connected=new FeedbackDialog(c.Window,Path.Combine(root,"setup"),"version=test; layout=zors",false);connected.Show();connected.Description.Text="Проверка формы без отправки";Pump(40);
                Check(connected.SendButton.IsEnabled,"embedded public endpoint enables feedback after a description is entered");Render(connected,Path.Combine(root,@"build\native-feedback-connected.png"));connected.Close();
            }
            bool rejected=false;try{FeedbackClient.Mime(new byte[]{77,90,0,0,0,0,0,0});}catch(InvalidDataException){rejected=true;}Check(rejected,"feedback rejects disguised non-image attachments");
            using(var form=FeedbackClient.BuildBody("Русский текст","","version=test",new byte[]{137,80,78,71,13,10,26,10,0,0})){
                string raw=form.ReadAsStringAsync().GetAwaiter().GetResult();
                Check(raw.Contains("name=\"description\"")&&raw.Contains("name=\"contact\"")&&raw.Contains("name=\"details\"")&&raw.Contains("name=\"screenshot\"")&&raw.Contains("filename=\"screenshot.png\""),"feedback serializes quoted multipart names and filename for Cloudflare");
            }
            c.UI<RadioButton>("NavIcons").IsChecked=true;
            bool skinWarningShown=false;
            var skinWarningTimer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(20)};
            skinWarningTimer.Tick+=(s,e)=>{
                var warning=c.Window.OwnedWindows.OfType<HudSkinWarningDialog>().FirstOrDefault();if(warning==null)return;
                skinWarningShown=true;skinWarningTimer.Stop();Render(warning,Path.Combine(root,@"build\native-default-skin-warning.png"));
                Check(warning.IsVisible&&warning.Title.Contains("По умолчанию"),"required default-skin warning opens after HUD installation");
                warning.Acknowledge.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            };
            skinWarningTimer.Start();
            c.UI<Button>("InstallBtn").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Wait(()=>c.UI<Button>("DoneBtn").Visibility==Visibility.Visible);
            skinWarningTimer.Stop();Check(skinWarningShown&&c.UI<TextBlock>("WorkText").Text.Contains("Скин HUD"),"default-skin instructions remain on the completed installation page");
            Check(c.UI<TextBlock>("WorkText").Text.Contains("Скин меню")&&HudSkinWarningDialog.Message(true,true,null,true).Contains("Menu Skin"),"Zors warning requires both Default HUD skin and Default menu background");
            Check(c.UI<TextBlock>("WorkTitle").Text=="Готово","native UI completes installation");
            Check(engine.Status(game).Layout=="zors","UI installs selected layout");
            c.Window.Close();
            var reduced=new WindowController(Path.Combine(root,"setup"),game,"en",true);reduced.Window.Show();Wait(()=>reduced.Ready!=null&&reduced.Ready.IsCompleted);reduced.SelectLayout("zors");
            Check(!((SolidColorBrush)reduced.Window.Resources["Accent"]).HasAnimatedProperties,"reduced motion applies theme immediately");
            Check(reduced.UI<Button>("InstallBtn").Content.ToString()=="Next","English UI translated");
            reduced.SelectLayout("grib");Check(reduced.UI<TextBlock>("HudTitle").Text.Contains("♂ True Grib Version ♂")&&reduced.UI<TextBlock>("LayoutText").Text.Contains("Skill not included")&&reduced.UI<TextBlock>("AfterLabel").Text.Contains("Mushroom"),"English Grib UI keeps the requested title and translates theme/cursor copy");
            Check(Engine.RequiresDefaultHudSkin("grib")&&Engine.RequiresDefaultHudSkin("zors")&&Engine.RequiresDefaultHudSkin("minimalist")&&Engine.RequiresDefaultHudSkin("tactical")&&!Engine.RequiresDefaultHudSkin("compact"),"default-skin requirement follows the installed HUD's supported skins");
            Check(HudSkinWarningDialog.Message(true).Contains("HUD Skin")&&HudSkinWarningDialog.Message(true).Contains("Default"),"default-skin warning has English instructions");
            Check(HudSkinWarningDialog.Message(false,false,"Firebird").Contains("Firebird")&&HudSkinWarningDialog.Message(false,false,"Firebird").Contains("совпадать"),"Starkoff warning names the selected skin and requires matching the game");
            {var warning=new HudSkinWarningDialog(reduced.Window,false,false,"Firebird");warning.Show();Pump(30);Render(warning,Path.Combine(root,@"build\native-starkoff-skin-warning.png"));warning.Close();}reduced.Window.Close();
            var updater=new WindowController(Path.Combine(root,"setup"),game,"ru",true);updater.Window.Show();Wait(()=>updater.Ready!=null&&updater.Ready.IsCompleted);bool revealed=false;
            updater.CompleteSelfUpdate("Forts.Setup-1.8.4.exe",file=>{Check(updater.Window.IsVisible&&file=="Forts.Setup-1.8.4.exe","update helper starts before the old installer closes");revealed=true;});
            Check(revealed&&!updater.Window.IsVisible,"old installer closes after a successful update download");
            string updateDir=Path.Combine(game,"update-test");Directory.CreateDirectory(updateDir);
            string target=Path.Combine(updateDir,"Forts Setup.exe"),source=Path.Combine(updateDir,"download.exe");
            File.WriteAllText(target,"old");File.WriteAllText(source,"new");bool started=false;
            SelfUpdate.Apply(source,target,path=>{Check(path==target&&File.ReadAllText(path)=="new","new installer replaces the current file before launch");started=true;});
            Check(started&&File.ReadAllText(target)=="new"&&!File.Exists(source),"successful update restarts the replacement and removes its download");
            File.WriteAllText(source,"next");bool failed=false;try{SelfUpdate.Apply(source,target,path=>{throw new IOException("simulated launch failure");});}catch(IOException){failed=true;}
            Check(failed&&File.ReadAllText(target)=="new"&&File.ReadAllText(source)=="next","failed restart restores the old executable and keeps the download");
            engine.Restore(game,null);Console.WriteLine(checks+" UI CHECKS PASSED");return 0;
        }catch(Exception e){Console.Error.WriteLine(e);return 1;}
        finally{if(c!=null)c.Window.Close();app.Shutdown();if(game.StartsWith(Path.Combine(root,"build")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))Directory.Delete(game,true);}
    }
}
