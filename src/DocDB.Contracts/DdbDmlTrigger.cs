using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbDmlTrigger : NamedDdbObject
{
    [JsonPropertyName("isEnabled"), JsonProperty("isEnabled")]
    public bool IsEnabled { get; set; }
    [JsonPropertyName("isSchemaBound"), JsonProperty("isSchemaBound")]
    public bool IsSchemaBound { get; set; }
    [JsonPropertyName("isInsteadOf"), JsonProperty("isInsteadOf")]
    public bool IsInsteadOf { get; set; }
    [JsonPropertyName("onDelete"), JsonProperty("onDelete")]
    public bool OnDelete { get; set; }
    [JsonPropertyName("onUpdate"), JsonProperty("onUpdate")]
    public bool OnUpdate { get; set; }
    [JsonPropertyName("onInsert"), JsonProperty("onInsert")]
    public bool OnInsert { get; set; }
}
