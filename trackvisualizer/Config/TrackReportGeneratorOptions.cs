using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace trackvisualizer.Config
{
    [DataContract]
    public class TrackReportGeneratorOptions
    {
        [JsonProperty("FilterPassesByName")]
        public bool FilterPassesByName { get; set; } = true;
        
        [JsonProperty("SliceptKickOutRadiusMeters")]
        public double? SliceptKickOutRadiusMeters { get; set; }

        [JsonProperty("SegDistance")]
        public bool SegDistance { get; set; } = true;

        [JsonProperty("RealTimes")]
        public bool RealTimes { get; set; } = true;

        [JsonProperty("ShowComments")]
        public bool Comments { get; set; } = true;

        [JsonProperty("AscDescCalcAccurate")]
        public bool AscDescCalcAccurate { get; set; } = false;

        [JsonProperty("LebedevSpeedKmH")]
        public double LebedevSpeedKmH { get; set; } = 3.0;
    }
}