using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace trackvisualizer.Config
{
    public static class HeightmapTemplateTokens
    {
        public const string SrtmZippedName = "%SRTM_ZIP%";
    }

    [DataContract]
    public class HeightmapSourceSettings
    {        
        [JsonProperty("active_provider_id")] public Guid? ActiveProviderId { get; set; }
        
        [JsonProperty("srtm_base_url")]
        public string SrtmBaseUrlTemplate { get; set; } = $@"https://dds.cr.usgs.gov/srtm/version2_1/SRTM3/Eurasia/{HeightmapTemplateTokens.SrtmZippedName}";
    }
}