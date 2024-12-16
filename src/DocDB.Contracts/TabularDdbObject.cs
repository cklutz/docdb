using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public abstract class TabularDdbObject<TColumn> : NamedDdbObject where TColumn : DdbColumnBase
{
    [JsonPropertyName("indexes"), JsonProperty("indexes")]
    public List<DdbIndex> Indexes { get; set; } = [];
    [JsonPropertyName("triggers"), JsonProperty("triggers")]
    public List<DdbDmlTrigger> Triggers { get; set; } = [];
    [JsonPropertyName("columns"), JsonProperty("columns")]
    public List<TColumn> Columns { get; set; } = [];
}
