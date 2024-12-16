using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbUserDefinedTableType : TabularDdbObject<DdbTableColumn>
{
    [JsonPropertyName("collation"), JsonProperty("collation")]
    public string? Collation { get; set; }
    [JsonPropertyName("checks"), JsonProperty("checks")]
    public List<DdbCheckConstraint> Checks { get; set; } = [];

    [JsonPropertyName("isMemoryOptimized"), JsonProperty("isMemoryOptimized")]
    public bool IsMemoryOptimized { get; set; }
    [JsonPropertyName("isUserDefined"), JsonProperty("isUserDefined")]
    public bool IsUserDefined { get; set; }
    [JsonPropertyName("isSchemaOwned"), JsonProperty("isSchemaOwned")]
    public bool IsSchemaOwned { get; set; }
    [JsonPropertyName("owner"), JsonProperty("owner")]
    public string? Owner { get; set; }
    [JsonPropertyName("maxLength"), JsonProperty("maxLength")]
    public int? MaxLength { get; set; }
    [JsonPropertyName("isNullable"), JsonProperty("isNullable")]
    public bool IsNullable { get; set; }
}
