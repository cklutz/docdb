using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbDatabase : NamedDdbObject
{
    [JsonPropertyName("productInformation"), JsonProperty("productInformation")]
    public string? ProductInformation { get; set; }

    [JsonPropertyName("productType"), JsonProperty("productType")]
    public DdbServerProductType ProductType { get; set; }

    [JsonPropertyName("collation"), JsonProperty("collation")]
    public string? Collation { get; set; }

    [JsonPropertyName("isCloudHosted"), JsonProperty("isCloudHosted")]
    public bool IsCloudHosted { get; set; }

    [JsonPropertyName("fileGroups"), JsonProperty("fileGroups")]
    public List<DdbFileGroup> FileGroups { get; set; } = [];

    [JsonPropertyName("dataFiles"), JsonProperty("dataFiles")]
    public List<DdbDataFile> DataFiles { get; set; } = [];

    [JsonPropertyName("logFiles"), JsonProperty("logFiles")]
    public List<DdbLogFile> LogFiles { get; set; } = [];

    [JsonPropertyName("options"), JsonProperty("options")]
    public List<DdbOptionCategory> Options { get; set; } = [];
}
