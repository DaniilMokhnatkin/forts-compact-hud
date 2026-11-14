using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace FortsSetup {
    public static class SelfUpdate {
        static string Quote(string path){return "\""+path+"\"";}
        public static void Start(string downloaded){
            string target=Assembly.GetExecutingAssembly().Location;
            string dir=Path.Combine(Path.GetTempPath(),"FortsSetupUpdates",Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            string helper=Path.Combine(dir,"updater.exe");
            File.Copy(target,helper);
            try{
                var process=Process.Start(new ProcessStartInfo(helper,"--apply-update "+Process.GetCurrentProcess().Id+" "+Quote(Path.GetFullPath(downloaded))+" "+Quote(target)){UseShellExecute=false,CreateNoWindow=true});
                if(process==null)throw new IOException("Не удалось запустить обновление.");
                process.Dispose();
            }catch{try{File.Delete(helper);Directory.Delete(dir);}catch{}throw;}
        }
        public static void Apply(string source,string target,Action<string> launch){
            source=Path.GetFullPath(source);target=Path.GetFullPath(target);
            if(String.Equals(source,target,StringComparison.OrdinalIgnoreCase))throw new IOException("Некорректные пути обновления.");
            string staged=target+".new-"+Guid.NewGuid().ToString("N"),backup=target+".old-"+Guid.NewGuid().ToString("N");
            bool replaced=false;
            try{
                File.Copy(source,staged);
                File.Replace(staged,target,backup);replaced=true;
                launch(target);
                try{File.Delete(backup);}catch{}
                try{File.Delete(source);}catch{}
            }catch{
                if(replaced)File.Replace(backup,target,null);
                throw;
            }finally{try{if(File.Exists(staged))File.Delete(staged);}catch{}}
        }
        public static int Run(string[] args){
            try{
                if(args.Length!=4)throw new ArgumentException("Некорректные параметры обновления.");
                int pid=Int32.Parse(args[1]);
                try{using(var process=Process.GetProcessById(pid)){if(!process.WaitForExit(60000))throw new IOException("Предыдущая версия не закрылась.");}}
                catch(ArgumentException){}
                Apply(args[2],args[3],path=>{var p=Process.Start(new ProcessStartInfo(path){UseShellExecute=true,WorkingDirectory=Path.GetDirectoryName(path)});if(p==null)throw new IOException("Не удалось запустить обновлённый сетап.");p.Dispose();});
                return 0;
            }catch(Exception e){System.Windows.MessageBox.Show("Не удалось обновить Forts Setup: "+e.Message+"\nСкачанный файл сохранён.","Forts Setup",System.Windows.MessageBoxButton.OK,System.Windows.MessageBoxImage.Error);return 1;}
        }
    }
}
