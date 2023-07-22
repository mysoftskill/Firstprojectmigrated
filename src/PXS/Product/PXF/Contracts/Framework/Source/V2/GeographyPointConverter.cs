// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.DataContracts.V2
{
    using System;
    using System.Linq;

    using System.Spatial;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class GeographyPointConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            if (objectType == typeof(GeographyPoint))
                return true;
            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jsonObject = JObject.Load(reader);
            double? alt = null;
            JToken token;
            if (jsonObject.TryGetValue("altitude", out token))
                alt = token.ToObject<double?>();
            GeographyPoint point = GeographyPoint.Create(jsonObject.Value<double>("latitude"), jsonObject.Value<double>("longitude"), alt ?? 0.0);
            return point;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JToken t = JToken.FromObject(value);
            if (t.Type != JTokenType.Object)
                t.WriteTo(writer);
            else
            {
                var point = (GeographyPoint)value;
                writer.WriteStartObject();
                writer.WritePropertyName("latitude");
                writer.WriteValue(point.Latitude);
                writer.WritePropertyName("longitude");
                writer.WriteValue(point.Longitude);
                writer.WritePropertyName("altitude");
                writer.WriteValue(point.Z);
                writer.WriteEndObject();
            }
        }
    }
}
