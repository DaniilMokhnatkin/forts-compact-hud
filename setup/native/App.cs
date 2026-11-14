using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Linq;

[assembly:AssemblyTitle("Forts Setup")]
[assembly:AssemblyProduct("Forts Setup")]
[assembly:AssemblyDescription("Native HUD and crosshair installer for Forts")]
[assembly:AssemblyVersion("1.17.1.0")]
[assembly:AssemblyFileVersion("1.17.1.0")]
namespace FortsSetup {
    public static class EmbeddedPack {
        public static string Extract() {
            byte[] data;using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("pack.zip"))using(var m=new MemoryStream()){if(s==null)throw new IOException("В установщике отсутствуют ресурсы.");s.CopyTo(m);data=m.ToArray();}
            string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(data)).Replace("-","").Substring(0,16);
            string parent=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FortsSetup","Native");Directory.CreateDirectory(parent);
            string root=Path.Combine(parent,hash);
            using(var mutex=new System.Threading.Mutex(false,"Local\\FortsSetupNative-"+hash)) {
                bool locked=false;
                try {try {locked=mutex.WaitOne(TimeSpan.FromSeconds(30));}catch(System.Threading.AbandonedMutexException){locked=true;}if(!locked)throw new IOException("Другой установщик ещё распаковывает ресурсы. Повтори запуск.");
                    if(File.Exists(Path.Combine(root,".complete")))return root;
                    string temp=Path.Combine(parent,hash+"-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
                    try {
                        using(var memory=new MemoryStream(data))using(var zip=new ZipArchive(memory,ZipArchiveMode.Read))foreach(var entry in zip.Entries){if(String.IsNullOrEmpty(entry.Name))continue;string path=Engine.SafePath(temp,entry.FullName);Directory.CreateDirectory(Path.GetDirectoryName(path));entry.ExtractToFile(path);}
                        File.WriteAllText(Path.Combine(temp,".complete"),hash);
                        if(Directory.Exists(root))throw new IOException("Незавершённая распаковка ресурсов: "+root);
                        Directory.Move(temp,root);
                    } finally {if(Directory.Exists(temp)&&Path.GetFullPath(temp).StartsWith(Path.GetFullPath(parent)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))Directory.Delete(temp,true);}
                } finally {if(locked)mutex.ReleaseMutex();}
            }
            return root;
        }
    }
    public static class Program {
        static string Arg(string[] args,string name) {for(int i=0;i+1<args.Length;i++)if(args[i].TrimStart('-').Equals(name,StringComparison.OrdinalIgnoreCase))return args[i+1];return null;}
        [STAThread] public static int Main(string[] args) {
            if(args.Length>0&&args[0]=="--apply-update")return SelfUpdate.Run(args);
            try {
                var app=new Application{ShutdownMode=ShutdownMode.OnLastWindowClose};
                string game=Arg(args,"game")??Engine.FindGame();
                string language=Arg(args,"lang")??Engine.DefaultLanguage(game);if(language!="en")language="ru";
                string pack=EmbeddedPack.Extract();
                var controller=new WindowController(pack,game,language,args.Any(a=>a.TrimStart('-').Equals("NoMotion",StringComparison.OrdinalIgnoreCase)));
                app.DispatcherUnhandledException+=(s,e)=>{MessageBox.Show(e.Exception.Message,"Forts Setup",MessageBoxButton.OK,MessageBoxImage.Error);e.Handled=true;};
                return app.Run(controller.Window);
            } catch(Exception e){MessageBox.Show(e.Message,"Forts Setup",MessageBoxButton.OK,MessageBoxImage.Error);return 1;}
        }
    }
    static class Glass {
        [StructLayout(LayoutKind.Sequential)] struct Margins {public int L,R,T,B;}
        [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
        [DllImport("dwmapi.dll")]static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd,ref Margins margins);
        [DllImport("user32.dll")]static extern int GetWindowLong(IntPtr hwnd,int index);
        [DllImport("user32.dll")]static extern int SetWindowLong(IntPtr hwnd,int index,int value);
        [DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int w,int h,uint flags);
        public static void Apply(Window window) {
            var hwnd=new WindowInteropHelper(window).Handle;
            SetWindowLong(hwnd,-16,GetWindowLong(hwnd,-16)&~0x80000); // WS_SYSMENU: custom caption buttons only
            SetWindowPos(hwnd,IntPtr.Zero,0,0,0,0,0x37); // refresh non-client frame without moving/activating
            HwndSource.FromHwnd(hwnd).CompositionTarget.BackgroundColor=System.Windows.Media.Colors.Transparent;
            var m=new Margins{L=-1,R=-1,T=-1,B=-1};DwmExtendFrameIntoClientArea(hwnd,ref m);
            int value=2;DwmSetWindowAttribute(hwnd,33,ref value,4);value=3;DwmSetWindowAttribute(hwnd,38,ref value,4);
        }
    }
}
