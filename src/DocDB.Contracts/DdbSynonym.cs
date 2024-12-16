using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbSynonym : NamedDdbObject
{
    [JsonPropertyName("baseRef"), JsonProperty("baseRef")]
    public NamedDdbRef? BaseRef { get; set; }
    [JsonPropertyName("baseType"), JsonProperty("baseType")]
    public string? BaseType { get; set; }
    [JsonPropertyName("baseIsInSameDatabase"), JsonProperty("baseIsInSameDatabase")]
    public bool BaseIsInSameDatabase { get; set; }
    [JsonPropertyName("isSchemaOwned"), JsonProperty("isSchemaOwned")]
    public bool IsSchemaOwned { get; set; }
    [JsonPropertyName("owner"), JsonProperty("owner")]
    public string? Owner { get; set; }
}
