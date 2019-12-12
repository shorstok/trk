using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace trackvisualizer.Config
{
    [DataContract]
    public class DirectorySettings
    {
        [JsonProperty("temp")] public string TempDownloadFolder { get; set; }
    }
}