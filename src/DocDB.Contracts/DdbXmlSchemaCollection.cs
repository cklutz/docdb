using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbXmlSchemaCollection : NamedDdbObject
{
    [JsonPropertyName("schemas"), JsonProperty("schemas")]
    public List<DdbXmlSchema> Schemas { get; set; } = [];
    [JsonPropertyName("schemasError"), JsonProperty("schemasError")]
    public string? SchemasError { get; set; }
}
