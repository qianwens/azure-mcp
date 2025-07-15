using System.Text.Json.Nodes;

namespace AzureMcp.Areas.Deploy.Models;


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

