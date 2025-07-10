// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

using AzureMcp.Areas.Deploy.Models;
using AzureMcp.Areas.Deploy.Options;
using AzureMcp.Commands;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.Deploy.Commands.Plan;

public sealed class PlanGetCommand(ILogger<PlanGetCommand> logger)
    : BaseCommand()
{
    private const string CommandTitle = "Generate Azure Deployment Plan";
    private readonly ILogger<PlanGetCommand> _logger = logger;

    private readonly Option<string> _rawMcpToolInputOption = new(
        $"--{DeployOptionDefinitions.RawMcpToolInput.RawMcpToolInputName}",
        PlanGetParametersSchema.Schema.ToJsonString()
    )
    {
        IsRequired = true
    };

    public override string Name => "get";

    public override string Description =>
        """
        Entry point to help the agent deploy a service to the cloud. Agent should read its output and generate a deploy plan in '.codetocloud/deploy-plan.md' for execution steps, recommended azure services based on the information agent detected from project. Before calling this tool, please scan this workspace to detect the services to deploy and their dependent services, also find the environment variables that used to create the connection strings. Try your best to fulfill the input schema with your analyze result.
        """;

    public override string Title => CommandTitle;

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.AddOption(_rawMcpToolInputOption);
    }

    private RawMcpToolInputOptions BindOptions(ParseResult parseResult)
    {
        var options = new RawMcpToolInputOptions();
        options.RawMcpToolInput = parseResult.GetValueForOption(_rawMcpToolInputOption);
        return options;
    }

    [McpServerTool(
        Destructive = false,
        ReadOnly = true,
        Title = CommandTitle)]
    public override Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
    {
        var options = BindOptions(parseResult);
        var rawMcpToolInput = options.RawMcpToolInput;
        if (string.IsNullOrWhiteSpace(rawMcpToolInput))
        {
            throw new ArgumentException("Input cannot be null or empty.", nameof(options.RawMcpToolInput));
        }

        PlanGetParameters? parameters;
        try
        {
            parameters = JsonSerializer.Deserialize<PlanGetParameters>(
                          rawMcpToolInput, DeployJsonContext.Default.PlanGetParameters)
                          ?? throw new ArgumentException("Failed to deserialize input.", nameof(rawMcpToolInput));
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid JSON format: {ex.Message}", nameof(rawMcpToolInput), ex);
        }

        _logger.LogInformation("Successfully parsed PlanGetParameters");
        if (parameters == null)
        {
            throw new ArgumentException("Parsed parameters cannot be null.", nameof(rawMcpToolInput));
        }

        if (string.IsNullOrWhiteSpace(parameters.WorkspaceFolder))
        {
            throw new ArgumentException("Workspace folder cannot be null or empty.", nameof(parameters.WorkspaceFolder));
        }

        try
        {
            var planTemplate = GetPlanTemplate(parameters.ProjectName);

            context.Response.Message = planTemplate;
            context.Response.Status = 200;
            return Task.FromResult(context.Response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating deployment plan");
            context.Response.Status = 500;
            context.Response.Message = $"Error generating deployment plan: {ex.Message}";
            return Task.FromResult(context.Response);
        }
    }

    private static string GetPlanTemplate(string? projectName)
    {
        var title = string.IsNullOrWhiteSpace(projectName)
            ? "Azure Deployment Plan"
            : $"Azure Deployment Plan for {projectName} Project";

        return $$"""
Title: "{{title}}"
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
    }

    // Implementation-specific error handling
    protected override string GetErrorMessage(Exception ex) => ex switch
    {
        ArgumentException argEx => $"Invalid argument: {argEx.Message}",
        JsonException jsonEx => $"JSON parsing error: {jsonEx.Message}",
        _ => base.GetErrorMessage(ex)
    };

    protected override int GetStatusCode(Exception ex) => ex switch
    {
        ArgumentException => 400,
        JsonException => 400,
        _ => base.GetStatusCode(ex)
    };
}
