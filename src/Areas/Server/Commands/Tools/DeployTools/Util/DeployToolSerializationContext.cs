using System.Text.Json.Serialization;
using AzureMcp.Areas.Server.Commands.Tools.Models;
using AzureMcp.Areas.Server.Commands.Tools.DeployTools.Models;
using Json.Schema;
using ModelContextProtocol.Protocol;

namespace AzureMcp.Areas.Server.Commands.Tools.Utils;

[JsonSerializable(typeof(JsonSchema))]
[JsonSerializable(typeof(ListToolsResult))]
[JsonSerializable(typeof(IEnumerable<Tool>))]
[JsonSerializable(typeof(Dictionary<string, object?>))]
[JsonSerializable(typeof(BicepChecks))]
[JsonSerializable(typeof(RecommendConfigsParameters))]
[JsonSerializable(typeof(ServiceConfig))]
[JsonSerializable(typeof(DependencyConfig))]
[JsonSerializable(typeof(DeploymentPrecheckParameters))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true
)]
public partial class DeployToolSerializationContext : JsonSerializerContext
{
}