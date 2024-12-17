using DocDB.Contracts;
using Docfx.YamlSerialization;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Docfx.Plugins.DocDB;

public class DocDBViewModel
{
    public DocDBViewModel(DdbObject obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        Id = obj.Id;
        Uid = obj.Id;
        Name = obj is NamedDdbObject named ? named.Name : obj.Type;
        Type = obj.Type;
        Payload = obj;
    }

    [YamlMember(Alias = "id")]
    [JsonProperty("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [YamlMember(Alias = "uid")]
    [JsonProperty("uid")]
    [JsonPropertyName("uid")]
    public string Uid { get; set; } = null!;

    [YamlMember(Alias = "name")]
    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public string? Name { get; set; } = null!;

    [YamlMember(Alias = "description")]
    [JsonProperty("description")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "type")]
    [JsonProperty("type")]
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [YamlMember(Alias = "displayType")]
    [JsonProperty("displayType")]
    [JsonPropertyName("displayType")]
    public string DisplayType { get; set; } = null!;

    [YamlMember(Alias = "payload")]
    [JsonProperty("payload")]
    [JsonPropertyName("payload")]
    public DdbObject Payload { get; set; } = null!;

    [ExtensibleMember]
    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = null!;
}
