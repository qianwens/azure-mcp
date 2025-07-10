// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Areas.Extension.Commands;
using AzureMcp.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AzureMcp.Areas.Deploy.Commands;
using AzureMcp.Areas.Deploy.Commands.Region;
using AzureMcp.Areas.Deploy.Commands.Plan;
using AzureMcp.Areas.Deploy.Services;
using AzureMcp.Areas.Deploy.Commands.Quota;
using AzureMcp.Areas.Deploy.Commands.InfraCodeRules;

namespace AzureMcp.Areas.Deploy;

internal sealed class DeploySetup : IAreaSetup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IDeployService, DeployService>();
    }

    public void RegisterCommands(CommandGroup rootGroup, ILoggerFactory loggerFactory)
    {
        var deploy = new CommandGroup("deploy", "Deploy commands for deploying applications to Azure");
        rootGroup.AddSubGroup(deploy);

        deploy.AddCommand("generate_architecture_diagram", new GenerateArchitectureDiagramCommand(loggerFactory.CreateLogger<GenerateArchitectureDiagramCommand>()));

        deploy.AddCommand("plan_get", new PlanGetCommand(loggerFactory.CreateLogger<PlanGetCommand>()));

        deploy.AddCommand("infra_code_rules_get", new InfraCodeRulesGetCommand(loggerFactory.CreateLogger<InfraCodeRulesGetCommand>()));

        deploy.AddCommand("region_check", new RegionCheckCommand(loggerFactory.CreateLogger<RegionCheckCommand>()));
        deploy.AddCommand("quota_check", new QuotaCheckCommand(loggerFactory.CreateLogger<QuotaCheckCommand>()));

        deploy.AddCommand("azd_app_log_get", new AzdAppLogGetCommand(loggerFactory.CreateLogger<AzdAppLogGetCommand>()));

        deploy.AddCommand("pipeline_generate", new PipelineGenerateCommand(loggerFactory.CreateLogger<PipelineGenerateCommand>()));
    }
}
