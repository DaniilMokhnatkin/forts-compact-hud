using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace FortsSetup {
    // Downloads the newest Matchroom release straight from GitHub (no browser).
    public static class Matchroom {
        public const string Repo="DaniilMokhnatkin/MatchroomNext",KadrRepo="DaniilMokhnatkin/Kadr";
        public const string Page="https://github.com/"+Repo,KadrPage="https://github.com/"+KadrRepo;
        public const string Author="https://github.com/DaniilMokhnatkin";
        static Matchroom(){System.Net.ServicePointManager.SecurityProtocol|=(System.Net.SecurityProtocolType)3072;} // TLS 1.2 for GitHub
        public sealed class Asset {public string Tag,Name,Url,HashUrl;}
        // Picks the installer .exe and its optional .sha256.txt from a GitHub "latest release" JSON.
        public static Asset Pick(string json,string repo=Repo){
            string prefix="https://github.com/"+repo+"/releases/download/";
            var release=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(json);
            var result=new Asset{Tag=release.ContainsKey("tag_name")?release["tag_name"] as string:null};
            var assets=release.ContainsKey("assets")?release["assets"] as IEnumerable:null;
            if(assets==null)throw new InvalidDataException("No assets");
            foreach(var item in assets){
                var a=item as Dictionary<string,object>;if(a==null)continue;
                string name=a["name"] as string,url=a["browser_download_url"] as string;
                if(name==null||url==null||!url.StartsWith(prefix,StringComparison.Ordinal))continue;
                if(name.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)&&result.Name==null){result.Name=name;result.Url=url;}
                else if(name.EndsWith(".txt",StringComparison.OrdinalIgnoreCase)&&name.IndexOf("sha256",StringComparison.OrdinalIgnoreCase)>=0)result.HashUrl=url;
            }
            if(result.Name==null)throw new InvalidDataException("No installer in the release");
            // The name comes from GitHub; never let it carry a path.
            result.Name=Path.GetFileName(result.Name);
            return result;
        }
        public static string ExpectedHash(string text,string file=null){
            if(text==null)return null;
            if(file!=null)foreach(string line in text.Split('\n'))if(line.IndexOf(file,StringComparison.OrdinalIgnoreCase)>=0){string h=ExpectedHash(line);if(h!=null)return h;}
            foreach(string token in text.Split(new[]{' ','\t','\r','\n','*'},StringSplitOptions.RemoveEmptyEntries))
                if(token.Length==64&&System.Text.RegularExpressions.Regex.IsMatch(token,"^[0-9a-fA-F]{64}$"))return token.ToLowerInvariant();
            return null;
        }
        public static string DownloadsFolder(){return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads");}
        // Returns the saved file. Progress is 0..100. Throws IOException with a user-readable message.
        public static async Task<string> Download(Action<int> progress,string folder=null,string repo=Repo){
            folder=folder??DownloadsFolder();Directory.CreateDirectory(folder);
            using(var http=new HttpClient{Timeout=TimeSpan.FromMinutes(10)}){
                http.DefaultRequestHeaders.UserAgent.ParseAdd("FortsSetup");
                Asset asset;
                try{asset=Pick(await http.GetStringAsync("https://api.github.com/repos/"+repo+"/releases/latest"),repo);}
                catch(Exception e){throw new IOException("Не удалось получить последнюю версию. Проверьте подключение.",e);}
                string expected=null;
                if(asset.HashUrl!=null)try{expected=ExpectedHash(await http.GetStringAsync(asset.HashUrl),asset.Name);}catch{}
                string target=Path.Combine(folder,asset.Name),part=target+".part";
                try{
                    using(var response=await http.GetAsync(asset.Url,HttpCompletionOption.ResponseHeadersRead)){
                        response.EnsureSuccessStatusCode();long total=response.Content.Headers.ContentLength??-1,done=0;
                        using(var input=await response.Content.ReadAsStreamAsync())using(var output=File.Create(part)){
                            var buffer=new byte[81920];int n;while((n=await input.ReadAsync(buffer,0,buffer.Length))>0){await output.WriteAsync(buffer,0,n);done+=n;if(total>0)progress((int)(done*100/total));}
                        }
                    }
                    if(expected!=null){
                        string actual;using(var sha=SHA256.Create())using(var stream=File.OpenRead(part))actual=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
                        if(actual!=expected)throw new IOException("Контрольная сумма не совпала. Файл удалён, попробуйте ещё раз.");
                    }
                    if(File.Exists(target))File.Delete(target);File.Move(part,target);progress(100);return target;
                }catch(IOException){TryDelete(part);throw;}
                catch(Exception e){TryDelete(part);throw new IOException("Не удалось скачать файл. Проверьте подключение и повторите позже.",e);}
            }
        }
        static void TryDelete(string path){try{if(File.Exists(path))File.Delete(path);}catch{}}
    }
}
