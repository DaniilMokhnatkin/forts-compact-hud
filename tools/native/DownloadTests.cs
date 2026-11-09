using System;
using System.IO;
using FortsSetup;

static class DownloadTests {
    static int checks;
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
    static int Main(string[] args){
        string root=Path.GetFullPath(args[0]),folder=Path.Combine(root,"build","download-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try {
            string name=Matchroom.DownloadName(new Matchroom.Asset{Name="Forts.Setup.exe",Tag="v1.8.4"});
            Check(name=="Forts.Setup-1.8.4.exe","download filename includes the GitHub release version");
            Check(Matchroom.DownloadName(new Matchroom.Asset{Name="Matchroom.Next.v1.2.3.exe",Tag="v1.2.3"})=="Matchroom.Next.v1.2.3.exe","existing versioned asset names are not duplicated");
            bool rejected=false;try{Matchroom.DownloadName(new Matchroom.Asset{Name="Forts.Setup.exe",Tag="v1/../../bad"});}catch(InvalidDataException){rejected=true;}Check(rejected,"unsafe release tag cannot become a path");
            string old=Path.Combine(folder,"Forts.Setup.exe"),part=Path.Combine(folder,"new.part");File.WriteAllText(old,"old installer");File.WriteAllText(part,"new installer");
            using(var locked=new FileStream(old,FileMode.Open,FileAccess.Read,FileShare.Read)){
                string saved=Matchroom.CompleteDownload(part,folder,name);
                Check(Path.GetFileName(saved)==name&&File.ReadAllText(saved)=="new installer"&&File.ReadAllText(old)=="old installer","a locked previous installer is preserved while the new version is saved");
            }
            string existing=Path.Combine(folder,name);File.WriteAllText(part,"second download");
            using(var locked=new FileStream(existing,FileMode.Open,FileAccess.Read,FileShare.Read)){
                string saved=Matchroom.CompleteDownload(part,folder,name);
                Check(Path.GetFileName(saved)=="Forts.Setup-1.8.4 (1).exe"&&File.ReadAllText(existing)=="new installer"&&File.ReadAllText(saved)=="second download","repeated downloads do not overwrite a locked installer of the same version");
            }
            File.WriteAllText(part,"third download");Directory.CreateDirectory(Path.Combine(folder,"Forts.Setup-1.8.4 (2).exe"));
            Check(Path.GetFileName(Matchroom.CompleteDownload(part,folder,name))=="Forts.Setup-1.8.4 (3).exe","existing directories are also treated as occupied names");
            Check(!File.Exists(part),"completed temporary file is moved to its final name");
            Console.WriteLine(checks+" DOWNLOAD CHECKS PASSED");return 0;
        }catch(Exception e){Console.Error.WriteLine(e);return 1;}
        finally{if(folder.StartsWith(Path.Combine(root,"build")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))Directory.Delete(folder,true);}
    }
}
