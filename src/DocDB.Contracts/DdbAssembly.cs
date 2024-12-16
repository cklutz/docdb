using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbAssembly : NamedDdbObject
{
    [JsonPropertyName("assemblyName"), JsonProperty("assemblyName")]
    public string? AssemblyName { get; set; }
    [JsonPropertyName("assemblySecurityLevel"), JsonProperty("assemblySecurityLevel")]
    public string? AssemblySecurityLevel { get; set; }
    [JsonPropertyName("culture"), JsonProperty("culture")]
    public string? Culture { get; set; }
    [JsonPropertyName("isVisible"), JsonProperty("isVisible")]
    public bool IsVisible { get; set; }
    [JsonPropertyName("owner"), JsonProperty("owner")]
    public string? Owner { get; set; }
    [JsonPropertyName("publicKey"), JsonProperty("publicKey")]
    public string? PublicKey { get; set; }
    [JsonPropertyName("version"), JsonProperty("version")]
    public string? Version { get; set; }
    [JsonPropertyName("fileNames"), JsonProperty("fileNames")]
    public List<string> FileNames { get; set; } = [];
}
