using Newtonsoft.Json;
using System.Collections;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public abstract class DdbObject : IDdbRef
{
    protected DdbObject()
    {
        Type = GetTypeTag(GetType());
    }

    [JsonPropertyName("databaseId"), JsonProperty("databaseId")]
    public string DatabaseId { get; set; } = null!;

    [JsonPropertyName("id"), JsonProperty("id")]
    public string Id { get; set; } = null!;
    [JsonPropertyName("type"), JsonProperty("type")]
    public string Type { get; }
    [JsonPropertyName("description"), JsonProperty("description")]
    public string? Description { get; set; }
    [JsonPropertyName("script"), JsonProperty("script")]
    public string? Script { get; set; }
    [JsonPropertyName("schemaVersion"), JsonProperty("schemaVersion")]
    public string? SchemaVersion { get; set; }
    [JsonPropertyName("lastSchemaModificationAt"), JsonProperty("lastSchemaModificationAt")]
    public DateTime LastSchemaModificationAt { get; set; }

    public override bool Equals(object? obj) => obj is DdbObject dbo && dbo.Id == Id && dbo.Type == Type;
    public override int GetHashCode() => HashCode.Combine(Id, Type);
    public override string ToString() => Id;

    public static Dictionary<string, Type> GetTypeMappings()
    {
        return typeof(DdbObject).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && t.IsClass && t.IsAssignableTo(typeof(DdbObject)))
            .ToDictionary(GetTypeTag, t => t);
    }

    private static string GetTypeTag(Type type) => GetTypeTag(type, false);
    public static string GetTypeTag<T>(bool isRef = false) where T : DdbObject => GetTypeTag(typeof(T), isRef);
    public static string GetTypeTag(Type type, bool isRef)
    {
        var span = type.Name.AsSpan();
        if (span.StartsWith("Ddb"))
        {
            span = span.Slice(3);
        }

        if (isRef)
        {
            return string.Concat(span, "Ref");
        }

        return span.ToString();
    }

    public virtual Dictionary<string, string> GetAllIds(bool includeSelf = true)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var queue = new Queue<DdbObject>();
        queue.Enqueue(this);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            bool shouldContinue = false;
            if (includeSelf || !ReferenceEquals(this, current))
            {
                shouldContinue = result.TryAdd(current.Id, current is NamedDdbObject named ? named.Name : current.Type);
            }

            if (shouldContinue)
            {
                var properties = current.GetType()
                    .GetProperties()
                    .Where(p => typeof(DdbObject).IsAssignableFrom(p.PropertyType) ||
                           (p.PropertyType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(p.PropertyType)));

                foreach (var property in properties)
                {
                    var value = property.GetValue(current);

                    if (value is DdbObject DdbObjectValue)
                    {
                        queue.Enqueue(DdbObjectValue);
                    }
                    else if (value is IEnumerable enumerable)
                    {
                        foreach (var item in enumerable)
                        {
                            if (item is DdbObject DdbObjectItem)
                            {
                                queue.Enqueue(DdbObjectItem);
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
}
