using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using trackvisualizer.Service;

namespace trackvisualizer.Config
{
    [DataContract]
    public class TrekplannerConfiguration
    {
        private static readonly string ConfigFilename;

        private static readonly object ConfigReaderWriterLock = new object();

        static TrekplannerConfiguration()
        {
            ConfigFilename = Path.Combine(PathService.AppData, "tkrplanner-config.json");
        }

        [JsonProperty("report_generator_options")] 
        public TrackReportGeneratorOptions ReportGeneratorOptions { get; set; }  = new TrackReportGeneratorOptions();
        
        [JsonProperty("heightmap_settings")] 
        public HeightmapSourceSettings Heightmap { get; set; } = new HeightmapSourceSettings();

        [JsonProperty("directories")] 
        public DirectorySettings Directories { get;set; } = new DirectorySettings();

        [JsonProperty("last_tracks_list")] 
        public List<string> LastUsedTrackNames { get; set; } = new List<string>();

        [JsonProperty("last_track")] 
        public string LastLoadedTrackFilename { get; set; } 

        [JsonProperty("language")] 
        public string CurrentLanguage { get; set; } 

        public static TrekplannerConfiguration LoadOrCreate(bool saveIfNew = false)
        {
            lock (ConfigReaderWriterLock)
            {
                PathService.EnsurePathExists();

                if (!File.Exists(ConfigFilename))
                {
                    var result = new TrekplannerConfiguration();

                    if (saveIfNew)
                        result.Save();

                    return result;
                }

                var existing = JsonConvert.DeserializeObject<TrekplannerConfiguration>(
                    File.ReadAllText(ConfigFilename),
                    JsonFormatters.IndentedAutotype);

                //future: additional processing

                return existing;
            }
        }

        public bool Reload()
        {
            lock(ConfigReaderWriterLock)
            {
                if (!File.Exists(ConfigFilename))
                    return false;
            
                JsonConvert.PopulateObject(File.ReadAllText(ConfigFilename),
                    this,
                    JsonFormatters.IndentedAutotype);

                return true;
            }           
        }

        public void Save()
        {
            lock(ConfigReaderWriterLock)
                File.WriteAllText(ConfigFilename, JsonConvert.SerializeObject(this, JsonFormatters.IndentedAutotype));
        }
    }
}