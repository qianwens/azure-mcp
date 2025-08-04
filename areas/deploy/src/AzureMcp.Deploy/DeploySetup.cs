// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Core.Areas;
using AzureMcp.Core.Commands;
using AzureMcp.Deploy.Commands;
using AzureMcp.Deploy.Commands.InfraCodeRules;
using AzureMcp.Deploy.Commands.Plan;
using AzureMcp.Deploy.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Deploy;

public sealed class DeploySetup : IAreaSetup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IDeployService, DeployService>();
    }

    public void RegisterCommands(CommandGroup rootGroup, ILoggerFactory loggerFactory)
    {
        var deploy = new CommandGroup("deploy", "Deploy commands for deploying applications to Azure, including sub commands: "
            + "- plan-get: entry point to help the agent deploy a project to Azure. Agent should read its output and generate a deploy plan in '.azure/plan.copilotmd' for execution steps, recommended azure services based on the information agent detected from project. Before calling this tool, please scan this workspace to detect the services to deploy and their dependent services; "
            + "- iac-rules-get: offers guidelines for creating Bicep/Terraform files to deploy applications on Azure; "
            + "- azd-app-log-get: fetch logs from log analytics workspace for Container Apps, App Services, function apps that were deployed through azd; "
            + "- cicd-pipeline-guidance-get: guidance to create a CI/CD pipeline which provision Azure resources and build and deploy applications to Azure; "
            + "- architecture-diagram-generate: generates an azure service architecture diagram for the application based on the provided app topology; ");
        rootGroup.AddSubGroup(deploy);

        deploy.AddCommand("plan-get", new PlanGetCommand(loggerFactory.CreateLogger<PlanGetCommand>()));

        deploy.AddCommand("iac-rules-get", new IaCRulesGetCommand(loggerFactory.CreateLogger<IaCRulesGetCommand>()));

        deploy.AddCommand("azd-app-log-get", new AzdAppLogGetCommand(loggerFactory.CreateLogger<AzdAppLogGetCommand>()));

        deploy.AddCommand("cicd-pipeline-guidance-get", new PipelineGenerateCommand(loggerFactory.CreateLogger<PipelineGenerateCommand>()));

        deploy.AddCommand("architecture-diagram-generate", new GenerateArchitectureDiagramCommand(loggerFactory.CreateLogger<GenerateArchitectureDiagramCommand>()));
    }
}
