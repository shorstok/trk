using Newtonsoft.Json;

namespace trackvisualizer.Config
{
    public static class JsonFormatters
    {
        public static readonly JsonSerializerSettings IndentedAutotype = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto
        };

        public static readonly JsonSerializerSettings IndentedAutotypeIgnoreNull = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto
        };
    }
}