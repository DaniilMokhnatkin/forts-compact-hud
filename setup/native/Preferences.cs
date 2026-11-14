using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace FortsSetup {
    public sealed class SavedPreferences {
        public int Schema=1;
        public string Language="ru",GamePath,Page="HudPage",MenuImage,ScreensImage;
        public Options Selection;
    }
    // This directory is outside Native/<package hash>, so replacing the EXE keeps it.
    public sealed class Preferences {
        readonly string root;
        readonly JavaScriptSerializer json=new JavaScriptSerializer{MaxJsonLength=512*1024};
        public Preferences(string directory){root=Path.GetFullPath(directory);}
        public static Preferences Default(){return new Preferences(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FortsSetup","Preferences"));}
        string ImageFile(string hash){if(hash==null||!Regex.IsMatch(hash,@"\A[A-F0-9]{64}\z"))throw new InvalidDataException("Invalid image reference");return Path.Combine(root,hash+".dds");}
        string SaveImage(byte[] data){
            if(data==null)return null;Engine.ValidateBackground(data);
            string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(data)).Replace("-","");
            string file=ImageFile(hash);if(!File.Exists(file))AtomicWrite(file,data);return hash;
        }
        static void AtomicWrite(string file,byte[] data){
            string temp=file+"."+Guid.NewGuid().ToString("N")+".tmp";
            try{File.WriteAllBytes(temp,data);if(File.Exists(file))File.Replace(temp,file,null);else File.Move(temp,file);}
            finally{if(File.Exists(temp))File.Delete(temp);}
        }
        byte[] LoadImage(string hash){
            try{string file=ImageFile(hash);if(new FileInfo(file).Length>128+2048*2048*4)return null;byte[] data=File.ReadAllBytes(file);Engine.ValidateBackground(data);return data;}catch{return null;}
        }
        static byte[] Crop(byte[] source,ref int[] frame){
            if(source==null){frame=null;return null;}
            try{return frame==null?source:BackgroundImage.Crop(source,new System.Windows.Int32Rect(frame[0],frame[1],frame[2],frame[3]));}catch{frame=null;return source;}
        }
        public bool Save(SavedPreferences state){
            try{
                Directory.CreateDirectory(root);var o=state.Selection;
                var metadata=new SavedPreferences{Language=state.Language,GamePath=state.GamePath,Page=state.Page,
                    MenuImage=SaveImage(o.MenuBackgroundSource??o.MenuBackground),ScreensImage=SaveImage(o.ScreensBackgroundSource??o.ScreensBackground),
                    Selection=new Options{Hud=o.Hud,ClickThrough=o.ClickThrough,Layout=o.Layout,Skin=o.Skin,Crosshair=o.Crosshair,
                        CrosshairChoice=o.CrosshairChoice,ColorChoice=o.ColorChoice,Color=o.Color,Size=o.Size,SkipIntro=o.SkipIntro,
                        Customization=o.Customization,Icons=o.Icons,BackgroundLogo=o.BackgroundLogo,MenuCrop=o.MenuCrop,ScreensCrop=o.ScreensCrop}};
                AtomicWrite(Path.Combine(root,"settings.json"),Encoding.UTF8.GetBytes(json.Serialize(metadata)));return true;
            }catch(Exception e){System.Diagnostics.Trace.WriteLine("Save preferences: "+e.Message);return false;}
        }
        public SavedPreferences Load(){
            try{
                string file=Path.Combine(root,"settings.json");if(new FileInfo(file).Length>512*1024)return null;
                var state=json.Deserialize<SavedPreferences>(File.ReadAllText(file));if(state==null||state.Schema!=1||state.Selection==null)return null;
                var o=state.Selection;
                if(!Engine.LayoutIds.Contains(o.Layout))o.Layout="compact";
                if(!Engine.SkinIds.Contains(o.Skin))o.Skin="dlc3";
                if(!Engine.Colors.ContainsKey(o.Color??""))o.Color="white";
                if(!Engine.Colors.ContainsKey(o.ColorChoice??""))o.ColorChoice=o.Color;
                o.Size=Math.Max(50,Math.Min(200,o.Size/5*5));o.RestoreBackground=false;
                o.Icons=(o.Icons??new Dictionary<string,int>()).Where(v=>Engine.IconIds.Contains(v.Key)&&v.Value>=10&&v.Value<=100).ToDictionary(v=>v.Key,v=>v.Value);
                if(o.Customization!=null)try{o.Customization.Validate();}catch{o.Customization=null;}
                o.MenuBackgroundSource=LoadImage(state.MenuImage);o.ScreensBackgroundSource=LoadImage(state.ScreensImage);
                o.MenuBackground=Crop(o.MenuBackgroundSource,ref o.MenuCrop);o.ScreensBackground=Crop(o.ScreensBackgroundSource,ref o.ScreensCrop);
                if(state.Language!="en")state.Language="ru";
                if(!new[]{"HudPage","CrossPage","IconsPage","BackgroundPage","AboutPage"}.Contains(state.Page))state.Page="HudPage";
                return state;
            }catch{return null;}
        }
    }
}
