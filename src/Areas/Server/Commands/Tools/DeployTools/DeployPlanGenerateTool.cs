using System.Text.Json.Nodes;
using AzureMcp.Areas.Server.Commands.Tools.Utils;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;


namespace AzureMcp.Areas.Server.Commands.Tools;

[McpServerToolType]
public sealed class DeployPlanGenerateTool(ILogger<DeployPlanGenerateTool> logger) : McpServerTool
{
    private readonly ILogger<DeployPlanGenerateTool> _logger = logger;

    static readonly string ToolName = "deploy_plan_generate";

    static readonly string ToolDescription = "Entry point to help the agent deploy a service to the cloud. Agent should read its output and generate a deploy plan in '.codetocloud/deploy-plan.md' for execution steps, recommended azure services based on the information agent detected from project. Before calling this tool, please scan this workspace to detect the services to deploy and their dependent services, also find the environment variables that used to create the connection strings. Try your best to fulfill the input schema with your analyze result.";

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
                        Text = getPlanTemplate(),
                    }
                ]
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeployPlanGenerateTool");

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

    private static string getPlanTemplate()
    {
        String planTemplate = """
Title: "Azure Deployment Plan for {projectName} Project"
## **Goal** 
Based on the project to provide a plan to deploy the project to Azure using AZD. It will generate Bicep files and Azure YAML configuration.
## **Execution Step**
1. Generate IaC files (Bicep files) based on the plan
2. Precheck: use get_errors tool to check generated Bicep grammar errors and predeploy_check check the Bicep logic. Fix the errors if exist.
3. Start Deployment with AZD CLI command: `azd up`
4. After deployment finished, summarize the deployment result.

## **Recommended Resource Provisioning**

App template //agent should fulfill this for each app instance
- AppName {service.name} 
  - Service Type: {azureComputeHost} // it can be containerapp, webapp, functionapp, etc. Recommend one based on the project.
  - language: {language}  //detect from the project, it can be nodejs, python, dotnet, etc.
  - dockerFilePath: {dockerFilePath}// fulfill this if service.azureComputeHost is ContainerApp
  - dockerContext: {dockerContext}// fulfill this if service.azureComputeHost is ContainerApp
  - Environment Variables: [] // the env variables that are used in the project/required by service
  - Dependencies Resource 
    - Dependecy Name
    - Service Type
    - Connection Type // it can be connection string, managed identity, etc.
    - Environment Variables: [] // the env variables that are used in the project/required by dependency
Recommended Supporting Services:
- Application Insights
- Log Analytics Workspace: set all app service to connect to this
- Key Vault(Optional): If there are dependencies such as postgresql/sql/mysql, create a Key Vault to store connection string. If not, the resource should not show.
If there is a Container App, the following resources are required:
- Container Registry
- User managed identity: Must be assigned to the container app.
- AcrPull role assignment: User managed identity must have **AcrPull** role ("7f951dda-4ed3-4680-a7ca-43fe172d538d") assigned to the container registry.
If there is a WebApp(App Service):
- App Service Site Extension (Microsoft.Web/sites/siteextensions): Required for App Service deployments.
- User managed identity: Must have **AcrPull** role ("7f951dda-4ed3-4680-a7ca-43fe172d538d") to the container registry.

## **Azure Resources Architecture**:
{a mermaid graph of the deployed resource architecture}
""";
        return planTemplate;
        
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
