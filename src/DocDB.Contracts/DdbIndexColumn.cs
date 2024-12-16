using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbIndexColumn : NamedDdbObject
{
    [JsonPropertyName("columnRef"), JsonProperty("columnRef")]
    public NamedDdbRef? ColumnRef { get; set; }

    [JsonPropertyName("isDescending"), JsonProperty("isDescending")]
    public bool IsDescending { get; set; }

    [JsonPropertyName("isIncluded"), JsonProperty("isIncluded")]
    public bool IsIncluded { get; set; }

    [JsonPropertyName("columnStoreOrderOrdinal"), JsonProperty("columnStoreOrderOrdinal")]
    public int ColumnStoreOrderOrdinal { get; set; }
}
