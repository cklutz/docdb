using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbStoredProcedureParameter : DdbParameterBase
{
    [JsonPropertyName("isOutputParameter"), JsonProperty("isOutputParameter")]
    public bool IsOutputParameter { get; set; }
    [JsonPropertyName("isCursorParameter"), JsonProperty("isCursorParameter")]
    public bool IsCursorParameter { get; set; }
}
