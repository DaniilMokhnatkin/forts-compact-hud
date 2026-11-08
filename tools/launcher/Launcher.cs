// Forts Setup.exe: one file to send to friends. The installer (setup\app + payload)
// is embedded as a zip; it is unpacked once per version into
// %LOCALAPPDATA%\FortsSetup\<hash> and the PowerShell window is started from there.
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows.Forms;

[assembly: AssemblyTitle("Forts Setup")]
[assembly: AssemblyProduct("Forts Setup")]
[assembly: AssemblyDescription("Compact HUD and crosshair installer for Forts")]

static class Launcher {
    [STAThread]
    static int Main(string[] args) {
        try {
            byte[] zip;
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("app.zip"))
            using (var m = new MemoryStream()) { s.CopyTo(m); zip = m.ToArray(); }
            string hash;
            using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(zip)).Replace("-", "").Substring(0, 12);

            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FortsSetup");
            var dir = Path.Combine(root, hash);
            var script = Path.Combine(dir, "app", "setup.ps1");
            if (!File.Exists(Path.Combine(dir, ".complete"))) {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
                var tmp = dir + ".tmp";
                if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
                using (var m = new MemoryStream(zip))
                using (var archive = new ZipArchive(m)) archive.ExtractToDirectory(tmp);
                Directory.Move(tmp, dir);
                File.WriteAllText(Path.Combine(dir, ".complete"), hash);
                // Older versions are no longer needed.
                foreach (var old in Directory.GetDirectories(root))
                    if (!old.Equals(dir, StringComparison.OrdinalIgnoreCase)) try { Directory.Delete(old, true); } catch { }
            }

            var extra = "";
            foreach (var a in args) extra += " \"" + a.Replace("\"", "") + "\"";
            var psi = new ProcessStartInfo {
                FileName = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe"),
                Arguments = "-NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + script + "\"" + extra,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            Process.Start(psi);
            return 0;
        } catch (Exception e) {
            MessageBox.Show("Forts Setup could not start:\n" + e.Message, "Forts Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
}
