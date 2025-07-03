using System.Text.Json.Nodes;
using AzureMcp.Areas.Server.Commands.Tools.Utils;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;


namespace AzureMcp.Areas.Server.Commands.Tools;


[McpServerToolType]
public sealed class DeployAzdUpTool(ILogger<DeployAzdUpTool> logger) : McpServerTool
{
    private readonly ILogger<DeployAzdUpTool> _logger = logger;

    static readonly string ToolName = "deploy_azd_up";

    static readonly string ToolDescription = "Provision the recommended Azure resources and deploy project to Azure using AZD.";

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
                        Text = "Run the command `azd up` in the terminal to deploy the app.",
                    }
                ]
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeployAzdUpTool");

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
