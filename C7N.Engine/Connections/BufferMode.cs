using System.Text.Json.Serialization;

namespace C7N.Engine.Connections;

[JsonConverter(typeof(JsonStringEnumConverter))] 
public enum BufferMode
{
    Latest,
    Queue,
    Stack,
    Batch,
    DropNewest,
    DropOldest,
}