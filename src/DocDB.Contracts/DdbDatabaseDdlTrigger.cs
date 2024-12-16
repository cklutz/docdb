using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbDatabaseDdlTrigger : NamedDdbObject
{
    [JsonPropertyName("ansiNulls"), JsonProperty("ansiNulls")]
    public bool AnsiNulls { get; set; }
    [JsonPropertyName("quotedIdentifier"), JsonProperty("quotedIdentifier")]
    public bool QuotedIdentifier { get; set; }
    [JsonPropertyName("implementationType"), JsonProperty("implementationType")]
    public string? ImplementationType { get; set; }
    [JsonPropertyName("isEnabled"), JsonProperty("isEnabled")]
    public bool IsEnabled { get; set; }
    [JsonPropertyName("isEncrypted"), JsonProperty("isEncrypted")]
    public bool IsEncrypted { get; set; }
    [JsonPropertyName("notForReplication"), JsonProperty("notForReplication")]
    public bool NotForReplication { get; set; }
    [JsonPropertyName("text"), JsonProperty("text")]
    public string? Text { get; set; }

    [JsonPropertyName("assemblyName"), JsonProperty("assemblyName")]
    public string? AssemblyName { get; set; }
    [JsonPropertyName("assemblyRef"), JsonProperty("assemblyRef")]
    public NamedDdbRef? AssemblyRef { get; set; }
    [JsonPropertyName("className"), JsonProperty("className")]
    public string? ClassName { get; set; }
    [JsonPropertyName("methodName"), JsonProperty("methodName")]
    public string? MethodName { get; set; }
}
