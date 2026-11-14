using System;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FortsSetup;

static class BackgroundTests {
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);}
    static void Reject(Action action,string name){try{action();}catch{Console.WriteLine("PASS "+name);return;}throw new Exception(name);}
    static string MainRel(string skin){return @"data\ui\textures\menu_backgrounds\"+skin+@"\FE-MainMenu-BG.dds";}
    [STAThread] static int Main(string[] args){
        string root=Path.GetFullPath(args[0]),dir=Path.Combine(root,"build","background-test-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try{
            string png=Path.Combine(dir,"картинка.png");
            byte[] pixels=Enumerable.Range(0,80*60).SelectMany(i=>new byte[]{30,60,200,255}).ToArray();
            pixels[pixels.Length-4]=230;pixels[pixels.Length-3]=120;pixels[pixels.Length-2]=10;
            var image=BitmapSource.Create(80,60,96,96,PixelFormats.Bgra32,null,pixels,80*4);
            var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using(var file=File.Create(png))encoder.Save(file);
            var dds=BackgroundImage.Encode(png);
            Check(BitConverter.ToInt32(dds,16)==80&&BitConverter.ToInt32(dds,12)==60&&dds.Length==128+80*60*4,"non-16:9 image retains its dimensions without cropping");
            Check(dds[128]==30&&dds[129]==60&&dds[130]==200&&dds[131]==255,"DDS keeps BGRA colours and opaque alpha");
            Check(dds[dds.Length-4]==230&&dds[dds.Length-3]==120&&dds[dds.Length-2]==10,"image edges are retained");
            Check(BackgroundImage.Preview(dds).PixelWidth==80&&BackgroundImage.Preview(dds).PixelHeight==60,"preview shows the complete image");
            var crop=BackgroundImage.Crop(dds,new System.Windows.Int32Rect(10,10,40,30));
            Check(BitConverter.ToInt32(crop,16)==40&&BitConverter.ToInt32(crop,12)==30&&crop.Length==128+40*30*4,"crop returns the chosen image region");
            Check(crop[128]==dds[128+(10*80+10)*4]&&crop[crop.Length-4]==dds[128+(39*80+49)*4],"crop copies the selected pixels without stretching");
            Reject(()=>BackgroundImage.Crop(dds,new System.Windows.Int32Rect(50,0,40,30)),"crop outside the image is rejected");
            var frame=BackgroundImage.CropFrame(80,60,1,0.5,0.5);
            Check(frame.Width==80&&frame.Height==45&&frame.Y==7,"initial 16:9 frame is centred and fits the image");
            var moved=BackgroundImage.CropFrame(80,60,2,1,1);
            Check(moved.Width==40&&moved.Height==22&&moved.X==40&&moved.Y==38,"zoom and pan select the lower-right region");
            foreach(string extension in new[]{"jpg","bmp"}){
                BitmapEncoder other=extension=="jpg"?(BitmapEncoder)new JpegBitmapEncoder():new BmpBitmapEncoder();
                other.Frames.Add(BitmapFrame.Create(image));string file=Path.Combine(dir,"source."+extension);using(var output=File.Create(file))other.Save(output);
                Check(BackgroundImage.Encode(file).Length==dds.Length,extension+" input converts without an external tool");
            }
            var translucent=BitmapSource.Create(1,1,96,96,PixelFormats.Bgra32,null,new byte[]{40,80,120,128},4);
            var transparentEncoder=new PngBitmapEncoder();transparentEncoder.Frames.Add(BitmapFrame.Create(translucent));
            string alphaFile=Path.Combine(dir,"alpha.png");using(var output=File.Create(alphaFile))transparentEncoder.Save(output);
            var opaque=BackgroundImage.Encode(alphaFile);
            var withLogo=BackgroundImage.AddLogo(dds,alphaFile);int logoPixel=128+(8*80+12)*4;
            Check(!Object.ReferenceEquals(dds,withLogo)&&dds.SequenceEqual(BackgroundImage.Encode(png)),"logo composition leaves the original background available");
            Check(withLogo[logoPixel]==35&&withLogo[logoPixel+1]==70&&withLogo[logoPixel+2]==160,"logo alpha blends with the background");
            Check(withLogo.Skip(dds.Length-4).SequenceEqual(dds.Skip(dds.Length-4)),"logo composition preserves pixels outside the logo");
            Check(opaque[128]==20&&opaque[129]==40&&opaque[130]==60&&opaque[131]==255,"transparent colours are composited on black");
            string bad=Path.Combine(dir,"bad.png");File.WriteAllText(bad,"invalid");Reject(()=>BackgroundImage.Encode(bad),"invalid image refused");
            string game=Path.Combine(dir,"game");Directory.CreateDirectory(Path.Combine(game,@"data\ui\screens"));File.WriteAllText(Path.Combine(game,"Forts.exe"),"fixture");
            foreach(string file in new[]{"hud.lua","transaction.lua"})File.Copy(Path.Combine(root,"original",file),Path.Combine(game,@"data\ui\screens",file));
            File.Copy(Path.Combine(root,@"original\uisprites.lua"),Path.Combine(game,@"data\ui\uisprites.lua"));
            foreach(string skin in Engine.MenuSkins){string target=Path.Combine(game,MainRel(skin));Directory.CreateDirectory(Path.GetDirectoryName(target));File.WriteAllText(target,"original "+skin);}
            var engine=new Engine(Path.Combine(root,"setup"));
            engine.Install(game,new Options{MenuBackground=dds},null);
            Check(Engine.MenuSkins.All(skin=>File.ReadAllBytes(Path.Combine(game,MainRel(skin))).SequenceEqual(dds)),"background-only install covers all six regular menu skins");
            Check(!File.Exists(Path.Combine(game,@"data\ui\textures\menu_backgrounds\base\FE-AllScreens-BG.dds")),"main menu leaves other screens alone");
            Check(engine.Status(game).MenuBackground,"installed background is detected");
            engine.Install(game,new Options{MenuBackground=dds,ScreensBackground=dds},null);
            Check(File.Exists(Path.Combine(game,@"data\ui\textures\menu_backgrounds\dlc3\FE-AllScreens-BG.dds")),"optional clean image covers DLC screens");
            engine.Install(game,new Options{Hud=true,Layout="zors"},null);
            Check(Engine.MenuSkins.All(skin=>File.ReadAllBytes(Path.Combine(game,MainRel(skin))).SequenceEqual(dds)),"Zors installation preserves custom menu backgrounds");
            engine.Install(game,new Options{Hud=true,Layout="compact"},null);
            Check(File.ReadAllBytes(Path.Combine(game,MainRel("base"))).SequenceEqual(dds),"switching away from Zors preserves custom art");
            string hudBeforeReset=Engine.Hash(Path.Combine(game,@"data\ui\screens\hud.lua"));
            string state=Path.Combine(game,Engine.BackupName,"state.json"),before=File.ReadAllText(state);
            var different=(byte[])dds.Clone();different[128]=100;
            Reject(()=>engine.Install(game,new Options{MenuBackground=different},(step,total,text)=>{if(step==3)throw new IOException("injected");}),"write failure is surfaced");
            Check(File.ReadAllText(state)==before&&File.ReadAllBytes(Path.Combine(game,MainRel("base"))).SequenceEqual(dds),"failed background install rolls back texture and state");
            Reject(()=>engine.Install(game,new Options{MenuBackground=new byte[128]},null),"invalid DDS refused before writes");
            engine.Install(game,new Options{RestoreBackground=true},null);
            Check(Engine.Hash(Path.Combine(game,@"data\ui\screens\hud.lua"))==hudBeforeReset,"background reset leaves the installed HUD intact");
            Check(Engine.MenuSkins.All(skin=>File.ReadAllText(Path.Combine(game,MainRel(skin)))=="original "+skin),"background reset returns originals");
            Check(!engine.Status(game).MenuBackground&&!File.Exists(Path.Combine(game,@"data\ui\textures\menu_backgrounds\dlc3\FE-AllScreens-BG.dds")),"reset removes newly created textures and status");
            engine.Install(game,new Options{MenuBackground=dds},null);
            File.WriteAllText(Path.Combine(game,MainRel("dlc3")),"Steam update");
            Reject(()=>engine.Install(game,new Options{RestoreBackground=true},null),"reset refuses to overwrite an external update");
            var skipped=engine.Restore(game,null);
            Check(skipped.Contains(MainRel("dlc3"))&&File.ReadAllText(Path.Combine(game,MainRel("dlc3")))=="Steam update","global restore preserves updated textures");
            return 0;
        }finally{Directory.Delete(dir,true);}
    }
}
