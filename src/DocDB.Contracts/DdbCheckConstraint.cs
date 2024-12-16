using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbCheckConstraint : NamedDdbObject
{
    [JsonPropertyName("isChecked"), JsonProperty("isChecked")]
    public bool IsChecked { get; set; }
    [JsonPropertyName("isEnabled"), JsonProperty("isEnabled")]
    public bool IsEnabled { get; set; }
    [JsonPropertyName("constraintText"), JsonProperty("constraintText")]
    public string? ConstraintText { get; set; }
}
