using AzureMcp.Areas.Server.Commands.Tools.DeployTools.Models;

namespace Areas.Server.Commands.Tools.DeployTools.Util;


public static class BicepValidator
{
    public static List<string> ValidateRequiredFields(DeploymentPrecheckParameters parameters)
    {
        List<string> missedFields = new List<string>();
        if (parameters.DeploymentTool == DeploymentTool.Azd)
        {
            if (!parameters.AzureYamlExists)
            {
                missedFields.Add("FATAL: azure.yaml is required for AZD deployments.");
            }
        }

        if (parameters.IacType == IacType.Bicep)
        {
            if (parameters.BicepChecks == null)
            {
                missedFields.Add("FATAL: bicepChecks is undefined. This is required when using AZD. Fill this based on the Bicep files from the infra folder.");
            }
            else
            {
                if (parameters.BicepChecks.HasAppService && parameters.BicepChecks.AppServiceChecks == null)
                {
                    missedFields.Add("FATAL: appServiceChecks is undefined. This is required when using AZD with an app service. Fill this based on the app service Bicep files from the infra folder.");
                }

                if (parameters.BicepChecks.HasContainerApp && parameters.BicepChecks.ContainerAppChecks == null)
                {
                    missedFields.Add("FATAL: containerAppChecks is undefined. This is required when using AZD with a container app. Fill this based on the container app Bicep files from the infra folder.");
                }

                if (parameters.BicepChecks.HasContainerApp && parameters.BicepChecks.ContainerRegistryChecks == null)
                {
                    missedFields.Add("FATAL: containerRegistryChecks is undefined. This is required when using AZD with a container app. Fill this based on the container registry Bicep files from the infra folder.");
                }

                if (parameters.BicepChecks.HasContainerApp && parameters.BicepChecks.ContainerAppsEnvironmentChecks == null)
                {
                    missedFields.Add("FATAL: containerAppsEnvironmentChecks is undefined. This is required when using AZD with a container app. Fill this based on the container app environment Bicep files from the infra folder.");
                }

                if (parameters.BicepChecks.HasFunctionApp && parameters.BicepChecks.FunctionAppChecks == null)
                {
                    missedFields.Add("FATAL: functionAppChecks is undefined. This is required when using AZD with a function app. Fill this based on the function app Bicep files from the infra folder.");
                }
            }
        }
        return missedFields;
    }

    public static List<string> ProcessAndValidatePreDeployParameters(DeploymentPrecheckParameters parameters)
    {
        System.Console.WriteLine($"Processing PreDeploy checks for deploymentTool {parameters.DeploymentTool} with IacType {parameters.IacType}");

        if (parameters.IacType == IacType.Bicep)
        {
            return ProcessBicepChecks(parameters.DeploymentTool, parameters.BicepChecks!);
        }
        return new List<string>
        {
        };
    }

    private static List<string> ProcessBicepChecks(DeploymentTool deploymentTool, BicepChecks bicepChecks)
    {
        List<string> validateResult = new List<string>();
        var mainBicepChecks = bicepChecks.MainBicepChecks;

        if (!mainBicepChecks.MainDotBicepFileExists)
        {
            validateResult.Add("main.bicep file not found. This file is required.");
        }

        if (!mainBicepChecks.MainParametersExists)
        {
            validateResult.Add("main.parameters.json file not found. This file is required.");
        }

        if (mainBicepChecks.TargetScope != TargetScopeType.Subscription && mainBicepChecks.TargetScope != TargetScopeType.ResourceGroup)
        {
            validateResult.Add("main.bicep file must have targetScope set to subscription or resourceGroup. This is resourceGroup by default.");
            return validateResult;
        }

        var resourceTokenFormat = mainBicepChecks.TargetScope == TargetScopeType.ResourceGroup
            ? "uniqueString(subscription().id, resourceGroup().id, location, environmentName)"
            : "uniqueString(subscription().id, location, environmentName)";

        if (!mainBicepChecks.ResourceNamesUseResourceToken)
        {
            validateResult.Add($"Resource names should use the resource token which is generated using *{resourceTokenFormat}*. The same resource token should be used for all resources in the same AZD environment. Resources should be named like az-{{resourcePrefix}}-{{resourceToken}}, where resourcePrefix is a shorthand prefix for the resource (ex. 'kv' for key vault) but no longer than 3 characters.");
        }

        if (string.IsNullOrEmpty(mainBicepChecks.ResourceTokenFormat) || !mainBicepChecks.ResourceTokenFormat.Contains(resourceTokenFormat))
        {
            validateResult.Add($"Resource token format is not correct. The format must contain {resourceTokenFormat}.");
        }
        else if (mainBicepChecks.ResourceTokenFormat.Contains("${environmentName}"))
        {
            validateResult.Add("Resource token format should not include ${environmentName}. It should be ONLY uniqueString with no prefixes nor suffixes.");
        }

        var requiredMainOutputs = new Dictionary<string, string>
        {
            ["RESOURCE_GROUP_ID"] = "Resource group ID."
        };

        if (bicepChecks.HasContainerApp)
        {
            requiredMainOutputs["AZURE_CONTAINER_REGISTRY_ENDPOINT"] = "The container registry endpoint.";
        }

        foreach (var output in requiredMainOutputs.Keys)
        {
            if (!mainBicepChecks.MainDotBicepFileOutputs.Contains(output))
            {
                validateResult.Add($"{output} not found as an output in main.bicep file (not any other files). It is required. Description - {requiredMainOutputs[output]}. Declared like \"output {output} <type> = <value>\".");
            }
        }

        if (deploymentTool == DeploymentTool.Azd)
        {
            var requiredMainParameters = new Dictionary<string, string>
            {
                ["environmentName"] = "Required for AZD Up. Use AZD env ${AZURE_ENV_NAME}",
                ["location"] = "Required for AZD Up. Use AZD env ${AZURE_LOCATION}"
            };

            if (mainBicepChecks.TargetScope == TargetScopeType.Subscription)
            {
                requiredMainParameters["resourceGroupName"] = "Required for AZD Up. Use rg-${AZURE_ENV_NAME}";
            }

            foreach (var param in requiredMainParameters.Keys)
            {
                if (!mainBicepChecks.MainParametersParameters.Contains(param))
                {
                    validateResult.Add($"{param} not found in main.parameters.json. {requiredMainParameters[param]}");
                }
            }

            if (mainBicepChecks.TargetScope == TargetScopeType.Subscription && !bicepChecks.MainBicepChecks.AzdEnvNameTagExists)
            {
                validateResult.Add("azd-env-name tag not found in main.bicep. This must be placed on the resource group resource matching environmentName parameter. This is required for AZD UP.");
            }
        }

        if (bicepChecks.HasFunctionApp)
        {
            ProcessFuncAppChecks(bicepChecks.FunctionAppChecks!, validateResult);
        }

        if (bicepChecks.HasContainerApp)
        {
            ProcessContainerAppChecks(bicepChecks.ContainerAppChecks!, validateResult);
            ProcessContainerRegistryChecks(bicepChecks.ContainerRegistryChecks!, validateResult);
            ProcessContainerAppEnvironmentChecks(bicepChecks.ContainerAppsEnvironmentChecks!, validateResult);
        }

        if (bicepChecks.HasAppService)
        {
            ProcessAppServiceChecks(bicepChecks.AppServiceChecks!, validateResult);
        }
        return validateResult;
    }

    private static void ProcessFuncAppChecks(FunctionAppChecks funcAppChecks, List<string> validateResult)
    {
        if (!funcAppChecks.UserAssignedManagedIdentityAttached)
        {
            validateResult.Add("Function App must have an attached user-assigned managed identity to connect to storage.");
        }

        if (!funcAppChecks.StorageAccountExists)
        {
            validateResult.Add("Function App needs a storage account to store data. Please create a storage account and connect it to the function app.");
        }

        if (!funcAppChecks.DiagnosticSettingsExists)
        {
            validateResult.Add("Function App must have diagnostic settings defined for function app logs. The resource type is Microsoft.Insights/diagnosticSettings.");
        }

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
            var hasRole = funcAppChecks.UserAssignedManagedIdentityRoleAssignments.Any(assignment =>
                assignment.RoleDefinitionId.EndsWith(role.RoleId) &&
                assignment.PrincipalType == "ServicePrincipal");

            if (!hasRole)
            {
                validateResult.Add($"Missing role assignment: {role.Name}. Please add role assignment with GUID '{role.RoleId}' to the user-assigned managed identity.");
            }
        }
    }

    private static void ProcessContainerAppChecks(ContainerAppChecks containerAppChecks, List<string> validateResult)
    {
        if (!containerAppChecks.UserAssignedManagedIdentityAttached)
        {
            validateResult.Add("Container App must attach an user-assigned managed identity.");
        }

        if (!containerAppChecks.CorsEnabled)
        {
            validateResult.Add("Container App must have CORS enabled. This is enabled by configuring ingress.corsPolicy.");
        }

        if (!containerAppChecks.ContainerAppDefinesAllUsedSecrets)
        {
            validateResult.Add("All secrets used by the Container App must be first defined. Use Key Vault if possible.");
        }

        const string baseImage = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest";
        if (containerAppChecks.TemplateImageUris.Count() > 0)
        {
            foreach (var image in containerAppChecks.TemplateImageUris)
            {
                if (image != baseImage)
                {
                    validateResult.Add($"Container App is using a non-base image {image}. We must use the base image {baseImage}. It is absolutely required. The property is set via properties.template.containers.image.");
                }
            }
        }
        else
        {
            validateResult.Add($"Please hardcode the container app Bicep to use the base image {baseImage} as absolutely required. The property is set via properties.template.containers.image.");
        }

        if (containerAppChecks.RegistryConnections.Count() == 0)
        {
            validateResult.Add("Container App must connect to the container registry. This is required for Container Apps to work with container images.");
        }
        else if (containerAppChecks.RegistryConnections.Count() > 1)
        {
            validateResult.Add("Container App must connect to only one container registry. Multiple registries are not supported.");
        }
        else
        {
            foreach (var registryConnection in containerAppChecks.RegistryConnections)
            {
                if (string.IsNullOrEmpty(registryConnection.Identity) ||
                    registryConnection.Identity.Equals("system", System.StringComparison.OrdinalIgnoreCase) ||
                    registryConnection.Identity.Equals("systemassigned", System.StringComparison.OrdinalIgnoreCase))
                {
                    validateResult.Add("Container App must connect to the container registry via user-assigned managed identity and not system-assigned managed identity nor a secret. That identity must have AcrPull permissions into the registry.");
                }
            }
        }
    }

    private static void ProcessContainerRegistryChecks(ContainerRegistryChecks containerRegistryChecks, List<string> validateResult)
    {
        if (!containerRegistryChecks.UserAssignedManagedIdentityHasAcrPullPermissions)
        {
            validateResult.Add("The user-assigned managed identity must be given the AcrPull (7f951dda-4ed3-4680-a7ca-43fe172d538d) role assignment into the container registry.");
        }
    }

    private static void ProcessContainerAppEnvironmentChecks(ContainerAppsEnvironmentChecks containerAppsEnvironmentChecks, List<string> validateResult)
    {
        if (!containerAppsEnvironmentChecks.ConnectedToLogAnalyticsWorkspace)
        {
            validateResult.Add("Container App Environment is not connected to Log Analytics Workspace. Please connect it using logAnalyticsConfiguration -> customerId=logAnalytics.properties.customerId and sharedKey=logAnalytics.listKeys().primarySharedKey.");
        }
    }

    private static void ProcessAppServiceChecks(AppServiceChecks appServiceChecks, List<string> validateResult)
    {
        if (!appServiceChecks.UserAssignedManagedIdentityAttached)
        {
            validateResult.Add("App Service must use user-assigned managed identity.");
        }

        if (!appServiceChecks.CorsEnabled)
        {
            validateResult.Add("App Service must have CORS enabled. This is enabled by configuring SiteConfig.cors.");
        }
    }
}
