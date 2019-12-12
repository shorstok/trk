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
        public string SrtmBaseUrlTemplate { get; set; } = $"http://caper.ws/terrain/de-ferranti/1degree/{HeightmapTemplateTokens.SrtmZippedName}";
    }
}