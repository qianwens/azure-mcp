using System.Text.Json;
using Areas.Server.Commands.Tools.DeployTools.Util;
using AzureMcp.Areas.Server.Commands.Tools.Models;
using AzureMcp.Areas.Server.Commands.Tools.Utils;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;

namespace AzureMcp.Areas.Server.Commands.Tools;

[McpServerToolType]
public sealed class DeployRegionCheckTool(ILogger<DeployRegionCheckTool> logger) : McpServerTool
{
    private readonly ILogger<DeployRegionCheckTool> _logger = logger;

    static readonly string ToolName = "deploy_region_check";

    static readonly string ToolDescription = "Given a list of Azure resource types, this tool will return a list of regions where the resource types are available. Always get the user's subscription ID before calling this tool.";

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
    public override ValueTask<CallToolResult> InvokeAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<string, JsonElement>? args = request.Params?.Arguments;

        try
        {
            if (args == null)
            {
                return ValueTask.FromResult(new CallToolResult
                {
                    Content = [
                        new TextContentBlock
                        {
                            Text = "Error: No arguments provided"
                        }
                    ],
                    IsError = true
                });
            }

            var parameters = ParseRequestParameters(args);
    //         const availableRegions = await getAvailableRegionsForResourceTypes(params.resourceTypes, params.subscriptionId, params.cognitiveServiceProperties);
    //         const allRegions = Object.values(availableRegions).filter(regions => regions.length > 0);
    //         let toolResult = `If you are deploying an app, you MUST choose a region which exists in the following available region list for all resource types(because regions not listed are not available).DO NOT only choose a common region by yourself! Call the tool ${ ToolAgentNames.AzureQuotaCheck}
    //         to check if the selected region REALLY has enough quota for all resources. \n\n `;

    // // List regions that are available for all resource types
    // let validRegions = allRegions[0];
    //         for (const availableRegionSet of allRegions) {
    //             validRegions = validRegions.filter(x => availableRegionSet.includes(x));
    //         }
    //         toolResult += `Regions available for all resource types: ${ validRegions.length > 0 ? validRegions.join(', ') : 'None'}\n\n`;
            var availableRegions = DeployRegionCheckUtil.GenerateRegionCheckCommands(parameters.ResourceTypes, parameters.SubscriptionId, parameters.CognitiveServiceProperties);
            var allRegions = availableRegions.Values
                .Where(regions => regions.Count > 0)
                .SelectMany(regions => regions)
                .Distinct()
                .ToList();
            var toolResult = $"If you are deploying an app, you MUST choose a region which exists in the following available region list for all resource types (because regions not listed are not available). DO NOT only choose a common region by yourself! Call the tool `azure_quota_check` to check if the selected region REALLY has enough quota for all resources.\n\n";

            var commonValidRegions = availableRegions.Values
                .Aggregate((current, next) => current.Intersect(next).ToList());
            string regionList = commonValidRegions.Count > 0 ? string.Join(", ", commonValidRegions) : "None";
            result += $"\n\nRegions available for all resource types: {regionList}";

            return ValueTask.FromResult(new CallToolResult
            {
                Content = [
                    new TextContentBlock
                    {
                        Text = result
                    }
                ]
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeployRegionCheckTool");

            return ValueTask.FromResult(new CallToolResult
            {
                Content = [
                    new TextContentBlock
                    {
                        Text = $"Error: {ex.Message}"
                    }
                ],
                IsError = true
            });
        }
    }

    private static JsonElement GetInputSchema()
    {
        return JsonSerializer.SerializeToElement(AzureRegionCheckParametersSchema.Schema, DeployToolSerializationContext.Default.JsonObject);
    }


    private static AzureRegionCheckParameters ParseRequestParameters(IReadOnlyDictionary<string, JsonElement> args)
    {
        var parameters = new AzureRegionCheckParameters
        {
            SubscriptionId = args.GetValueOrDefault("subscriptionId").GetStringSafe(),
            ResourceTypes = args.GetValueOrDefault("resourceTypes").EnumerateArray()
                .Select(e => e.GetStringSafe())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray()
        };

        // Parse cognitive service properties if provided
        if (args.TryGetValue("cognitiveServiceProperties", out var cognitiveProps) &&
            cognitiveProps.ValueKind == JsonValueKind.Object)
        {
            parameters.CognitiveServiceProperties = new CognitiveServiceProperties
            {
                ModelName = cognitiveProps.GetProperty("modelName").GetStringSafe(),
                ModelVersion = cognitiveProps.GetProperty("modelVersion").GetStringSafe(),
                DeploymentSkuName = cognitiveProps.GetProperty("deploymentSkuName").GetStringSafe()
            };
        }

        return parameters;
    }
}
