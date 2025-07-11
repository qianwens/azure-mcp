// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Areas.Deploy.Options;
using AzureMcp.Commands;
using AzureMcp.Commands.Subscription;
using AzureMcp.Services.Telemetry;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.Deploy.Commands;

public sealed class PipelineGenerateCommand(ILogger<PipelineGenerateCommand> logger)
    : SubscriptionCommand<PipelineGenerateOptions>()
{
    private const string CommandTitle = "Generate Azure Deployment Pipeline";
    private readonly ILogger<PipelineGenerateCommand> _logger = logger;

    private readonly Option<bool> _useAZDPipelineConfigOption = DeployOptionDefinitions.PipelineGenerateOptions.UseAZDPipelineConfig;
    private readonly Option<string> _organizationNameOption = DeployOptionDefinitions.PipelineGenerateOptions.OrganizationName;
    private readonly Option<string> _repositoryNameOption = DeployOptionDefinitions.PipelineGenerateOptions.RepositoryName;
    private readonly Option<string> _githubEnvironmentNameOption = DeployOptionDefinitions.PipelineGenerateOptions.GithubEnvironmentName;

    public override string Name => "generate";

    public override string Description =>
        """
        Guidance to create a CI/CD pipeline which provision Azure resources and build and deploy applications to Azure. Use this tool BEFORE generating/creating a Github actions workflow file for DEPLOYMENT on Azure. Infrastructure files should be ready and the application should be ready to be containerized.
        """;

    public override string Title => CommandTitle;

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.AddOption(_useAZDPipelineConfigOption);
        command.AddOption(_organizationNameOption);
        command.AddOption(_repositoryNameOption);
        command.AddOption(_githubEnvironmentNameOption);
    }

    protected override PipelineGenerateOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.UseAZDPipelineConfig = parseResult.GetValueForOption(_useAZDPipelineConfigOption);
        options.OrganizationName = parseResult.GetValueForOption(_organizationNameOption);
        options.RepositoryName = parseResult.GetValueForOption(_repositoryNameOption);
        options.GithubEnvironmentName = parseResult.GetValueForOption(_githubEnvironmentNameOption);
        return options;
    }

    [McpServerTool(
        Destructive = false,
        ReadOnly = false,
        Title = CommandTitle)]
    public override Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
    {
        var options = BindOptions(parseResult);

        try
        {
            if (!Validate(parseResult.CommandResult, context.Response).IsValid)
            {
                return Task.FromResult(context.Response);
            }

            context.Activity?.WithSubscriptionTag(options);
            var result = GeneratePipelineGuidelines(options);

            context.Response.Message = result;
            context.Response.Status = 200;
            return Task.FromResult(context.Response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating deployment pipeline");
            context.Response.Status = 500;
            context.Response.Message = $"Error generating deployment pipeline: {ex.Message}";
            return Task.FromResult(context.Response);
        }
    }

    private static string GeneratePipelineGuidelines(PipelineGenerateOptions options)
    {
        if (options.UseAZDPipelineConfig)
        {
            return AZDPipelinePrompt;
        }
        else
        {
            return AZCLIPipelinePrompt(options);
        }
    }

    private static readonly string AZDPipelinePrompt = "Run \"azd pipeline config\" to help the user create a deployment pipeline.\n";

    private static string AZCLIPipelinePrompt(PipelineGenerateOptions options)
    {
        const string defaultEnvironment = "dev";
        var environmentNamePrompt = !string.IsNullOrEmpty(options.GithubEnvironmentName)
            ? $"Use {options.GithubEnvironmentName} for environment name of the deployment job."
            : $"Use '{defaultEnvironment}' for the $environment for the deployment job.";

        var subscriptionIdPrompt = !string.IsNullOrEmpty(options.Subscription) && CheckGUIDFormat(options.Subscription)
            ? $"User is deploying to subscription {options.Subscription}"
            : "Use \"az account show --query id -o tsv\" as default subscription ID.";

        var organizationName = !string.IsNullOrEmpty(options.OrganizationName) ? options.OrganizationName : "{$organization-of-repo}";
        var repositoryName = !string.IsNullOrEmpty(options.RepositoryName) ? options.RepositoryName : "{$repository-name}";
        var environmentName = !string.IsNullOrEmpty(options.GithubEnvironmentName) ? options.GithubEnvironmentName : defaultEnvironment;

        var subjectConfig = $"repo:{organizationName}/{repositoryName}:environment:{environmentName}";
        var environmentArg = !string.IsNullOrEmpty(options.GithubEnvironmentName) ? $"--env {options.GithubEnvironmentName}" : "--env dev";
        var environmentCreateCommand = $"gh api --method PUT -H \"Accept: application/vnd.github+json\" repos/{organizationName}/{repositoryName}/environments/{environmentName}";
        var jsonParameters = $"{{\"name\":\"github-federated\",\"issuer\":\"https://token.actions.githubusercontent.com\",\"subject\":\"{subjectConfig}\",\"audiences\":[\"api://AzureADTokenExchange\"]}}";
        return $"""
Help the user to set up a CI/CD pipeline to deploy to Azure with the following steps IN ORDER. **RUN the commands directly and DO NOT just give instructions. DO NOT ask user to provide information.**

  1. First generate a Github Actions workflow file to deploy to Azure. {environmentNamePrompt} The pipeline at least contains these steps in order:
    a. Azure login: login with a service principal using OIDC. DO NOT use secret.
    b. Docker build
    c. Deploy infrastructure: Use AZ CLI "az deployment sub/group create" command. Use "az deployment sub/group wait" to wait the deployment to finish. Refer to the infra files to set the correct parameters.
    d. Azure Container Registry login: login into the container registry created in the previous step. Use "az acr list" to get the correct registry name if you are not sure.
    e. Push app images to ACR
    f. Deploy to hosting service. Use the infra deployment output or AZ CLI to list hosting resources. Find the name or ID of the hosting resources from "az <resource> list" if you are not sure.

    Pay attention to the name of the branches to which the pipeline is triggered.

  2. Run '{environmentCreateCommand}' to create the environment in the repository.

  3. - {subscriptionIdPrompt}
     - Run "az ad sp create-for-rbac" command to create a service principal. Grant the service principal *Contributor* role of the subscription. Also grant the service principal *User Access Administrator*
    **Use Federated credentials in order to authenticate to Azure services from GitHub Actions workflows. The command is **az ad app federated-credential create --id <$service-principal-app-id> --parameters '{jsonParameters}'**. You MUST use ' and \"(DO NOT forget the slash \) in the command. Use the current Github org/repo to fill in the subject property.

  4. Run command "gh secret set <secret_name> --body <secret_value> {environmentArg}" to configure the AZURE_CLIENT_ID, AZURE_TENANT_ID and AZURE_SUBSCRIPTION_ID of the service principal in Github secrets using Github CLI.

  ** DO NOT prompt user for any information. Find them on your own. **
""";
    }

    private static bool CheckGUIDFormat(string input)
    {
        return Guid.TryParse(input, out _);
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
