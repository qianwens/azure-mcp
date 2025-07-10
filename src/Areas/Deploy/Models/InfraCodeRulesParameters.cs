using System.Text.Json.Nodes;

namespace AzureMcp.Areas.Deploy.Models;

public sealed class InfraCodeRulesParameters
{
    public string DeploymentTool { get; set; } = string.Empty;
    public string IacType { get; set; } = string.Empty;
    public string[] ResourceTypes { get; set; } = [];
}

public static class DeploymentTool
{
    public const string Azd = "azd";
    public const string AzCLI = "azcli";
}

public static class IacType
{
    public const string Bicep = "bicep";
    public const string Terraform = "terraform";
}

public static class AzureServiceNames
{
    public const string AzureContainerApp = "containerapp";
    public const string AzureAppService = "appservice";
    public const string AzureFunctionApp = "function";
}


public static class InfraCodeRulesParametersSchema
{
    public static JsonNode Schema => JsonNode.Parse("""
    {
        "type": "object",
        "properties": {
            "deploymentTool": {
                "type": "string",
                "description": "The deployment tool to use (azd, azcli)",
                "enum": ["azd", "azcli"]
            },
            "iacType": {
                "type": "string",
                "description": "The Infrastructure as Code type (bicep, terraform)",
                "enum": ["bicep", "terraform"]
            },
            "resourceTypes": {
                "type": "array",
                "items": {
                    "type": "string",
                    "enum": [
                        "azureaisearch",
                        "azureaiservices",
                        "appservice",
                        "azureapplicationinsights",
                        "azurebotservice",
                        "containerapp",
                        "azurecosmosdb",
                        "function",
                        "azurekeyvault",
                        "azuredatabaseformysql",
                        "azureopenai",
                        "azuredatabaseforpostgresql",
                        "azureprivateendpoint",
                        "azurecacheforredis",
                        "azuresqldatabase",
                        "azurestorageaccount",
                        "staticwebapp",
                        "azureservicebus",
                        "azuresignalrservice",
                        "azurevirtualnetwork",
                        "azurewebpubsub"
                    ]
                },
                "description": "The types of Azure resources to deploy"
            }
        },
        "required": ["deploymentTool", "iacType", "resourceTypes"],
        "additionalProperties": false
    }
    """)!;
}
