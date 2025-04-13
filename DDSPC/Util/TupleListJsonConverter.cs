namespace DDSPC.Util;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

public class TupleListJsonConverter : JsonConverter<List<(int, int)>>
{
    public override List<(int, int)> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = new List<(int, int)>();

        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected StartArray token");

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                return result;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected StartObject token");

            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected PropertyName token");

            reader.Read();
            int item1 = reader.GetInt32();

            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected PropertyName token");

            reader.Read();
            int item2 = reader.GetInt32();

            reader.Read(); // EndObject
            result.Add((item1, item2));
        }

        throw new JsonException("Unexpected end of JSON");
    }

    public override void Write(Utf8JsonWriter writer, List<(int, int)> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var (item1, item2) in value)
        {
            writer.WriteStartObject();
            writer.WriteNumber("Item1", item1);
            writer.WriteNumber("Item2", item2);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }
}