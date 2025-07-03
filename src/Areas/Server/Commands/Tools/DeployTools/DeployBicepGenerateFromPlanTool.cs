using System.Text.Json.Nodes;
using Areas.Server.Commands.Tools.DeployTools.Util;
using AzureMcp.Areas.Server.Commands.Tools.Models;
using AzureMcp.Areas.Server.Commands.Tools.Utils;
using Json.More;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using Newtonsoft.Json;


namespace AzureMcp.Areas.Server.Commands.Tools;

[McpServerToolType]
public sealed class DeployBicepGenerateFromPlanTool(ILogger<DeployBicepGenerateFromPlanTool> logger) : McpServerTool
{
    private readonly ILogger<DeployBicepGenerateFromPlanTool> _logger = logger;

    static readonly string ToolName = "deploy_bicep_generate_from_plan";

    static readonly string ToolDescription = "Read the file '.codetocloud/deploy-plan.md' and fulfill the input to call the tools. The tool will provide some rules for Bicep. Agent should follow the rule and plan to generate bicep files.";

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
        IReadOnlyDictionary<string, JsonElement>?  args = request.Params?.Arguments;

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
            
            var parseResult = ParseRequestParameters(args);
            if (!string.IsNullOrEmpty(parseResult.InputError))
            {
                return  ValueTask.FromResult(new CallToolResult
                {
                    Content = [
                        new TextContentBlock
                        {
                            Text = parseResult.InputError
                        }
                    ],
                    IsError = true
                });
            }
            
            var parameters = parseResult.Parameters;

            List<string> result = PopulatePrompts(parameters);

            return ValueTask.FromResult(new CallToolResult
            {
                Content = [
                    new TextContentBlock
                    {
                        Text = string.Join("\n", result)
                    }
                ]
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeployBicepGenerateFromPlanTool");

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


    private static List<string> PopulatePrompts(RecommendConfigsParameters parameters)
    {
        var responses = new List<string>();
        var isContainerAppType = parameters.Services.Any(service => service.AzureComputeHost.Contains(AzureYamlServiceType.ContainerApp));

        PopulateGeneralIaCPrompts(responses, isContainerAppType);

        if (parameters.Services.Any(service => service.AzureComputeHost.Contains(AzureYamlServiceType.ContainerApp)))
        {
            PopulateContainerAppIaCPrompts(responses);
        }

        if (parameters.Services.Any(service => service.AzureComputeHost.Contains(AzureYamlServiceType.AppService)))
        {
            PopulateAppServiceIaCPrompts(responses);
        }

        foreach (var service in parameters.Services)
        {
            PopulateServiceIaCPrompts(service, responses);
        }

        PopulateAzureYamlGenerationPrompts(parameters, responses);
        return responses;
    }

    private static JsonElement GetInputSchema()
    {
        return System.Text.Json.JsonSerializer.SerializeToElement(BicepGenerateParametersSchema.Schema, DeployToolSerializationContext.Default.JsonObject);
    }

    private static void PopulateGeneralIaCPrompts(List<string> parameters, bool isContainerAppType)
    {
        parameters.Add($"""
Generate infrastructure files to deploy an app using AZD and Bicep by following the rules below.
File Structure: Place Bicep files under 'infra/'. Generate a 'main.parameters.json'. Generate an 'azure.yaml' at project root.
main.bicep File Requirements:
    * Must contain:
    * Correct "targetScope": "subscription" or "resourceGroup".
    * Correct "resourceToken" format:
    * If "resourceGroup" scope: "uniqueString(subscription().id, resourceGroup().id, location, environmentName)"
    * If "subscription" scope: "uniqueString(subscription().id, location, environmentName)"
    * Resource names format: "az-<resourcePrefix>-<resourceToken>".
    * "resourcePrefix": ≤ 3 characters.
    * Required **outputs**:
    * Always: "RESOURCE_GROUP_ID"
    * {(isContainerAppType ? "AZURE_CONTAINER_REGISTRY_ENDPOINT" : "")}
""");
        parameters.Add("""
Parameters File (main.parameters.json):
    * Define the following parameters: environmentName='${AZURE_ENV_NAME}', location='${AZURE_LOCATION}', resourceGroupName='rg-${AZURE_ENV_NAME}'.
    * If targetScope = "subscription": Resource group in "main.bicep" must have "azd-env-name" tag matching "environmentName".
""");

    }

    private static void PopulateContainerAppIaCPrompts(List<string> responses)
    {
        responses.Add("""
Container Apps Configurations: 
- MUST set properties.template.containers.image = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest". 
- Set properties.template.containers.resources.cpu = json('0.5').
- Enable CORS in properties.configuration.ingress.corsPolicy
- Must attach user-assigned managed identity
- Identity must have **AcrPull** role ("7f951dda-4ed3-4680-a7ca-43fe172d538d") to the container registry.
- Connect container app environment to the log analytics workspace via:
```bicep
  logAnalyticsConfiguration: {
  customerId: logAnalytics.properties.customerId
  sharedKey: logAnalytics.listKeys().primarySharedKey
}
```
""");
    }

    private static void PopulateAppServiceIaCPrompts(List<string> responses)
    {
        responses.Add("""
App Service Configurations:
- Enable CORS in SiteConfig.cors.
""");
    }

    private static void PopulateServiceIaCPrompts(ServiceConfig service, List<string> responses)
    {
        // TODO List (note that maybe we can use iac_generator_ts to populate fine grained rules)
        // Firewall for resources like CosmosDB
        // VNet as a best practice
        // Managed identity connection

        responses.Add($"""
{service.Name} Service Name
- host: {service.AzureComputeHost}
- Tags: azd-service-name ='{service.Name}'
- Dependencies:
""");

        foreach (var dependency in service.Dependencies)
        {
            responses.Add($"  - {dependency.Name}: Service Type: {dependency.ServiceType}, Connection Type: {dependency.ConnectionType}, Environment Variables: [{string.Join(", ", dependency.EnvironmentVariables)}]");
        }

        var notCoveredEnvVars = service.Settings.Where(x => !service.Dependencies.Any(dep => dep.EnvironmentVariables.Contains(x))).ToArray();
        if (notCoveredEnvVars.Length > 0)
        {
            responses.Add($"- Environment Variables: [{string.Join(", ", notCoveredEnvVars)}] (not covered by dependencies, please add these into main.parameters.json for the user to fill in)");
        }
    }

    private static void PopulateAzureYamlGenerationPrompts(RecommendConfigsParameters parameters, List<string> responses)
    {
        if (parameters.Services.Any(service => service.AzureComputeHost == "containerapp" && service.DockerSettings == null))
        {
            throw new InvalidOperationException("FATAL: All services to be deployed via Container App must have dockerSettings defined.");
            // However, if dockerFilePath is defined but it is not a container app, we can still proceed.
        }

        responses.Add($"""
azure.yaml details: Create azure.yaml file at root folder {parameters.WorkspaceFolder} with the following properties. DO NOT add any additional properties.
""");

        const string iacType = "bicep"; // TODO: Support Terraform

        responses.Add($"ROOT.name: {parameters.ProjectName}");
        responses.Add($"ROOT.infra.provider: {iacType}"); // TODO: Support Terraform

        var projectRoot = parameters.WorkspaceFolder;

        foreach (var service in parameters.Services)
        {
            var serviceRelative = Path.GetRelativePath(projectRoot, service.Path);

            responses.Add($"ROOT.services.{service.Name}.project: {(string.IsNullOrEmpty(serviceRelative) ? "./" : serviceRelative)}");
            responses.Add($"ROOT.services.{service.Name}.host: {service.AzureComputeHost}");
            responses.Add($"ROOT.services.{service.Name}.language: {service.Language}");

            if (service.AzureComputeHost == "containerapp")
            {
                var dockerFilePathRelative = Path.GetRelativePath(service.Path, service.DockerSettings!.DockerFilePath);
                var dockerContextPathRelative = Path.GetRelativePath(service.Path, service.DockerSettings!.DockerContext);

                responses.Add($"ROOT.services.{service.Name}.docker.path: {(string.IsNullOrEmpty(dockerFilePathRelative) ? "./" : dockerFilePathRelative)}");
                responses.Add($"ROOT.services.{service.Name}.docker.context: {(string.IsNullOrEmpty(dockerContextPathRelative) ? "./" : dockerContextPathRelative)}");
            }
        }

        responses.Add("\n");
    }

    private static (string InputError, RecommendConfigsParameters Parameters) ParseRequestParameters(IReadOnlyDictionary<string, JsonElement>? args)
    {

        var workspaceFolder = args?.GetValueOrDefault("workspaceFolder").GetStringSafe();
        var projectName = args?.GetValueOrDefault("projectName").GetStringSafe();
        
        if (string.IsNullOrEmpty(workspaceFolder))
        {
            return ("Error: workspaceFolder is required in input", null!);
        }
        if (string.IsNullOrEmpty(projectName))
        {
            return ("Error: projectName is required in input", null!);
        }
   
        var servicesJson = args?.GetValueOrDefault("services").EnumerateArray();
        
        var servicesList = new List<ServiceConfig>();
        foreach (JsonElement service in servicesJson ?? [])
        {
            string jsonstring = service.ToJsonString(); 
            servicesList.Add(JsonConvert.DeserializeObject<ServiceConfig>(jsonstring) ?? new ServiceConfig());
        }
        if (servicesList.Count == 0)
        {
            return ("Error: services is required in input", null!);
        }
        
        var parameters = new RecommendConfigsParameters
        {
            WorkspaceFolder = workspaceFolder,
            ProjectName = projectName,
            Services = servicesList.ToArray(),
            CreateIaC = true
        };
        
        return ("", parameters);
    }

}
