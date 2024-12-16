using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbForeignKey : NamedDdbObject
{
    [JsonPropertyName("columns"), JsonProperty("columns")]
    public List<NamedDdbRef> Columns { get; set; } = [];
    [JsonPropertyName("isChecked"), JsonProperty("isChecked")]
    public bool IsChecked { get; set; }
    [JsonPropertyName("referencedKey"), JsonProperty("referencedKey")]
    public string? ReferencedKey { get; set; }
    [JsonPropertyName("referencedTable"), JsonProperty("referencedTable")]
    public NamedDdbRef? ReferencedTable { get; set; }
    [JsonPropertyName("deleteAction"), JsonProperty("deleteAction")]
    public string? DeleteAction { get; set; }
    [JsonPropertyName("updateAction"), JsonProperty("updateAction")]
    public string? UpdateAction { get; set; }
}
