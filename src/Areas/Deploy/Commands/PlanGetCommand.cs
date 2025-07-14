// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

using AzureMcp.Areas.Deploy.Options;
using AzureMcp.Areas.Deploy.Services.Util;
using AzureMcp.Commands;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.Deploy.Commands.Plan;

public sealed class PlanGetCommand(ILogger<PlanGetCommand> logger)
    : BaseCommand()
{
    private const string CommandTitle = "Generate Azure Deployment Plan";
    private readonly ILogger<PlanGetCommand> _logger = logger;

    private readonly Option<string> _workspaceFolderOption = DeployOptionDefinitions.PlanGet.WorkspaceFolder;
    private readonly Option<string> _projectNameOption = DeployOptionDefinitions.PlanGet.ProjectName;

    public override string Name => "plan-get";

    public override string Description =>
        """
        Entry point to help the agent deploy a service to the cloud. Agent should read its output and generate a deploy plan in '.codetocloud/plan.md' for execution steps, recommended azure services based on the information agent detected from project. Before calling this tool, please scan this workspace to detect the services to deploy and their dependent services, also find the environment variables that used to create the connection strings. Try your best to fulfill the input schema with your analyze result.
        """;

    public override string Title => CommandTitle;

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.AddOption(_workspaceFolderOption);
        command.AddOption(_projectNameOption);
    }

    private PlanGetOptions BindOptions(ParseResult parseResult)
    {
        return new PlanGetOptions
        {
            WorkspaceFolder = parseResult.GetValueForOption(_workspaceFolderOption) ?? string.Empty,
            ProjectName = parseResult.GetValueForOption(_projectNameOption) ?? string.Empty
        };
    }

    [McpServerTool(
        Destructive = false,
        ReadOnly = true,
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
            var planTemplate = DeploymentPlanTemplateUtil.GetPlanTemplate(options.ProjectName);

            context.Response.Message = planTemplate;
            context.Response.Status = 200;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating deployment plan");
            HandleException(context, ex);
        }
        return Task.FromResult(context.Response);

    }

}
