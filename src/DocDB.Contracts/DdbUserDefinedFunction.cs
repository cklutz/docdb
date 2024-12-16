using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbUserDefinedFunction : DdbProcedureOrFunctionObject<DdbUserDefinedFunctionParameter>
{
    [JsonPropertyName("functionType"), JsonProperty("functionType")]
    public DdbUserDefinedFunctionType FunctionType { get; set; }
    [JsonPropertyName("returnsNullOnNullInput"), JsonProperty("returnsNullOnNullInput")]
    public bool ReturnsNullOnNullInput { get; set; }
}
