using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using FortsSetup;

static class EngineTests {
    static int checks;
    static void Check(bool value, string text) { if (!value) throw new Exception(text); Console.WriteLine("PASS " + text); checks++; }
    static void Reject(Action action, string text) { try { action(); } catch { Check(true, text); return; } throw new Exception(text); }
    static Dictionary<string,string> Snapshot(string path) { return Directory.GetFiles(path,"*",SearchOption.AllDirectories).Where(p => !p.Contains(Engine.BackupName)).ToDictionary(p => p.Substring(path.Length), Engine.Hash); }
    static bool Same(Dictionary<string,string> a, Dictionary<string,string> b) { return a.Count==b.Count && a.All(p => b.ContainsKey(p.Key) && b[p.Key]==p.Value); }
    public static int Main(string[] args) {
        string root=Path.GetFullPath(args[0]), game=Path.Combine(root,"build","native-test-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(game,"data","ui","screens"));
        try {
            File.WriteAllText(Path.Combine(game,"Forts.exe"),"test fixture");
            foreach (string file in new[]{"hud.lua","transaction.lua"}) File.Copy(Path.Combine(root,"original",file),Path.Combine(game,"data","ui","screens",file));
            File.Copy(Path.Combine(root,"original","uisprites.lua"),Path.Combine(game,"data","ui","uisprites.lua"));
            Directory.CreateDirectory(Path.Combine(game,"data","ui","textures"));
            foreach (string file in new[]{"mouse_target.dds","mouse_target_painted.dds"}) File.Copy(Path.Combine(root,"textures","vanilla",file),Path.Combine(game,"data","ui","textures",file));
            var engine=new Engine(Path.Combine(root,"setup"));
            var initial=Snapshot(game);
            Check(engine.Status(game).HudCompatible,"vanilla compatibility");
            Reject(() => Engine.SafePath(game,"..\\outside"),"reject path traversal");
            Reject(() => Engine.SafePath(game,game),"reject absolute path");
            Reject(() => engine.Install(game,new Options{Hud=true,Layout="unknown"},null),"reject unknown layout before writes");
            Check(Same(initial,Snapshot(game)),"invalid choice leaves game untouched");
            var options=new Options { Hud=true,Layout="zors",Crosshair="zors",Size=150,SkipIntro=true };
            options.Icons["repair"]=50;
            engine.Install(game,options,null);
            var installed=engine.Status(game);
            Check(installed.Layout=="zors" && installed.HasBackup,"Zors install and status");
            Check(installed.Crosshair=="zors" && installed.Size==150,"crosshair size config");
            Check(installed.SkipIntro && installed.Icons["repair"]==50,"intro and icon opacity");
            var beforeFailure=Snapshot(game);
            string stateFile=Path.Combine(game,Engine.BackupName,"state.json");
            string beforeState=File.ReadAllText(stateFile);
            Reject(() => engine.Install(game,new Options{Hud=true,Layout="compact"},(step,total,text)=>{if(step==3)throw new IOException("injected write failure");}),"injected mid-install failure");
            Check(Same(beforeFailure,Snapshot(game))&&File.ReadAllText(stateFile)==beforeState,"failed install rolls back files and preserves metadata");
            engine.Install(game,new Options{Hud=true,Layout="compact",Crosshair="scope",Color="cyan",Size=75,SkipIntro=false},null);
            installed=engine.Status(game);
            Check(installed.Layout=="compact" && installed.Color=="cyan" && installed.Size==75,"switch layout and tinted crosshair");
            Check(!installed.SkipIntro,"disable intro override");
            var custom=new HudCustomization{Resources="corner",Cards=65,Background=0,Hue=90,Indicators=true};
            engine.Install(game,new Options{Hud=true,Layout="tactical",Customization=custom},null);
            installed=engine.Status(game);
            Check(installed.Layout=="tactical"&&installed.Customization.Resources=="corner"&&installed.Customization.Hue==90,"custom profile and Tactical layout persist");
            Check(Engine.Hash(Path.Combine(game,@"data\ui\screens\hud.lua"))==Engine.Hash(engine.Payload(@"customization\variants\tactical\corner-65-0-1.lua")),"actual installed Lua matches chosen custom layout");
            string cornerHash=Engine.Hash(Path.Combine(game,@"data\ui\screens\hud.lua"));
            custom.Resources="center";custom.Cards=100;custom.Background=50;custom.Indicators=false;
            engine.Install(game,new Options{Hud=true,Layout="tactical",Customization=custom},null);
            Check(Engine.Hash(Path.Combine(game,@"data\ui\screens\hud.lua"))!=cornerHash&&engine.Status(game).Customization.Resources=="center","resource placement is selectable, not hardcoded");
            byte[] sample=File.ReadAllBytes(engine.Payload(@"layouts\tactical\textures\HUD\custom\Pause-A.dds"));
            var shifted=HudCustomization.ShiftHue(sample,90);
            Check(!sample.SequenceEqual(shifted)&&Enumerable.Range(0,(sample.Length-128)/4).All(i=>sample[131+i*4]==shifted[131+i*4]),"hue changes texture pixels and preserves alpha");
            Reject(()=>engine.Install(game,new Options{Hud=true,Customization=new HudCustomization{Resources="../bad"}},null),"reject invalid customization values");
            var skipped=engine.Restore(game,null);
            Check(skipped.Count==0 && Same(initial,Snapshot(game)),"restore byte-identical originals after switching layouts");
            engine.Install(game,new Options{Hud=true,Layout="zors"},null);
            string hud=Path.Combine(game,"data","ui","screens","hud.lua"); File.WriteAllText(hud,"steam update");
            skipped=engine.Restore(game,null);
            Check(skipped.Contains("data\\ui\\screens\\hud.lua") && File.ReadAllText(hud)=="steam update","restore preserves Steam/external updates");
            File.Copy(Path.Combine(root,"original","hud.lua"),hud,true);
            engine.Install(game,new Options{Crosshair="cross",Size=100},null);
            string original=Path.Combine(game,Engine.BackupName,"data","ui","textures","mouse_target.dds");
            File.WriteAllText(original,"corrupt");
            var beforeBadRestore=Snapshot(game);
            Reject(() => engine.Restore(game,null),"reject corrupt backup");
            Check(Same(beforeBadRestore,Snapshot(game)),"corrupt backup causes no partial restoration");
            Console.WriteLine(checks+" ENGINE CHECKS PASSED"); return 0;
        } catch(Exception e) { Console.Error.WriteLine(e); return 1; }
        finally { if (game.StartsWith(Path.Combine(root,"build")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)) Directory.Delete(game,true); }
    }
}
