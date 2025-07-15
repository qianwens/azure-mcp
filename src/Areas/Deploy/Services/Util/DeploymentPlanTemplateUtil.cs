// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AzureMcp.Areas.Deploy.Services.Util;

/// <summary>
/// Utility class for generating deployment plan templates.
/// </summary>
public static class DeploymentPlanTemplateUtil
{
    /// <summary>
    /// Generates a deployment plan template with the specified project name.
    /// </summary>
    /// <param name="projectName">The name of the project. Can be null or empty.</param>
    /// <returns>A formatted deployment plan template string.</returns>
    public static string GetPlanTemplate(string? projectName)
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

## **Project Summary**
{
briefly summarize the project structure, services, and configurations, example:
- **Technology Stack**: ASP.NET Core 7.0 Razor Pages application
- **Application Type**: Task Manager web application with client-side JavaScript
- **Containerization**: Ready for deployment with existing Dockerfile
- **Dependencies**: No external dependencies detected (database, APIs, etc.)
- **Hosting Recommendation**: Azure Container Apps for scalable, serverless container hosting
}

## **Recommended Azure Resources**

Recommended App service hosting the project //agent should fulfill this for each app instance
- Application {{projectName}}
  - Hosting Service Type: {azureComputeHost} // it can be Azure Container Apps, Web App Service, Azure Functions, Azure Kubernetes Service. Recommend one based on the project.
  - SKU // recommend a sku based on the project, show its cost and performance
  - Configuration:
    - language: {language}  //detect from the project, it can be nodejs, python, dotnet, etc.
    - dockerFilePath: {dockerFilePath}// fulfill this if service.azureComputeHost is ContainerApp
    - dockerContext: {dockerContext}// fulfill this if service.azureComputeHost is ContainerApp
    - Environment Variables: [] // the env variables that are used in the project/required by service
  - Dependencies Resource
    - Dependency Name
    - Service Type
    - Connection Type // it can be connection string, managed identity, etc.
    - Environment Variables: [] // the env variables that are used in the project/required by dependency

Recommended Supporting Services
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

## **Azure Resources Architecture**
{a mermaid graph of the deployed resource architecture. Only keep the most important edges to make structure clear and readable.}
""";
    }
}
