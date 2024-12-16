using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public abstract class DdbProcedureOrFunctionObject<TParameter> : NamedDdbObject
    where TParameter : DdbParameterBase
{
    [JsonPropertyName("parameters"), JsonProperty("parameters")]
    public List<TParameter> Parameters { get; set; } = [];

    [JsonPropertyName("syntax"), JsonProperty("syntax")]
    public string? Syntax { get; set; }
}
