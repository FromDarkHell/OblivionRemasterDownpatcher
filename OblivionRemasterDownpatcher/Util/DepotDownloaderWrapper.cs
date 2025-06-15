using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OblivionRemasterDownpatcher.Util
{
    public class DepotDownloaderWrapper
    {
        public DepotDownloaderWrapper(string ExecutablePath) => this.ExecutablePath = ExecutablePath;

        public string ExecutablePath { get; set; }

        public bool ChangeVersion(string gamePath, string username, string? password, OblivionVersion newVersion)
        {
            var currentVersion = OblivionVersionManager.FromInstall(gamePath);
            if (currentVersion == null) return false;

            int currentIdx = OblivionVersionManager.AllVersions().FindIndex(x => x.ProductVersion == currentVersion.ProductVersion);
            int newIdx = OblivionVersionManager.AllVersions().FindIndex(x => x.ProductVersion == newVersion.ProductVersion);

            bool upgrading = currentIdx < newIdx;

            HashSet<string> filesToDelete = [];
            Dictionary<string, OblivionVersion> filesToAdd = [];

            void removeFiles(List<string> h)
            {
                filesToDelete.UnionWith(h);
                foreach (var file in h) filesToAdd.Remove(file);
            }
            void addFiles(List<string> h, OblivionVersion version)
            {
                foreach (var file in h) filesToAdd[file] = version;
                
                filesToDelete.RemoveWhere((x) => h.Contains(x));
            }

            if (upgrading)
            {
                for(int idx = currentIdx + 1; idx <= newIdx; idx++)
                {
                    var version = OblivionVersionManager.AllVersions()[idx];
                    removeFiles(version.Deleted);
                    addFiles(version.Added, version);
                    addFiles(version.Replaced, version);
                } 
            }
            else
            {
                for(int idx = currentIdx; idx > newIdx; idx--)
                {
                    var version = OblivionVersionManager.AllVersions()[idx];
                    removeFiles(version.Added);

                    var previousVersion = OblivionVersionManager.AllVersions()[idx - 1];
                    addFiles(version.Deleted, previousVersion);
                    addFiles(version.Replaced, previousVersion);
                }
            }

            Dictionary<OblivionVersion, HashSet<string>> manifests = [];
            foreach(var entry in filesToAdd)
            {
                if(!manifests.ContainsKey(entry.Value)) 
                    manifests[entry.Value] = [];

                manifests[entry.Value].Add(entry.Key);
            }

            foreach (var manifest in manifests)
            {
                var version = manifest.Key;
                var files = manifest.Value;

                string cachePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");

                using (FileStream stream = File.Create(cachePath))
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    foreach(string file in files)
                        writer.WriteLine(file);
                }

                RunDepotDownloader(
                    gamePath, username, password, 
                    version.Manifests.First(),
                    [$"-filelist \"{cachePath}\""]
                );
            }

            using (FileStream stream = File.Create(Path.Combine("steam_appid.txt")))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine(OblivionVersionManager.APP_ID);
            }

            return true;
        }
        
        public bool VerifyVersion(string gamePath, string username, string? password)
        {
            HashSet<string> filesToDelete = [];
            bool belowCurrent = true;

            var currentVersion = OblivionVersionManager.FromInstall(gamePath);
            if (currentVersion == null) return false;

            for (int idx = 0; idx < OblivionVersionManager.AllVersions().Count; idx++)
            {
                OblivionVersion version = OblivionVersionManager.AllVersions()[idx];

                if(belowCurrent)
                {
                    filesToDelete.UnionWith(version.Deleted);
                    filesToDelete.RemoveWhere(x => version.Added.Contains(x));
                }
                else
                {
                    filesToDelete.UnionWith(version.Added);
                }

                if (version.ProductVersion == currentVersion.ProductVersion) belowCurrent = false;
            }

            foreach (string file in filesToDelete)
            {
                try
                {
                    var filePath = Path.Combine(gamePath, file);
                    Console.WriteLine($"Deleting {filePath}");
                    File.Delete(filePath);
                }
                catch (DirectoryNotFoundException) { }
            }

            List<ulong> manifests = currentVersion.Manifests;

            foreach (var manifest in manifests)
            {
                RunDepotDownloader(gamePath, username, password, manifest, ["-validate"]);
            }

            return true;
        }
    
        public static string GetDefaultExecutable()
        {
            var executableDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var versionFile = Path.Combine(executableDirectory, "bin", "DepotDownloader-windows-x64", "DepotDownloader.exe");

            return versionFile;
        }

        private int RunDepotDownloader(string gamePath, string username, string? password, ulong manifest, string[] args, Action<string>? outputDataReceived = null)
        {
            var commandLine = $"-user \"{username}\"";

            if(password != null)
            {
                commandLine += $" -password \"{password}\"";
            }

            commandLine += $" -remember-password";
            commandLine += $" -app {OblivionVersionManager.APP_ID} -depot {OblivionVersionManager.DEPOT_ID} -manifest {manifest}";
            commandLine += $" -dir \"{gamePath}\"";

            foreach (var arg in args)
            {
                commandLine += $" {arg}";
            }

            Process ddProc = Process.Start(
                new ProcessStartInfo(ExecutablePath!, commandLine) { 
                    UseShellExecute = false,
                }
            )!;

            void OnExit(object sender, ExitEventArgs args) => ddProc.Kill();

            Application.Current.Exit += OnExit;
            ddProc.WaitForExit();
            Application.Current.Exit -= OnExit;

            return ddProc.ExitCode;
        }
    }
}
