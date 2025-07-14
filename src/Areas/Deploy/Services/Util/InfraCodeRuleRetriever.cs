using AzureMcp.Areas.Deploy.Models;

public static class InfraCodeRuleRetriever
{
    public static List<string> PopulateAZDPrompts(string deploymentTool, string iacType, string[] resourceTypes)
    {
        var prompts = new List<string>
        {
            "Ensure an User-Assigned Managed Identity (UAMI) is created for the deployment.",
            "If the deployment infrastructure contains a resource group, it must have tag \"azd-env-name\" with the value of the environment name.",
            $"Expected parameters in {iacType} parameters file: environmentName='${{AZURE_ENV_NAME}}', location='${{AZURE_LOCATION}}', resourceGroupName='rg-${{AZURE_ENV_NAME}}' (if scope is subscription).",
            "All services (container app, app service, function app, static web app) must have tag \"azd-service-name\" matching the service name in azure.yaml."
        };

        var outputsFileName = iacType == IacType.Bicep ? "main.bicep" : "outputs.tf";
        prompts.Add($"Outputs file {outputsFileName} must output RESOURCE_GROUP_ID.");
        if (resourceTypes.Contains(AzureServiceNames.AzureContainerApp))
        {
            prompts.Add($"{outputsFileName} must output AZURE_CONTAINER_REGISTRY_ENDPOINT representing the URI of the container registry endpoint.");
        }

        return prompts;
    }

    public static List<string> PopulateAZCLIPrompts(string deploymentTool, string iacType, string[] resourceTypes)
    {
        return new List<string> { "Not supported. Please use AZD for deployments." };
    }

    public static List<string> PopulateBicepPrompts(string deploymentTool, string iacType, string[] resourceTypes)
    {
        return [
            """
            Expected files: main.bicep, main.parameters.json (with the required parameters and parameters from main.bicep).
            main.bicep file must have targetScope set to subscription or resourceGroup. This is resourceGroup by default.
            Expected resourceToken format: 'uniqueString(subscription().id, resourceGroup().id, location, environmentName)' (scope = resourceGroup) or 'uniqueString(subscription().id, location, environmentName)' (scope = subscription).
            The same resourceToken should be used for all resource names in the same AZD environment. Resources should be named like az{resourcePrefix}{resourceToken}, where resourcePrefix is a shorthand prefix for the resource (ex. 'kv' for key vault) but no longer than 3 characters. Do not add symbols.
            """
        ];
    }

    public static List<string> PopulateTerraformPrompts(string deploymentTool, string iacType, string[] resourceTypes)
    {
        return new List<string>
        {
            "Expected files: main.tf, main.tfvars.json (with the minimally required parameters), outputs.tf.",
            "Resource names should use Azure CAF naming convention. This is required for deployments. Add aztfmod/azurecaf in the required provider. DO NOT use random_length. NO suffixes needed."
        };
    }

    public static List<string> PopulateContainerAppPrompts(string deploymentTool, string iacType, string[] resourceTypes)
    {
        var prompts = new List<string>
        {
            "Additional requirements for Container Apps:",
            "- All container apps must have an assigned User-Assigned Managed Identity.",
            "- User-Assigned Managed Identity must have the AcrPull role (\"7f951dda-4ed3-4680-a7ca-43fe172d538d\") on the container registry."
        };

        if (iacType == IacType.Bicep)
        {
            prompts.Add("- Container App must have CORS enabled. This is enabled by configuring ingress.corsPolicy in the Bicep file.");
        }
        else if (iacType == IacType.Terraform)
        {
            prompts.Add("- Create an ***azapi_resource_action*** resource using :type `Microsoft.App/containerApps`, method `PATCH`, and body `properties.configuration.ingress.corsPolicy` property to enable CORS for all origins, headers, and methods. Use 'azure/azapi' provider version *2.0*. DO NOT use jsonencode() for the body.");
        }

        prompts.Add("- All secrets used by the Container App must be first defined. Use Key Vault if possible.");

        const string image = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest";
        var imageProperty = iacType == IacType.Bicep ? "properties.template.containers.image" : "azurerm_container_app.template.container.image";
        prompts.Add($"- Container Apps must use base container image {image}. It is absolutely required. The property is set via {imageProperty}.");

        prompts.Add("- Container App must be connected to the container registry via user-assigned managed identity and NOT 'system'. That identity must have AcrPull permissions into the registry.");

        if (iacType == IacType.Bicep)
        {
            prompts.Add("- Container App Environment must be connected to Log Analytics Workspace. Please connect it using logAnalyticsConfiguration -> customerId=logAnalytics.properties.customerId and sharedKey=logAnalytics.listKeys().primarySharedKey.");
        }
        else
        {
            prompts.Add("- Container App Environment must be connected to Log Analytics Workspace. Please connect it using logs_destination=\"log-analytics\" azurerm_container_app_environment.log_analytics_workspace_id = azurerm_log_analytics_workspace.<workspaceName>.id.");
        }

        return prompts;
    }

    public static List<string> PopulateFunctionAppPrompts(string deploymentTool, string iacType, string[] resourceTypes)
    {
        var prompts = new List<string>
        {
            "Function App Rules:",
            "- Function App must have an attached user-assigned managed identity to connect to storage.",
            "- Function App needs a storage account to store data. Please create a storage account and connect it to the function app."
        };

        var diagnosticSettingsResourceType = iacType == IacType.Bicep ? "Microsoft.Insights/diagnosticSettings" : "azurerm_monitor_diagnostic_setting";
        prompts.Add($"Function App must have diagnostic settings defined for function app logs. The resource type is {diagnosticSettingsResourceType}.");

        var requiredRoles = new[]
        {
            new { RoleId = "b7e6dc6d-f1e8-4753-8033-0f276bb0955b", Name = "Storage Blob Data Owner" },
            new { RoleId = "ba92f5b4-2d11-453d-a403-e96b0029c9fe", Name = "Storage Blob Data Contributor" },
            new { RoleId = "974c5e8b-45b9-4653-ba55-5f855dd0fb88", Name = "Storage Queue Data Contributor" },
            new { RoleId = "0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3", Name = "Storage Table Data Contributor" },
            new { RoleId = "3913510d-42f4-4e42-8a64-420c390055eb", Name = "Monitoring Metrics Publisher" }
        };

        foreach (var role in requiredRoles)
        {
            prompts.Add($"- Function App's user-assigned managed identity must have the {role.Name} role ({role.RoleId}).");
        }

        return prompts;
    }

    public static List<string> PopulateAppServiceIaCPrompts(string iacType)
    {
        return new List<string>
        {
            "App Service Rules:",
            "- App Service must be configured with appropriate settings."
        };
    }

    public static List<string> PopulateLLMResponse(string deploymentTool, string iacType, string[] resourceTypes)
    {
        var llmResponse = new List<string>
        {
            "Based on what is being deployed, you must ensure these rules are followed for deployment. Please adjust the infrastructure based on the listed rules.",
            $"Deployment Tool: {deploymentTool}. Deployment Tool rules:"
        };

        if (deploymentTool == DeploymentTool.Azd)
        {
            llmResponse.AddRange(PopulateAZDPrompts(deploymentTool, iacType, resourceTypes));
        }
        else if (deploymentTool == DeploymentTool.AzCLI)
        {
            llmResponse.AddRange(PopulateAZCLIPrompts(deploymentTool, iacType, resourceTypes));
        }

        llmResponse.Add($"IaC Type: {iacType}. IaC Type rules:");

        if (iacType == IacType.Bicep)
        {
            llmResponse.AddRange(PopulateBicepPrompts(deploymentTool, iacType, resourceTypes));
        }
        else if (iacType == IacType.Terraform)
        {
            llmResponse.AddRange(PopulateTerraformPrompts(deploymentTool, iacType, resourceTypes));
        }

        llmResponse.Add($"Resources: {string.Join(", ", resourceTypes)}");

        if (resourceTypes.Contains(AzureServiceNames.AzureContainerApp))
        {
            llmResponse.AddRange(PopulateContainerAppPrompts(deploymentTool, iacType, resourceTypes));
        }

        if (resourceTypes.Contains(AzureServiceNames.AzureAppService))
        {
            llmResponse.AddRange(PopulateAppServiceIaCPrompts(iacType));
        }

        if (resourceTypes.Contains(AzureServiceNames.AzureFunctionApp))
        {
            llmResponse.AddRange(PopulateFunctionAppPrompts(deploymentTool, iacType, resourceTypes));
        }

        llmResponse.Add("You must call the get_errors tool every time you make code changes, otherwise your deployment will fail.");

        var necessaryTools = new List<string> { "az cli (az --version)" };

        if (deploymentTool == DeploymentTool.Azd)
        {
            necessaryTools.Add("azd (azd --version)");
        }

        if (resourceTypes.Contains(AzureServiceNames.AzureContainerApp))
        {
            necessaryTools.Add("docker (docker --version)");
        }

        llmResponse.Add($"Ensure that the user has the necessary tools installed: {string.Join(",", necessaryTools)}.");

        if (iacType == IacType.Terraform && deploymentTool == DeploymentTool.Azd)
        {
            llmResponse.Add("Note: Do not use Terraform CLI directly.");
        }

        return llmResponse;
    }
}
{
            llmResponse.Add("Note: Do not use Terraform CLI directly.");
        }

        return llmResponse;
    }
}

