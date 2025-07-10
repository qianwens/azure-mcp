using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Areas.Server.Commands.Tools.DeployTools.Util;
using AzureMcp.Areas.Server.Commands.Tools.Models;
using AzureMcp.Areas.Server.Commands.Tools.Utils;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;

namespace AzureMcp.Areas.Server.Commands.Tools;

[McpServerToolType]
public sealed class GetAzdAppLogTool(ILogger<GetAzdAppLogTool> logger) : McpServerTool
{
    private readonly ILogger<GetAzdAppLogTool> _logger = logger;

    static readonly string ToolName = "get_azd_app_logs";

    static readonly string ToolDescription = "This tool helps fetch logs from log analytics workspace for Container Apps, App Services, function apps that were deployed through azd. Invoke this tool directly after a successful `azd up` or when user prompts to check the app's status or provide errors in the deployed apps.";

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
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "")]
    public override async ValueTask<CallToolResult> InvokeAsync(
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

            // Parse optional date parameters
            DateTime? startTime = null;
            DateTime? endTime = null;

            if (!string.IsNullOrEmpty(parameters.StartTime) && DateTime.TryParse(parameters.StartTime, out var parsedStartTime))
            {
                startTime = parsedStartTime;
            }

            if (!string.IsNullOrEmpty(parameters.EndTime) && DateTime.TryParse(parameters.EndTime, out var parsedEndTime))
            {
                endTime = parsedEndTime;
            }

            // Call our method to retrieve logs
            string result = await AzdResourceLogService.GetAzdResourceLogsAsync(
                parameters.WorkspaceFolder,
                parameters.AzdEnvName,
                parameters.SubscriptionId,
                startTime,
                endTime,
                parameters.Limit);

            return new CallToolResult
            {
                Content = [
                    new TextContentBlock
                    {
                        Text = result
                    }
                ]
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAzdAppLogTool");

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
        return System.Text.Json.JsonSerializer.SerializeToElement(GetAzdAppLogsParametersSchema.Schema, DeployToolSerializationContext.Default.JsonObject);
    }

    private static GetAzdAppLogsParameters ParseRequestParameters(IReadOnlyDictionary<string, JsonElement> args)
    {
        var parameters = new GetAzdAppLogsParameters
        {
            WorkspaceFolder = args.GetValueOrDefault("workspaceFolder").GetStringSafe() ?? string.Empty,
            AzdEnvName = args.GetValueOrDefault("azdEnvName").GetStringSafe() ?? string.Empty,
            SubscriptionId = args.GetValueOrDefault("subscriptionId").GetStringSafe() ?? string.Empty,
            StartTime = args.GetValueOrDefault("startTime").GetStringSafe(),
            EndTime = args.GetValueOrDefault("endTime").GetStringSafe(),
            Limit = args.GetValueOrDefault("limit").ValueKind == JsonValueKind.Number ? args.GetValueOrDefault("limit").GetInt32() : null
        };

        return parameters;
    }
}
