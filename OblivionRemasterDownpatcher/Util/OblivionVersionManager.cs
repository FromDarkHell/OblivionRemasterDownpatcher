using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OblivionRemasterDownpatcher.Util
{
    public class OblivionVersion
    {
        /// <summary>
        /// This responds to the "human" safe name for the game version
        /// </summary>
        public required string Name { get; set; }
        
        /// <summary>
        /// This is the exe version of `OblivionRemastered/Win64/OblivionRemastered-Win64-Shipping.exe`
        /// </summary>
        public required string ProductVersion { get; set; }

        /// <summary>
        /// A list of all of the manifests for this specific version
        /// Typically, each update will only come with one manifest
        /// </summary>
        public required List<ulong> Manifests { get; set; }
        
        /// <summary>
        /// This is a list of files added to the manifest *since* the last update
        /// </summary>
        public required List<string> Added { get; set; }

        /// <summary>
        /// This is a list of all the files deleted *since* the previous update
        /// </summary>
        public required List<string> Deleted { get; set; }
        
        /// <summary>
        /// This is a list of all the files which were replaced *since* the previous update
        /// </summary>
        public required List<string> Replaced { get; set; }
    }
    
    static class OblivionVersionManager
    {
        /// <summary>
        /// The Steam App ID used for accessing depots/etc for the game
        /// </summary>
        public const uint APP_ID = 2623190;

        /// <summary>
        /// The steam depot ID used for the main game
        /// </summary>
        public const uint DEPOT_ID = 2623191;

        private static List<OblivionVersion>? _AllVersions;
        public static List<OblivionVersion> AllVersions()
        {
            if(_AllVersions == null)
            {
                var executableDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var versionFile = Path.Combine(executableDirectory, "Config", "OblivionVersions.json");

                var versionContents = File.ReadAllText(versionFile);
                _AllVersions = JsonConvert.DeserializeObject<List<OblivionVersion>>(versionContents);
            }

            return _AllVersions!;
        }

        public static OblivionVersion? FromInstall(string installPath)
        {
            var gameExe = Path.Combine(installPath, "OblivionRemastered", "Binaries", "Win64", "OblivionRemastered-Win64-Shipping.exe");
            var productVersion = FileVersionHelper.GetProductVersion(gameExe);

            return AllVersions().FirstOrDefault(x => x.ProductVersion == productVersion, null);
        }
    }
}
