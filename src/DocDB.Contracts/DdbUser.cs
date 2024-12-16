using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbUser : NamedDdbObject
{
    [JsonPropertyName("userType"), JsonProperty("userType")]
    public string? UserType { get; set; }

    [JsonPropertyName("authenticationType"), JsonProperty("authenticationType")]
    public string? AuthenticationType { get; set; }

    [JsonPropertyName("hasDbAccess"), JsonProperty("hasDbAccess")]
    public bool HasDBAccess { get; set; }

    [JsonPropertyName("loginType"), JsonProperty("loginType")]
    public string? LoginType { get; set; }

    [JsonPropertyName("login"), JsonProperty("login")]
    public string? Login { get; set; }
}
