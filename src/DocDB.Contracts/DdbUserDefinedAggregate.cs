using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbUserDefinedAggregate : DdbProcedureOrFunctionObject<DdbUserDefinedAggregateParameter>
{
    [JsonPropertyName("assemblyName"), JsonProperty("assemblyName")]
    public string? AssemblyName { get; set; }
    [JsonPropertyName("assemblyRef"), JsonProperty("assemblyRef")]
    public NamedDdbRef? AssemblyRef { get; set; }
    [JsonPropertyName("className"), JsonProperty("className")]
    public string? ClassName { get; set; }
    [JsonPropertyName("isSchemaOwned"), JsonProperty("isSchemaOwned")]
    public bool IsSchemaOwned { get; set; }
    [JsonPropertyName("owner"), JsonProperty("owner")]
    public string? Owner { get; set; }

    [JsonPropertyName("dataType"), JsonProperty("dataType")]
    public string? ReturnDataType { get; set; }
    [JsonPropertyName("dataTypeRef"), JsonProperty("dataTypeRef")]
    public NamedDdbRef? ReturnDataTypeRef { get; set; }
}
