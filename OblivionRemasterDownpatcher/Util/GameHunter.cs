using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OblivionRemasterDownpatcher.Util
{
    static class GameHunter
    {
        public static string? FindEXE(uint APP_ID)
        {
            // Try check with a registry key first
            RegistryKey regLM = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            string regKey = $"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Steam App {APP_ID}";
            string? regLocation = (string?)regLM.OpenSubKey(regKey)?.GetValue("InstallLocation");
            if (regLocation != null)
            {
                string regFull = Path.Combine(regLocation);
                if (Directory.Exists(regFull)) return regFull;
            }

            // Find all steam libraries
            string steamapps = "C:\\Program Files (x86)\\Steam\\steamapps";
            string libraryFolders = Path.Combine(steamapps, "libraryfolders.vdf");
            if (!File.Exists(libraryFolders)) return null;

            List<string> steamLibraries = [steamapps];
            foreach (Match m in Regex.Matches(File.ReadAllText(libraryFolders), "\\t+\"\\d+\"\\t+\"(.+?)\""))
            {
                string dir = m.Groups[1].Value.Replace(@"\\", @"\");
                
                if (Directory.Exists(dir)) 
                    steamLibraries.Add(Path.Combine(dir, "steamapps"));
            }

            // Loop through all of our file paths
            foreach (string location in steamLibraries)
            {
                if (!File.Exists(Path.Combine(location, $"appmanifest_{APP_ID}.acf"))) continue;

                string exe = Path.Combine(location, "common", "Oblivion Remastered", "OblivionRemastered.exe");
                if (File.Exists(exe)) 
                    return Path.Combine(location, "common", "Oblivion Remastered");
            }

            return null;

        }
    }
}
