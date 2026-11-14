using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FortsSetup;
static class PreferencesTests {
 static int checks;
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
 static void Pump(){var frame=new DispatcherFrame();var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(150)};timer.Tick+=(s,e)=>{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);}
 static void Ready(WindowController c){while(c.Ready==null||!c.Ready.IsCompleted)Pump();Pump();}
 [STAThread] static int Main(string[] args){
  string root=Path.GetFullPath(args[0]),dir=Path.Combine(root,"build","preferences-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
  var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
  try{
   var store=new Preferences(dir);Check(store.Load()==null,"first launch has no saved profile");
   string pack=Path.Combine(root,"setup"),game=Path.Combine(dir,"game");
   Directory.CreateDirectory(Path.Combine(game,@"data\ui\screens"));File.WriteAllText(Path.Combine(game,"Forts.exe"),"fixture");
   foreach(string name in new[]{"hud.lua","transaction.lua"})File.Copy(Path.Combine(root,"original",name),Path.Combine(game,@"data\ui\screens",name));
   File.Copy(Path.Combine(root,@"original\uisprites.lua"),Path.Combine(game,@"data\ui\uisprites.lua"));
   var c=new WindowController(pack,game,"ru",true,null,store);c.Window.Show();Ready(c);
   c.SelectLayout("tactical");c.UI<CheckBox>("HudSwitch").IsChecked=true;c.UI<Slider>("SizeSlider").Value=145;
   c.UI<CheckBox>("CrossSwitch").IsChecked=false;c.UI<CheckBox>("ClickThroughSwitch").IsChecked=false;
   c.UI<CheckBox>("IntroSwitch").IsChecked=true;c.UI<RadioButton>("NavBackground").IsChecked=true;c.SelectBackgroundPreset("zors");
   c.ApplyBackgroundCrop(false,new Int32Rect(100,100,800,450));var before=c.Selection();c.Window.Close();
   var saved=store.Load();Check(saved!=null&&saved.Selection.Layout=="tactical"&&saved.Selection.Size==145,"closing saves unapplied choices");
   Check(saved.Selection.MenuBackground.SequenceEqual(before.MenuBackground)&&saved.Selection.MenuBackgroundSource.SequenceEqual(before.MenuBackgroundSource),"background and reversible crop survive reopening");
   Check(Directory.GetFiles(dir,"*.dds").Length==2,"background sources use binary sidecars");
   Check(new FileInfo(Path.Combine(dir,"settings.json")).Length<10000,"settings metadata stays small");
   var reopened=new WindowController(pack,game,saved.Language,true,saved.Selection,store);reopened.PreferredPage=saved.Page;reopened.Window.Show();Ready(reopened);
   Check(reopened.Layout=="tactical"&&reopened.Selection().Size==145&&!reopened.Selection().ClickThrough&&reopened.Selection().Crosshair==null,"new window restores toggles and size");
   Check(reopened.Page=="BackgroundPage"&&reopened.Selection().MenuBackground.SequenceEqual(before.MenuBackground),"new window restores page and background");
   reopened.CompleteSelfUpdate("fixture.exe",file=>{Check(store.Load().Selection.Size==145,"choices are saved before updater starts");});
   saved=store.Load();saved.Language="en";saved.Selection.Skin="dlc2";saved.Selection.Color="cyan";saved.Selection.ColorChoice="cyan";saved.Selection.CrosshairChoice="neo_star";
   saved.Selection.Icons["scrap"]=35;saved.Selection.Customization=new HudCustomization{Resources="center",Cards=65,Background=50,Hue=45,White=true,Indicators=false};saved.Selection.RestoreBackground=true;
   Check(store.Save(saved),"updated version can save the same stable profile");
   var same=new Preferences(dir).Load();Check(same.Language=="en"&&same.Selection.Skin=="dlc2"&&same.Selection.Icons["scrap"]==35&&same.Selection.Customization.White,"language skin icons and customization survive a new store instance");
   var third=new WindowController(pack,game,same.Language,true,same.Selection,store);third.Window.Show();Ready(third);
   Check(third.Selection().Skin=="dlc2"&&third.Selection().ColorChoice=="cyan"&&third.Selection().CrosshairChoice=="neo_star"&&third.Selection().Icons["scrap"]==35,"saved skin colour disabled cursor and icon values win over installed state");
   Check(third.Selection().Customization.White&&third.Selection().Customization.Cards==65&&third.Selection().Customization.Resources=="center","custom HUD settings restore in the real controller");third.Window.Close();
   var languageWindow=new WindowController(pack,game,"ru",true,same.Selection,store);languageWindow.Window.Show();Ready(languageWindow);
   languageWindow.UI<RadioButton>("LangEn").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
   Check(store.Load().Language=="en","language switch saves the new language before replacing the old window");Pump();Pump();Application.Current.MainWindow.Close();
   Check(store.Load().Language=="en","closing the replacement window retains its new language");
   Check(!same.Selection.RestoreBackground&&same.Selection.CrosshairChoice=="neo_star","inactive crosshair choice is retained and restore action is not rearmed");
   same.Selection.MenuCrop=new[]{0,0,-5,10};store.Save(same);Check(store.Load().Selection.MenuCrop==null,"invalid crop falls back to full image");
   foreach(string image in Directory.GetFiles(dir,"*.dds"))File.Delete(image);Check(store.Load().Selection.MenuBackground==null&&store.Load().Selection.Layout=="tactical","missing image keeps other preferences usable");
   File.WriteAllText(Path.Combine(dir,"settings.json"),"broken");Check(store.Load()==null,"corrupt metadata falls back safely");
   Check(!new Preferences(Path.Combine(dir,"settings.json")).Save(saved),"write failure is nonfatal");
   Console.WriteLine(checks+" PREFERENCES CHECKS PASSED");return 0;
  }catch(Exception e){Console.Error.WriteLine(e);return 1;}
  finally{app.Shutdown();Directory.Delete(dir,true);}
 }
}
