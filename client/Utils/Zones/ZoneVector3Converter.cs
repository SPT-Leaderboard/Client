using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace SPTLeaderboard.Utils.Zones
{
    public class ZoneVector3Converter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WriteEndObject();
        }

        public override Vector3 ReadJson(JsonReader reader, System.Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
        
            float x = jo["x"]?.Value<float>() ?? 0f;
            float y = jo["y"]?.Value<float>() ?? 0f;
            float z = jo["z"]?.Value<float>() ?? 0f;
        
            return new Vector3(x, y, z);
        }
    }
}
