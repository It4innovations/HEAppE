﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace HEAppE.RestUtils.JsonConvertors
{
    public class SingleValueOrArrayConvertor<T> : JsonConverter
    {
        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);
            // If is array convert to list of objects.
            if (token.Type == JTokenType.Array)
            {
                return token.ToObject<List<T>>();
            }

            // Otherwise return list with single object.
            return new List<T> { token.ToObject<T>() };
        }

        public override bool CanConvert(Type objectType) => (objectType == typeof(List<T>));

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}