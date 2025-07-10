using System.Text.Json;
using Areas.Server.Commands.Tools.DeployTools.Util;
using AzureMcp.Areas.Server.Commands.Tools.Models;
using AzureMcp.Areas.Server.Commands.Tools.Utils;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;

namespace AzureMcp.Areas.Server.Commands.Tools;

[McpServerToolType]
public sealed class DeployPipelineGenerateTool(ILogger<DeployPipelineGenerateTool> logger) : McpServerTool
{
    private readonly ILogger<DeployPipelineGenerateTool> _logger = logger;

    static readonly string ToolName = "deploy_pipeline_generate";

    static readonly string ToolDescription = "Guidance to create a CI/CD pipeline which provision Azure resources and build and deploy applications to Azure. Use this tool BEFORE generating/creating a Github actions workflow file for DEPLOYMENT on Azure. Infrastructure files should be ready and the application should be ready to be containerized.";

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
            string result = PopulatePipelinePrompts(parameters);

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
            _logger.LogError(ex, "Error in DeployPipelineGenerateTool");

            return ValueTask.FromResult(new CallToolResult
            {
                Content = [
                    new TextContentBlock
                    {
                        Text = $"Error1: {ex.Message}"
                    }
                ],
                IsError = true
            });
        }
    }

    private static JsonElement GetInputSchema()
    {
        return JsonSerializer.SerializeToElement(PipelineGenerateParametersSchema.Schema, DeployToolSerializationContext.Default.JsonObject);
    }

    private static string PopulatePipelinePrompts(PipelineGenerateParameters parameters)
    {
        if (parameters.UseAZDPipelineConfig)
        {
            return AZDPipelinePrompt;
        }
        else
        {
            return AZCLIPipelinePrompt(parameters);
        }
    }

    private static readonly string AZDPipelinePrompt = "Run \"azd pipeline config\" to help the user create a deployment pipeline.\n";

    private static string AZCLIPipelinePrompt(PipelineGenerateParameters parameters)
    {
        const string defaultEnvironment = "dev";
        var environmentNamePrompt = !string.IsNullOrEmpty(parameters.GithubEnvironmentName)
            ? $"Use {parameters.GithubEnvironmentName} for environment name of the deployment job."
            : $"Use '{defaultEnvironment}' for the $environment for the deployment job.";

        var subscriptionIdPrompt = !string.IsNullOrEmpty(parameters.SubscriptionId) && CheckGUIDFormat(parameters.SubscriptionId)
            ? $"User is deploying to subscription {parameters.SubscriptionId}"
            : "Use \"az account show --query id -o tsv\" as default subscription ID.";

        var organizationName = !string.IsNullOrEmpty(parameters.OrganizationName) ? parameters.OrganizationName : "{$organization-of-repo}";
        var repositoryName = !string.IsNullOrEmpty(parameters.RepositoryName) ? parameters.RepositoryName : "{$repository-name}";
        var environmentName = !string.IsNullOrEmpty(parameters.GithubEnvironmentName) ? parameters.GithubEnvironmentName : defaultEnvironment;

        var subjectConfig = $"repo:{organizationName}/{repositoryName}:environment:{environmentName}";
        var environmentArg = !string.IsNullOrEmpty(parameters.GithubEnvironmentName) ? $"--env {parameters.GithubEnvironmentName}" : "--env dev";
        var environmentCreateCommand = $"gh api --method PUT -H \"Accept: application/vnd.github+json\" repos/{organizationName}/{repositoryName}/environments/{environmentName}";
        var jsonParameters = $"{{\"name\":\"github-federated\",\"issuer\":\"https://token.actions.githubusercontent.com\",\"subject\":\"{subjectConfig}\",\"audiences\":[\"api://AzureADTokenExchange\"]}}";
        return $"""
Help the user to set up a CI/CD pipeline to deploy to Azure with the following steps IN ORDER. **RUN the commands directly and DO NOT just give instructions. DO NOT ask user to provide information.**

  1. First generate a Github Actions workflow file to deploy to Azure. {environmentNamePrompt} The pipeline at least contains these steps in order:
    a. Azure login: login with a service principal using OIDC. DO NOT use secret.
    b. Docker build
    c. Deploy infrastructure: Use AZ CLI "az deployment sub/group create" command. Use "az deployment sub/group wait" to wait the deployment to finish. Refer to the infra files to set the correct parameters.
    d. Azure Container Registry login: login into the container registry created in the previous step. Use "az acr list" to get the correct registry name if you are not sure.
    e. Push app images to ACR
    f. Deploy to hosting service. Use the infra deployment output or AZ CLI to list hosting resources. Find the name or ID of the hosting resources from "az <resource> list" if you are not sure.

    Pay attention to the name of the branches to which the pipeline is triggered.

  2. Run '{environmentCreateCommand}' to create the environment in the repository.

  3. - {subscriptionIdPrompt}
     - Run "az ad sp create-for-rbac" command to create a service principal. Grant the service principal *Contributor* role of the subscription. Also grant the service principal *User Access Administrator*
    **Use Federated credentials in order to authenticate to Azure services from GitHub Actions workflows. The command is **az ad app federated-credential create --id <$service-principal-app-id> --parameters '{jsonParameters}'**. You MUST use ' and \"(DO NOT forget the slash \) in the command. Use the current Github org/repo to fill in the subject property.

  4. Run command "gh secret set <secret_name> --body <secret_value> {environmentArg}" to configure the AZURE_CLIENT_ID, AZURE_TENANT_ID and AZURE_SUBSCRIPTION_ID of the service principal in Github secrets using Github CLI.

  ** DO NOT prompt user for any information. Find them on your own. **
""";
    }

    private static bool CheckGUIDFormat(string input)
    {
        return Guid.TryParse(input, out _);
    }

    private static  PipelineGenerateParameters  ParseRequestParameters(IReadOnlyDictionary<string, JsonElement> args)
    {
        var parameters = new PipelineGenerateParameters
        {
            UseAZDPipelineConfig = args.GetValueOrDefault("useAZDPipelineConfig").GetBoolean(),
            OrganizationName = args.GetValueOrDefault("organizationName").GetStringSafe(),
            RepositoryName = args.GetValueOrDefault("repositoryName").GetStringSafe(),
            GithubEnvironmentName = args.GetValueOrDefault("githubEnvironmentName").GetStringSafe(),
            SubscriptionId = args.GetValueOrDefault("subscriptionId").GetStringSafe()
        };

        return parameters;
    }
}
