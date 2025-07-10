using System.Text.Json;
using Areas.Server.Commands.Tools.DeployTools.Util;
using AzureMcp.Areas.Server.Commands.Tools.DeployTools.Util;
using AzureMcp.Areas.Server.Commands.Tools.Models;
using AzureMcp.Areas.Server.Commands.Tools.Utils;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;

namespace AzureMcp.Areas.Server.Commands.Tools;

[McpServerToolType]
public sealed class DeployQuotaCheckTool(ILogger<DeployQuotaCheckTool> logger) : McpServerTool
{
    private readonly ILogger<DeployQuotaCheckTool> _logger = logger;

    static readonly string ToolName = "azure_quota_check";

    static readonly string ToolDescription = "Given a subscription ID, region, and list of Azure resource types, this tool will check if there is sufficient quota available in the specified region for the resource types. Always verify the region is valid for the resource types before calling this tool.";

    /// <summary>
    /// Define the tool schema and metadata
    /// </summary>
    public override Tool ProtocolTool => new()
    {
        Name = ToolName,
        Description = ToolDescription,
        Annotations = new ToolAnnotations
        {
            ReadOnlyHint = true,
            DestructiveHint = false,
            IdempotentHint = true
        },
        InputSchema = GetInputSchema()
    };

    /// <summary>
    /// Handle tool invocation
    /// </summary>
    public async override ValueTask<CallToolResult> InvokeAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<string, JsonElement>? args = request.Params?.Arguments;

        try
        {
            if (args == null)
            {
                return new CallToolResult
                {
                    Content = [
                        new TextContentBlock
                        {
                            Text = "Error: No arguments provided"
                        }
                    ],
                    IsError = true
                };
            }

            var parameters = ParseRequestParameters(args);

            var quotaByResourceTypes = await AzureQuotaService.GetAzureQuotaAsync(
                parameters.ResourceTypes,
                parameters.SubscriptionId,
                parameters.Region);

            var toolResult = $"The quota info for the specified resource types in subscription: {parameters.SubscriptionId} and location: {parameters.Region} are:\n\n" +
                string.Join("\n", quotaByResourceTypes.Select(kvp =>
                {
                    var resourceType = kvp.Key;
                    var quotas = kvp.Value;

                    if (quotas.Count == 0)
                    {
                        return $"- {resourceType}: this resource type has no quota limitation";
                    }

                    return string.Join("\n", quotas.Select(quota =>
                        $"- {resourceType}: {quota.Name}\n" +
                        $"  - Current: {quota.Used}, Limit: {quota.Limit}{(string.IsNullOrEmpty(quota.Unit) ? "" : $" {quota.Unit}")}"));
                }));

            _logger.LogInformation("Quota check result: {ToolResult}", toolResult);

            return new CallToolResult
            {
                Content = [
                    new TextContentBlock
                    {
                        Text = toolResult
                    }
                ]
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeployQuotaCheckTool");

            return new CallToolResult
            {
                Content = [
                    new TextContentBlock
                    {
                        Text = $"Error: {ex.Message}"
                    }
                ],
                IsError = true
            };
        }
    }

    private static JsonElement GetInputSchema()
    {
        return JsonSerializer.SerializeToElement(AzureQuotaCheckParametersSchema.Schema, DeployToolSerializationContext.Default.JsonObject);
    }

    private static AzureQuotaCheckParameters ParseRequestParameters(IReadOnlyDictionary<string, JsonElement> args)
    {
        var parameters = new AzureQuotaCheckParameters
        {
            SubscriptionId = args.GetValueOrDefault("subscriptionId").GetStringSafe(),
            Region = args.GetValueOrDefault("region").GetStringSafe(),
            ResourceTypes = args.GetValueOrDefault("resourceTypes").EnumerateArray()
                .Select(e => e.GetStringSafe())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList()
        };

        return parameters;
    }
}
