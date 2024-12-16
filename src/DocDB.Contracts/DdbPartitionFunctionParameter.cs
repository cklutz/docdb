using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbPartitionFunctionParameter : NamedDdbObject
{
    [JsonPropertyName("collation"), JsonProperty("collation")]
    public string? Collation { get; set; }
    [JsonPropertyName("length"), JsonProperty("length")]
    public int? Length { get; set; }
    [JsonPropertyName("numericPrecision"), JsonProperty("numericPrecision")]
    public int NumericPrecision { get; set; }
    [JsonPropertyName("numericScale"), JsonProperty("numericScale")]
    public int NumericScale { get; set; }
}
