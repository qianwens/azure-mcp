using System.Text.Json.Nodes;

namespace AzureMcp.Areas.Server.Commands.Tools.Models;


public class RecommendConfigsParameters
{
    public string WorkspaceFolder { get; set; } = "";
    public string ProjectName { get; set; } = "";
    public ServiceConfig[] Services { get; set; } = [];
    public bool CreateIaC { get; set; }
}

public  class ServiceConfig
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string Language { get; set; } = "";
    public string Port { get; set; } = "";
    public string AzureComputeHost { get; set; } = "";
    public DependencyConfig[] Dependencies { get; set; } = [];
    public string[] Settings { get; set; } = [];
    public DockerSettings? DockerSettings { get; set; }
}
public  class AzureYamlServiceType
{
    public const string ContainerApp = "containerapp";
    public const string AppService = "appservice";
    public const string Function = "function";
    public const string StaticWebApp = "staticwebapp";
}
public  class DockerSettings
{
    public string DockerFilePath { get; set; } = "";
    public string DockerContext { get; set; } = "";
}

public  class DependencyConfig
{
    public string Name { get; set; } = "";
    public string ServiceType { get; set; } = "";
    public string ConnectionType { get; set; } = "";
    public string[] EnvironmentVariables { get; set; } = [];
}

public static class BicepGenerateParametersSchema
{
    public static readonly JsonObject Schema = new JsonObject
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["workspaceFolder"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "The full path of the workspace folder."
            },
            ["projectName"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "The name of the project. This is used to generate the resource names."
            },
            ["createIaC"] = new JsonObject
            {
                ["type"] = "boolean",
                ["description"] = "Whether to generate IaC files for the recommended services. Default is true.",
                ["default"] = true
            },
            ["services"] = new JsonObject
            {
                ["type"] = "array",
                ["description"] = "An array of service parameters.",
                ["items"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["name"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "The name of the service."
                        },
                        ["path"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "The relative path of the service main project folder"
                        },
                        ["language"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "The programming language of the service.",
                            ["enum"] = new JsonArray("dotnet", "python", "ts", "js", "java")
                        },
                        ["port"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "The port number the service uses. Get this from Dockerfile for container apps. If not available, default to '80'."
                        },
                        ["azureComputeHost"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "The appropriate azure service that should be used to host this service. Use containerapp if the service is containerized and has a Dockerfile.",
                            ["enum"] = new JsonArray("appservice", "containerapp", "function", "staticwebapp")
                        },
                        ["dockerSettings"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["description"] = "Docker settings for the service. This is only needed if the service's azureComputeHost is containerapp.",
                            ["properties"] = new JsonObject
                            {
                                ["dockerFilePath"] = new JsonObject
                                {
                                    ["type"] = "string",
                                    ["description"] = "The absolute path to the Dockerfile for the service. If the service's azureComputeHost is not containerapp, leave blank."
                                },
                                ["dockerContext"] = new JsonObject
                                {
                                    ["type"] = "string",
                                    ["description"] = "The absolute path to the Docker build context for the service. If the service's azureComputeHost is not containerapp, leave blank."
                                }
                            },
                            ["required"] = new JsonArray("dockerFilePath", "dockerContext")
                        },
                        ["dependencies"] = new JsonObject
                        {
                            ["type"] = "array",
                            ["description"] = "An array of dependent services. A compute service may have a dependency on another compute service.",
                            ["items"] = new JsonObject
                            {
                                ["type"] = "object",
                                ["properties"] = new JsonObject
                                {
                                    ["name"] = new JsonObject
                                    {
                                        ["type"] = "string",
                                        ["description"] = "The name of the dependent service. Can be arbitary, or must reference another service in the services array if referencing azureappservice, azurecontainerapp, azurestaticwebapps, or azurefunctions."
                                    },
                                    ["serviceType"] = new JsonObject
                                    {
                                        ["type"] = "string",
                                        ["description"] = "The name of the azure service that can be used for this dependent service.",
                                        ["enum"] = new JsonArray("azureaisearch", "azureaiservices", "appservice", "azureapplicationinsights", "azurebotservice", "containerapp", "azurecosmosdb", "function", "azurekeyvault", "azuredatabaseformysql", "azureopenai", "azuredatabaseforpostgresql", "azureprivateendpoint", "azurecacheforredis", "azuresqldatabase", "azurestorageaccount", "staticwebapp", "azureservicebus", "azuresignalrservice", "azurevirtualnetwork", "azurewebpubsub")
                                    },
                                    ["connectionType"] = new JsonObject
                                    {
                                        ["type"] = "string",
                                        ["description"] = "The connection authentication type of the dependency.",
                                        ["enum"] = new JsonArray("http", "secret", "system-identity", "user-identity", "bot-connection")
                                    },
                                    ["environmentVariables"] = new JsonObject
                                    {
                                        ["type"] = "array",
                                        ["description"] = "An array of environment variables defined in source code to set up the connection.",
                                        ["items"] = new JsonObject
                                        {
                                            ["type"] = "string"
                                        }
                                    }
                                },
                                ["required"] = new JsonArray("name", "serviceType", "connectionType", "environmentVariables")
                            }
                        },
                        ["settings"] = new JsonObject
                        {
                            ["type"] = "array",
                            ["description"] = "An array of environment variables needed to run this service.  Please search the entire codebase to find environment variables.",
                            ["items"] = new JsonObject
                            {
                                ["type"] = "string"
                            }
                        }
                    },
                    ["required"] = new JsonArray("name", "path", "azureComputeHost", "language", "port", "dependencies", "settings")
                }
            }
        },
        ["required"] = new JsonArray("workspaceFolder", "services")
    };
}

