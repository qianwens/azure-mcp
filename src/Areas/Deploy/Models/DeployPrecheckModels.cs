using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace AzureMcp.Areas.Server.Commands.Tools.DeployTools.Models;


[JsonConverter(typeof(JsonStringEnumConverter<DeploymentTool>))]
public enum DeploymentTool
{
    [JsonPropertyName("azd")]
    Azd,

    [JsonPropertyName("azcli")]
    AzCLI
}

[JsonConverter(typeof(JsonStringEnumConverter<IacType>))]
public enum IacType
{
    [JsonPropertyName("bicep")]
    Bicep,

    [JsonPropertyName("terraform")]
    Terraform
}

[JsonConverter(typeof(JsonStringEnumConverter<TargetScopeType>))]
public enum TargetScopeType
{
    [JsonPropertyName("resourceGroup")]
    ResourceGroup,

    [JsonPropertyName("subscription")]
    Subscription,

    [JsonPropertyName("managementGroup")]
    ManagementGroup,

    [JsonPropertyName("tenant")]
    Tenant
}

public class DeploymentPrecheckParameters
{
    [JsonPropertyName("deploymentTool")]
    public DeploymentTool DeploymentTool { get; set; } = DeploymentTool.Azd;

    [JsonPropertyName("iacType")]
    public IacType IacType { get; set; } = IacType.Bicep;

    [JsonPropertyName("azureYamlExists")]
    public bool AzureYamlExists { get; set; }

    [JsonPropertyName("bicepChecks")]
    public BicepChecks? BicepChecks { get; set; }
}

public class BicepChecks
{
    [JsonPropertyName("mainBicepChecks")]
    public MainBicepChecks MainBicepChecks { get; set; } = new();

    [JsonPropertyName("userAssignedManagedIdentityExists")]
    public bool UserAssignedManagedIdentityExists { get; set; }

    [JsonPropertyName("hasFunctionApp")]
    public bool HasFunctionApp { get; set; }

    [JsonPropertyName("functionAppChecks")]
    public FunctionAppChecks? FunctionAppChecks { get; set; }

    [JsonPropertyName("hasContainerApp")]
    public bool HasContainerApp { get; set; }

    [JsonPropertyName("containerAppChecks")]
    public ContainerAppChecks? ContainerAppChecks { get; set; }

    [JsonPropertyName("containerRegistryChecks")]
    public ContainerRegistryChecks? ContainerRegistryChecks { get; set; }

    [JsonPropertyName("containerAppsEnvironmentChecks")]
    public ContainerAppsEnvironmentChecks? ContainerAppsEnvironmentChecks { get; set; }

    [JsonPropertyName("hasAppService")]
    public bool HasAppService { get; set; }

    [JsonPropertyName("appServiceChecks")]
    public AppServiceChecks? AppServiceChecks { get; set; }
}

public class MainBicepChecks
{
    [JsonPropertyName("mainDotBicepFileExists")]
    public bool MainDotBicepFileExists { get; set; }

    [JsonPropertyName("mainParametersExists")]
    public bool MainParametersExists { get; set; }

    [JsonPropertyName("targetScope")]
    public TargetScopeType TargetScope { get; set; } = TargetScopeType.ResourceGroup;

    [JsonPropertyName("mainParametersParameters")]
    public string[] MainParametersParameters { get; set; } = [];

    [JsonPropertyName("mainDotBicepFileOutputs")]
    public string[] MainDotBicepFileOutputs { get; set; } = [];

    [JsonPropertyName("resourceNamesUseResourceToken")]
    public bool ResourceNamesUseResourceToken { get; set; }

    [JsonPropertyName("resourceTokenFormat")]
    public string ResourceTokenFormat { get; set; } = string.Empty;

    [JsonPropertyName("azdEnvNameTagExists")]
    public bool AzdEnvNameTagExists { get; set; }
}

public class RoleAssignment
{
    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    [JsonPropertyName("roleDefinitionId")]
    public string RoleDefinitionId { get; set; } = string.Empty;

    [JsonPropertyName("principalType")]
    public string PrincipalType { get; set; } = "ServicePrincipal";
}

public class FunctionAppChecks
{
    [JsonPropertyName("userAssignedManagedIdentityAttached")]
    public bool UserAssignedManagedIdentityAttached { get; set; }

    [JsonPropertyName("userAssignedManagedIdentityRoleAssignments")]
    public RoleAssignment[] UserAssignedManagedIdentityRoleAssignments { get; set; } = [];

    [JsonPropertyName("diagnosticSettingsExists")]
    public bool DiagnosticSettingsExists { get; set; }

    [JsonPropertyName("storageAccountExists")]
    public bool StorageAccountExists { get; set; }
}

public class RegistryConnection
{
    [JsonPropertyName("identity")]
    public string Identity { get; set; } = string.Empty;

    [JsonPropertyName("server")]
    public string? Server { get; set; }
}

public class ContainerAppChecks
{
    [JsonPropertyName("userAssignedManagedIdentityAttached")]
    public bool UserAssignedManagedIdentityAttached { get; set; }

    [JsonPropertyName("corsEnabled")]
    public bool CorsEnabled { get; set; }

    [JsonPropertyName("containerAppDefinesAllUsedSecrets")]
    public bool ContainerAppDefinesAllUsedSecrets { get; set; }

    [JsonPropertyName("templateImageUris")]
    public string[] TemplateImageUris { get; set; } = [];

    [JsonPropertyName("registryConnections")]
    public RegistryConnection[] RegistryConnections { get; set; } = [];
}

public class ContainerRegistryChecks
{
    [JsonPropertyName("userAssignedManagedIdentityHasAcrPullPermissions")]
    public bool UserAssignedManagedIdentityHasAcrPullPermissions { get; set; }
}

public class ContainerAppsEnvironmentChecks
{
    [JsonPropertyName("connectedToLogAnalyticsWorkspace")]
    public bool ConnectedToLogAnalyticsWorkspace { get; set; }
}

public class AppServiceChecks
{
    [JsonPropertyName("userAssignedManagedIdentityAttached")]
    public bool UserAssignedManagedIdentityAttached { get; set; }

    [JsonPropertyName("corsEnabled")]
    public bool CorsEnabled { get; set; }
}

public static class DeployPrecheckModelsSchema
{
    public static readonly JsonObject Schema = new JsonObject
    {
        ["type"] = "object",
        ["description"] = "Checks associated with infrastructure and deployment files.",
        ["properties"] = new JsonObject
        {
            ["deploymentTool"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Deployment tool used. Can be 'azd' or 'azcli'. 'azd' is recommended.",
                ["enum"] = new JsonArray("azd", "azcli")
            },
            ["iacType"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "The type of IaC file used for deployment. Can be 'bicep'.",
                ["enum"] = new JsonArray("bicep")
            },
            ["azureYamlExists"] = new JsonObject
            {
                ["type"] = "boolean",
                ["description"] = "True if azure.yaml exists in the root of the workspace. False otherwise. This is required for azd deployments."
            },
            ["bicepChecks"] = new JsonObject
            {
                ["type"] = new JsonArray("object", "null"),
                ["properties"] = new JsonObject
                {
                    ["mainBicepChecks"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["description"] = "Checks based on the main.bicep file and main.parameters.json files.",
                        ["properties"] = new JsonObject
                        {
                            ["mainDotBicepFileExists"] = new JsonObject
                            {
                                ["type"] = "boolean",
                                ["description"] = "True if main.bicep exists in the infra/ folder at the root of the workspace. False otherwise."
                            },
                            ["mainParametersExists"] = new JsonObject
                            {
                                ["type"] = "boolean",
                                ["description"] = "True if main.parameters.json exists in the infra/ folder at the root of the workspace. False otherwise."
                            },
                            ["targetScope"] = new JsonObject
                            {
                                ["type"] = "string",
                                ["description"] = "The target scope of the main.bicep file defined by targetScope = XXX. Can be 'subscription', 'resourceGroup', 'tenant', or 'managementGroup'. By default, it is 'resourceGroup' if not found in main.bicep.",
                                ["enum"] = new JsonArray("subscription", "resourceGroup", "managementGroup", "tenant")
                            },
                            ["mainParametersParameters"] = new JsonObject
                            {
                                ["type"] = "array",
                                ["description"] = "All parameters defined in main.parameters.json.",
                                ["items"] = new JsonObject
                                {
                                    ["type"] = "string",
                                    ["description"] = "The name of a parameter defined in main.parameters.json."
                                }
                            },
                            ["mainDotBicepFileOutputs"] = new JsonObject
                            {
                                ["type"] = "array",
                                ["description"] = "All the outputs at the end of main.bicep file. Do not include outputs from other files. They are declared like `output VAR_NAME <type> = <value>`. Give me the VAR_NAME.",
                                ["items"] = new JsonObject
                                {
                                    ["type"] = "string",
                                    ["description"] = "The name of an output defined in main.bicep file (NOT other bicep files)."
                                }
                            },
                            ["resourceNamesUseResourceToken"] = new JsonObject
                            {
                                ["type"] = "boolean",
                                ["description"] = "True if all resource names in the bicep file use the resource token."
                            },
                            ["resourceTokenFormat"] = new JsonObject
                            {
                                ["type"] = "string",
                                ["description"] = "Format of the resource token used to construct resource names."
                            },
                            ["azdEnvNameTagExists"] = new JsonObject
                            {
                                ["type"] = "boolean",
                                ["description"] = "True if the azd-env-name tag exists on the resource group resource. False otherwise."
                            }
                        },
                        ["required"] = new JsonArray("mainDotBicepFileExists", "targetScope", "mainParametersExists", "mainParametersParameters", "mainDotBicepFileOutputs", "resourceNamesUseResourceToken", "resourceTokenFormat", "azdEnvNameTagExists")
                    },
                    ["userAssignedManagedIdentityExists"] = new JsonObject
                    {
                        ["type"] = "boolean",
                        ["description"] = "True if a user-assigned managed identity is created for the infrastructure. NOT a system-assigned managed identity. False otherwise."
                    },
                    ["hasFunctionApp"] = new JsonObject
                    {
                        ["type"] = "boolean",
                        ["description"] = "True if the infrastructure has a function app defined."
                    },
                    ["functionAppChecks"] = new JsonObject
                    {
                        ["type"] = new JsonArray("object", "null"),
                        ["description"] = "Checks specific to infrastrucutre that provisions the function app.",
                        ["properties"] = new JsonObject
                        {
                            ["userAssignedManagedIdentityAttached"] = new JsonObject
                            {
                                ["type"] = "boolean",
                                ["description"] = "True if the infrastructure's user-assigned managed identity is attached to the function app. False otherwise."
                            },
                            ["userAssignedManagedIdentityRoleAssignments"] = new JsonObject
                            {
                                ["type"] = "array",
                                ["description"] = "Role assignments for the user-assigned managed identity attached to the resource",
                                ["items"] = new JsonObject
                                {
                                    ["type"] = "object",
                                    ["properties"] = new JsonObject
                                    {
                                        ["scope"] = new JsonObject
                                        {
                                            ["type"] = "string",
                                            ["description"] = "Resource scope for the role assignment"
                                        },
                                        ["roleDefinitionId"] = new JsonObject
                                        {
                                            ["type"] = "string",
                                            ["description"] = "The GUID format role definition ID (e.g., 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b' for Storage Blob Data Owner)"
                                        },
                                        ["principalType"] = new JsonObject
                                        {
                                            ["type"] = "string",
                                            ["description"] = "Type of the principal for the role assignment",
                                            ["enum"] = new JsonArray("ServicePrincipal")
                                        }
                                    },
                                    ["required"] = new JsonArray("scope", "roleDefinitionId", "principalType")
                                }
                            },
                            ["diagnosticSettingsExists"] = new JsonObject
                            {
                                ["type"] = "boolean",
                                ["description"] = "True if the resource has diagnostic settings defined. Resource type is called Microsoft.Insights/diagnosticSettings. False otherwise."
                            },
                            ["storageAccountExists"] = new JsonObject
                            {
                                ["type"] = "boolean",
                                ["description"] = "True if a storage account exists for the deployed infrastrucutre. Function apps have a dependency on storage accounts. False otherwise."
                            }
                        },
                        ["required"] = new JsonArray("userAssignedManagedIdentityAttached", "userAssignedManagedIdentityRoleAssignments", "diagnosticSettingsExists", "storageAccountExists")
                    },
                    ["hasContainerApp"] = new JsonObject
                    {
                        ["type"] = "boolean",
                        ["description"] = "True if the infrastructure has a container app defined."
                    },
                    ["containerAppChecks"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["description"] = "Checks specific to infrastrucutre that provisions the container app.",
                        ["properties"] = new JsonObject
                        {
                            ["userAssignedManagedIdentityAttached"] = new JsonObject
                            {
                                ["type"] = "boolean",
                                ["description"] = "True if the infrastructure's user-assigned managed identity is attached to the container app. False otherwise."
                            },
                            ["corsEnabled"] = new JsonObject
                            {
                                ["type"] = "boolean",
                                ["description"] = "True if the container app has CORSPolicy enabled and fully allowed. This is configured on ingress.corsPolicy. False otherwise."
                            },
                            ["containerAppDefinesAllUsedSecrets"] = new JsonObject
                            {
                                ["type"] = "boolean",
                                ["description"] = "True if the container apps define all used secrets. False otherwise."
                            },
                            ["templateImageUris"] = new JsonObject
                            {
                                ["type"] = "array",
                                ["description"] = "An array of template images used in the container apps. At container app properties: properties.template.containers.image.",
                                ["items"] = new JsonObject
                                {
                                    ["type"] = "string",
                                    ["description"] = "The template image value at property properties.template.containers.image."
                                }
                            },
                            ["registryConnections"] = new JsonObject
                            {
                                ["type"] = "array",
                                ["description"] = "Registry settings on each of the container apps. Located at properties.configuration.registries.* of each container app.",
                                ["items"] = new JsonObject
                                {
                                    ["type"] = "object",
                                    ["properties"] = new JsonObject
                                    {
                                        ["identity"] = new JsonObject
                                        {
                                            ["type"] = "string",
                                            ["description"] = "properties.configuration.registries.secretRef.identity"
                                        },
                                        ["server"] = new JsonObject
                                        {
                                            ["type"] = new JsonArray("string", "null"),
                                            ["description"] = "properties.configuration.registries.server"
                                        }
                                    },
                                    ["required"] = new JsonArray("identity")
                                }
                            }
                        },
                        ["required"] = new JsonArray("userAssignedManagedIdentityAttached", "corsEnabled", "containerAppDefinesAllUsedSecrets", "templateImageUris", "registryConnections")
                    },
                    ["containerRegistryChecks"] = new JsonObject
                    {
                        ["type"] = new JsonArray("object", "null"),
                        ["description"] = "Checks specific to container app registry (shared between all container apps).",
                        ["properties"] = new JsonObject
                        {
                            ["userAssignedManagedIdentityHasAcrPullPermissions"] = new JsonObject
                            {
                                ["type"] = "boolean",
                                ["description"] = "True if the user-assigned managed identity (NOT system-assigned managed identity) has the role assignment AcrPull (7f951dda-4ed3-4680-a7ca-43fe172d538d) into the container registry, False otherwise."
                            }
                        },
                        ["required"] = new JsonArray("userAssignedManagedIdentityHasAcrPullPermissions")
                    },
                    ["containerAppsEnvironmentChecks"] = new JsonObject
                    {
                        ["type"] = new JsonArray("object", "null"),
                        ["description"] = "Checks specific to container app environment (shared between all container apps).",
                        ["properties"] = new JsonObject
                        {
                            ["connectedToLogAnalyticsWorkspace"] = new JsonObject
                            {
                                ["type"] = "boolean",
                                ["description"] = "True if the container app environment is connected to a Log Analytics workspace. False otherwise."
                            }
                        },
                        ["required"] = new JsonArray("connectedToLogAnalyticsWorkspace")
                    },
                    ["hasAppService"] = new JsonObject
                    {
                        ["type"] = "boolean",
                        ["description"] = "True if the infrastructure has an app service defined. False otherwise."
                    },
                    ["appServiceChecks"] = new JsonObject
                    {
                        ["type"] = new JsonArray("object", "null"),
                        ["description"] = "Checks specific to infrastrucutre that provisions the app service.",
                        ["properties"] = new JsonObject
                        {
                            ["userAssignedManagedIdentityAttached"] = new JsonObject
                            {
                                ["type"] = "boolean",
                                ["description"] = "True if the infrastructure's user-assigned managed identity is attached to the app service. False otherwise."
                            },
                            ["corsEnabled"] = new JsonObject
                            {
                                ["type"] = "boolean",
                                ["description"] = "True if the app service has CORSPolicy enabled and fully allowed. This is configured on SiteConfig.cors. False otherwise."
                            }
                        },
                        ["required"] = new JsonArray("userAssignedManagedIdentityAttached", "corsEnabled")
                    }
                },
                ["required"] = new JsonArray("mainBicepChecks", "userAssignedManagedIdentityExists", "hasFunctionApp", "hasContainerApp", "hasAppService")
            }
        },
        ["required"] = new JsonArray("deploymentTool", "iacType", "azureYamlExists")
    };

}