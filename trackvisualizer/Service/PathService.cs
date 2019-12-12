using System;
using System.IO;
using System.Linq;
using trackvisualizer.Config;

namespace trackvisualizer.Service
{
    public static class PathService
    {
        public static string AppData =>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "tkrekplanner-tkg");

        public static string StartupDir { get;}

        static PathService()
        {
            StartupDir = AppDomain.CurrentDomain.BaseDirectory;
        }

        public static void EnsurePathExists()
        {
            if (!Directory.Exists(AppData))
                Directory.CreateDirectory(AppData);
        }

        public static string GetOrCreateTempDirectory(TrekplannerConfiguration configuration)
        {
            var explicitTempFolder = configuration.Directories?.TempDownloadFolder;

            var folder = string.IsNullOrWhiteSpace(explicitTempFolder) ? 
                Path.Combine(AppData, "downloads") : 
                explicitTempFolder;

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return folder;
        }

        public static string MapPath(string source, params Tuple<string, string>[] tokenValues)
        {
            return tokenValues.Aggregate(source, (current, tuple) => current.Replace(tuple.Item1, tuple.Item2));
        }
    }
}