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

    private readonly Option<string> _rawMcpToolInputOption = new(
        $"--{DeployOptionDefinitions.RawMcpToolInput.RawMcpToolInputName}",
        InfraCodeRulesParametersSchema.Schema.ToJsonString()
    )
    {
        IsRequired = true
    };

    public override string Name => "get";

    public override string Description =>
        """
        This tool helps get infrastructure code rules based on deployment tool, IaC type, and resource types. Use this tool to understand the requirements and best practices for deploying Azure resources.
        """;

    public override string Title => CommandTitle;

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.AddOption(_rawMcpToolInputOption);
    }

    private RawMcpToolInputOptions BindOptions(ParseResult parseResult)
    {
        var options = new RawMcpToolInputOptions();
        options.RawMcpToolInput = parseResult.GetValueForOption(_rawMcpToolInputOption);
        return options;
    }

    [McpServerTool(
        Destructive = false,
        ReadOnly = true,
        Title = CommandTitle)]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "")]
    public override  Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
    {
        var options = BindOptions(parseResult);
        var rawMcpToolInput = options.RawMcpToolInput;
        if (string.IsNullOrWhiteSpace(rawMcpToolInput))
        {
            throw new ArgumentException("Input cannot be null or empty.", nameof(options.RawMcpToolInput));
        }

        InfraCodeRulesParameters? parameters;
        try
        {
            parameters = JsonSerializer.Deserialize<InfraCodeRulesParameters>(
                          rawMcpToolInput, DeployJsonContext.Default.InfraCodeRulesParameters)
                          ?? throw new ArgumentException("Failed to deserialize input.", nameof(rawMcpToolInput));
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid JSON format: {ex.Message}", nameof(rawMcpToolInput), ex);
        }

        _logger.LogInformation("Successfully parsed InfraCodeRulesParameters");
        if (parameters == null)
        {
            throw new ArgumentException("Parsed parameters cannot be null.", nameof(rawMcpToolInput));
        }

        if (string.IsNullOrWhiteSpace(parameters.DeploymentTool))
        {
            throw new ArgumentException("Deployment tool cannot be null or empty.", nameof(parameters.DeploymentTool));
        }

        if (string.IsNullOrWhiteSpace(parameters.IacType))
        {
            throw new ArgumentException("IaC type cannot be null or empty.", nameof(parameters.IacType));
        }

        if (parameters.ResourceTypes == null || parameters.ResourceTypes.Length == 0)
        {
            throw new ArgumentException("Resource types cannot be null or empty.", nameof(parameters.ResourceTypes));
        }


        List<string> result = InfraCodeRuleRetriever.PopulateLLMResponse(
            parameters
        );

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
