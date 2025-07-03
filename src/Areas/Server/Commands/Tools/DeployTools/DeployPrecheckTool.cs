using System.Text.Json.Nodes;
using Areas.Server.Commands.Tools.DeployTools.Util;
using AzureMcp.Areas.Server.Commands.Tools.DeployTools.Models;
using AzureMcp.Areas.Server.Commands.Tools.Utils;
using Json.More;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using Newtonsoft.Json;


namespace AzureMcp.Areas.Server.Commands.Tools;

[McpServerToolType]
public sealed class DeployPrecheckTool(ILogger<DeployPrecheckTool> logger) : McpServerTool
{
    private readonly ILogger<DeployPrecheckTool> _logger = logger;

    static readonly string ToolName = "deploy_iac_precheck";

    static readonly string ToolDescription = "This tool ensures the created infrastructure files are ready for deployment. Scan the `./infra` folder to fill in the checks. Please run this before proceeding with deployment after Bicep files have been created. Run this every time you make changes until you get success. Otherwise, you cannot deploy. Call get_errors on all Bicep files before calling this tool.";

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
        var args = request.Params?.Arguments;

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
            // Extract parameters from the request build parameters from DeployPrecheckParameters
            var (inputError, param) = ParseRequestParameters(args);
            if (!string.IsNullOrEmpty(inputError))
            {
                return ValueTask.FromResult(new CallToolResult
                {
                    Content = [
                        new TextContentBlock
                        {
                            Text = inputError
                        }
                    ],
                    IsError = true
                });
            }
            List<string> checkResult = validateBicep(param);


            return ValueTask.FromResult(new CallToolResult
            {
                Content = [
                    new TextContentBlock
                    {
                        Text = string.Join("\n", checkResult)
                    }
                ]
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeployPrecheckTool");

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

    private static (string InputError, DeploymentPrecheckParameters Parameters) ParseRequestParameters(IReadOnlyDictionary<string, JsonElement>? args)
    {
        DeploymentTool deploymentTool = args?.GetValueOrDefault("deploymentTool").ToString()?.ToLowerInvariant() switch
        {
            "azd" => DeploymentTool.Azd,
            "azcli" => DeploymentTool.AzCLI,
            _ => throw new ArgumentException("Invalid deployment tool")
        };
        IacType iacType = args.GetValueOrDefault("iacType").ToString()?.ToLowerInvariant() switch
        {
            "bicep" => IacType.Bicep,
            "terraform" => IacType.Terraform,
            _ => throw new ArgumentException("Invalid IaC type")
        };
        bool azureYamlExists = args.GetValueOrDefault("azureYamlExists").GetBoolean();
        JsonElement bicepChecks = args.GetValueOrDefault("bicepChecks");
        if (bicepChecks.ValueKind == JsonValueKind.Null || bicepChecks.ValueKind == JsonValueKind.Undefined)
        {
            return ("Error: bicepChecks cannot be null in input", null!);
        }
        BicepChecks? checks = JsonConvert.DeserializeObject<BicepChecks>(bicepChecks.ToJsonString());
        DeploymentPrecheckParameters param = new DeploymentPrecheckParameters
        {
            DeploymentTool = deploymentTool,
            IacType = iacType,
            AzureYamlExists = azureYamlExists,
            BicepChecks = checks
        };
        return (string.Empty, param);

    }
    private static List<string> validateBicep(DeploymentPrecheckParameters parameters)
    {
        List<string> missedFields = BicepValidator.ValidateRequiredFields(parameters);
        if (missedFields.Count > 0)
        {
            missedFields.Insert(0, "FATAL: Pre-deployment validation failed. The inputs to this tool were incomplete. Errors: ");
            return missedFields;
        }
        List<string> result = BicepValidator.ProcessAndValidatePreDeployParameters(parameters);
        if (result.Count > 0)
        {
            result.Insert(0, "FATAL: Pre-deployment validation failed. Please fix the following BLOCKING issues by updating files to conform to requirements. Get the latest Bicep schema when you make changes. Call get_errors on all files you create. Call this tool again when done.");
            return result;
        }
        else
        {
            return new List<string> { "SUCCESS: Pre-deployment validation for infra files passed. You can proceed with deployment. AZD requires az (az --version), azd (azd version), docker (docker version, only if there is a container app) to be installed. Run the version commands for the user to ensure this. Do not run any other commands." };
        }
    }

    private static JsonElement GetInputSchema()
    {
        var schema = DeployPrecheckModelsSchema.Schema;
        return System.Text.Json.JsonSerializer.SerializeToElement(schema, DeployToolSerializationContext.Default.JsonObject);
    }

}
