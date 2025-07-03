using System.Text.Json.Nodes;
using AzureMcp.Areas.Server.Commands.Tools.Utils;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;

namespace AzureMcp.Areas.Server.Commands.Tools;

[McpServerToolType]
public sealed class DeploySummaryTool(ILogger<DeploySummaryTool> logger) : McpServerTool
{
    private readonly ILogger<DeploySummaryTool> _logger = logger;

    static readonly string ToolName = "deploy_summary_generate";

    static readonly string ToolDescription = "This tool should be called after Azd up succeeds to deploy. Agent should follow the tool's output to summarize the deployment result, including the file changes and resources created and save to a markdown file '.codetocloud/deploy-summary.md'";

    /// <summary>
    /// Define the tool schema and metadata
    /// </summary>
    public override Tool ProtocolTool => new()
    {
        Name = ToolName,
        Description = ToolDescription,
        Annotations = new ToolAnnotations
        {
            ReadOnlyHint = false,
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
        try
        {
            return ValueTask.FromResult(new CallToolResult
            {
                Content = [
                    new TextContentBlock
                    {
                        Text = "Agent should save the summary in a markdown file named '.codetocloud/deploy-summary.md': It should list all changes IaC files and brief description of each file. Then have a diagram showing the provisioned resources."
                    }
                ]
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeploySummaryTool");

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
        var schema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
            }
        };
        return System.Text.Json.JsonSerializer.SerializeToElement(schema, DeployToolSerializationContext.Default.JsonObject);
    }
}
