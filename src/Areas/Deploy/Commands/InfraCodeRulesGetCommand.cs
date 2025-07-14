// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using AzureMcp.Areas.Deploy.Models;
using AzureMcp.Areas.Deploy.Options;
using AzureMcp.Commands;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.Deploy.Commands.InfraCodeRules;

public sealed class InfraCodeRulesGetCommand(ILogger<InfraCodeRulesGetCommand> logger)
    : BaseCommand()
{
    private const string CommandTitle = "Get Infrastructure Code Rules";
    private readonly ILogger<InfraCodeRulesGetCommand> _logger = logger;

    private readonly Option<string> _deploymentToolOption = DeployOptionDefinitions.InfraCodeRules.DeploymentTool;
    private readonly Option<string> _iacTypeOption = DeployOptionDefinitions.InfraCodeRules.IacType;
    private readonly Option<string> _resourceTypesOption = DeployOptionDefinitions.InfraCodeRules.ResourceTypes;

    public override string Name => "infra-code-rules-get";

    public override string Description =>
        """
        This tool provides guidelines for generating deployment code to Azure. It supports 2 deployment tools: AZD and Infrastructure as Code (IaC), including Bicep or Terraform. Azure CLI with command script.
        """;

    public override string Title => CommandTitle;

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.AddOption(_deploymentToolOption);
        command.AddOption(_iacTypeOption);
        command.AddOption(_resourceTypesOption);
    }

    private InfraCodeRulesOptions BindOptions(ParseResult parseResult)
    {
        var options = new InfraCodeRulesOptions();
        options.DeploymentTool = parseResult.GetValueForOption(_deploymentToolOption) ?? string.Empty;
        options.IacType = parseResult.GetValueForOption(_iacTypeOption) ?? string.Empty;
        options.ResourceTypes = parseResult.GetValueForOption(_resourceTypesOption) ?? string.Empty;

        return options;
    }

    [McpServerTool(
        Destructive = false,
        ReadOnly = true,
        Title = CommandTitle)]
    public override Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
    {
        var options = BindOptions(parseResult);

        if (string.IsNullOrWhiteSpace(options.DeploymentTool))
        {
            throw new ArgumentException("Deployment tool cannot be null or empty.", nameof(options.DeploymentTool));
        }

        if (string.IsNullOrWhiteSpace(options.IacType))
        {
            throw new ArgumentException("IaC type cannot be null or empty.", nameof(options.IacType));
        }

        if (string.IsNullOrWhiteSpace(options.ResourceTypes))
        {
            throw new ArgumentException("Resource types cannot be null or empty.", nameof(options.ResourceTypes));
        }

        _logger.LogInformation("Successfully parsed InfraCodeRulesOptions");

        var resourceTypes = options.ResourceTypes.Split(',')
            .Select(rt => rt.Trim())
            .Where(rt => !string.IsNullOrWhiteSpace(rt))
            .ToArray();

        List<string> result = InfraCodeRuleRetriever.PopulateLLMResponse(
            options.DeploymentTool,
            options.IacType,
            resourceTypes);

        context.Response.Message = string.Join(Environment.NewLine, result);
        return Task.FromResult(context.Response);
    }

    // Implementation-specific error handling
    protected override string GetErrorMessage(Exception ex) => ex switch
    {
        ArgumentException argEx => $"Invalid input: {argEx.Message}",
        JsonException jsonEx => $"Invalid JSON format: {jsonEx.Message}",
        _ => base.GetErrorMessage(ex)
    };

    protected override int GetStatusCode(Exception ex) => ex switch
    {
        ArgumentException => 400,
        JsonException => 400,
        _ => base.GetStatusCode(ex)
    };
}
