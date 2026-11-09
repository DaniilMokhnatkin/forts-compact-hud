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
            var dialog=new CustomizationDialog(c.Window,c.Selection().Customization,"tactical",false);
            Check(dialog.CardsChoice.Style==c.Window.FindResource("TahoeCombo"),"settings use the shared Tahoe control system");
            Check(Enumerable.Range(-12,25).All(n=>!CustomizationDialog.HueLabel("compact",n*15,false).Any(Char.IsDigit)),"hue is shown as a colour name instead of degrees");
            dialog.Show();Pump(60);Render(dialog,Path.Combine(root,@"build\native-customization.png"));
            dialog.ResourcesChoice.SelectedIndex=1;dialog.CardsChoice.SelectedIndex=0;dialog.BackgroundChoice.SelectedIndex=2;dialog.HueSlider.Value=90;
            Check(dialog.Value.Resources=="center"&&dialog.Value.Cards==65&&dialog.Value.Background==100&&dialog.Value.Hue==90,"settings controls update live customization independently");
            dialog.ResourcesChoice.SelectedIndex=0;Check(dialog.Value.Resources=="corner","resources can be switched back to the corner");dialog.Close();
            {var sd=new CustomizationDialog(c.Window,new HudCustomization{Background=100},"compact",false);sd.Show();Pump(60);Render(sd,Path.Combine(root,@"build\native-starkoff.png"));sd.Close();}
            {var c1=new HudCustomization{Resources="corner",Cards=65,Background=100,Hue=90,Indicators=false}.For("compact");var c2=new HudCustomization{Resources="center",Background=100}.For("compact");Check(c1.Cards==80&&c1.Hue==0&&c1.Indicators&&c1.Background==0&&c2.Background==0,"Starkoff only varies resource position");}
            Render(c.Window,Path.Combine(root,@"build\native-compact.png"));
            c.UI<RadioButton>("NavAbout").IsChecked=true;Pump(300);
            Check(c.UI<StackPanel>("AboutPage").Visibility==Visibility.Visible&&c.UI<Button>("InstallBtn").Visibility==Visibility.Collapsed&&c.UI<Button>("MatchroomDownload")!=null,"About page shows the developer and Matchroom download without the install button");
            Render(c.Window,Path.Combine(root,@"build\native-about.png"));
            {var pick=Matchroom.Pick("{\"tag_name\":\"v1.2.3\",\"assets\":[{\"name\":\"Matchroom.Next.v1.2.3.exe\",\"browser_download_url\":\"https://github.com/DaniilMokhnatkin/MatchroomNext/releases/download/v1.2.3/Matchroom.Next.v1.2.3.exe\"},{\"name\":\"x.sha256.txt\",\"browser_download_url\":\"https://github.com/DaniilMokhnatkin/MatchroomNext/releases/download/v1.2.3/x.sha256.txt\"},{\"name\":\"evil.exe\",\"browser_download_url\":\"https://evil.example/evil.exe\"}]}");
             Check(pick.Name=="Matchroom.Next.v1.2.3.exe"&&pick.HashUrl!=null&&pick.Url.StartsWith("https://github.com/DaniilMokhnatkin/MatchroomNext/releases/download/"),"Matchroom download picks the installer and its checksum from the official repository only");
             Check(Matchroom.ExpectedHash("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef  file.exe")!=null&&Matchroom.ExpectedHash("nonsense")==null,"checksum file is parsed strictly");}
            c.UI<RadioButton>("NavCross").IsChecked=true;c.UI<Slider>("SizeSlider").Value=75;
            Check(c.UI<TextBlock>("SizeValue").Text=="75%","native slider updates preview");
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
            Check(c.UI<Button>("CustomizationBtn").Visibility==Visibility.Collapsed&&c.Selection().Customization==null,"Zors cannot inherit customization");
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
            c.UI<Button>("InstallBtn").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Wait(()=>c.UI<Button>("DoneBtn").Visibility==Visibility.Visible);
            Check(c.UI<TextBlock>("WorkTitle").Text=="Готово","native UI completes installation");
            Check(engine.Status(game).Layout=="zors","UI installs selected layout");
            c.Window.Close();
            var reduced=new WindowController(Path.Combine(root,"setup"),game,"en",true);reduced.Window.Show();Wait(()=>reduced.Ready!=null&&reduced.Ready.IsCompleted);reduced.SelectLayout("zors");
            Check(!((SolidColorBrush)reduced.Window.Resources["Accent"]).HasAnimatedProperties,"reduced motion applies theme immediately");
            Check(reduced.UI<Button>("InstallBtn").Content.ToString()=="Next","English UI translated");reduced.Window.Close();
            engine.Restore(game,null);Console.WriteLine(checks+" UI CHECKS PASSED");return 0;
        }catch(Exception e){Console.Error.WriteLine(e);return 1;}
        finally{if(c!=null)c.Window.Close();app.Shutdown();if(game.StartsWith(Path.Combine(root,"build")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))Directory.Delete(game,true);}
    }
}
