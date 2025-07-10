// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

using Areas.Deploy.Services.Util;
using AzureMcp.Areas.Deploy.Models;
using AzureMcp.Areas.Deploy.Options;
using AzureMcp.Areas.Deploy.Services;
using AzureMcp.Commands;
using AzureMcp.Options;
using AzureMcp.Services.Telemetry;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.Deploy.Commands.Quota;

public sealed class AzdAppLogGetCommand(ILogger<AzdAppLogGetCommand> logger)
    : BaseCommand()
{
    private const string CommandTitle = "Check Available Azure Quota for Regions";
    private readonly ILogger<AzdAppLogGetCommand> _logger = logger;

    private readonly Option<string> _rawMcpToolInputOption = new(
        $"--{DeployOptionDefinitions.RawMcpToolInput.RawMcpToolInputName}",
        GetAzdAppLogsParametersSchema.Schema.ToJsonString()
    )
    {
        IsRequired = true
    };

    public override string Name => "get";

    public override string Description =>
        """
        This tool helps fetch logs from log analytics workspace for Container Apps, App Services, function apps that were deployed through azd. Invoke this tool directly after a successful `azd up` or when user prompts to check the app's status or provide errors in the deployed apps.
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
    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
    {
        var options = BindOptions(parseResult);
        var rawMcpToolInput = options.RawMcpToolInput;
        if (string.IsNullOrWhiteSpace(rawMcpToolInput))
        {
            throw new ArgumentException("Input cannot be null or empty.", nameof(options.RawMcpToolInput));
        }
        GetAzdAppLogsParameters? parameters;
        try
        {
            parameters = JsonSerializer.Deserialize<GetAzdAppLogsParameters>(
                          rawMcpToolInput, DeployJsonContext.Default.GetAzdAppLogsParameters)
                          ?? throw new ArgumentException("Failed to deserialize input.", nameof(rawMcpToolInput));
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid JSON format: {ex.Message}", nameof(rawMcpToolInput), ex);
        }
        _logger.LogInformation("Successfully parsed AzureRegionCheckParameters");
        if (parameters == null)
        {
            throw new ArgumentException("Parsed parameters cannot be null.", nameof(rawMcpToolInput));
        }
        if (string.IsNullOrWhiteSpace(parameters.SubscriptionId))
        {
            throw new ArgumentException("Subscription ID cannot be null or empty.", nameof(parameters.SubscriptionId));
        }
        if (string.IsNullOrWhiteSpace(parameters.AzdEnvName))
        {
            throw new ArgumentException("Azd environment name cannot be null or empty.", nameof(parameters.AzdEnvName));
        }
        if (string.IsNullOrWhiteSpace(parameters.WorkspaceFolder))
        {
            throw new ArgumentException("Workspace folder cannot be null or empty.", nameof(parameters.WorkspaceFolder));
        }

        // Parse optional date parameters
        DateTime? startTime = null;
        DateTime? endTime = null;

        if (!string.IsNullOrEmpty(parameters.StartTime) && DateTime.TryParse(parameters.StartTime, out var parsedStartTime))
        {
            startTime = parsedStartTime;
        }

        if (!string.IsNullOrEmpty(parameters.EndTime) && DateTime.TryParse(parameters.EndTime, out var parsedEndTime))
        {
            endTime = parsedEndTime;
        }
        context.Activity?.WithSubscriptionTag(new SubscriptionOptions
        {
            Subscription = parameters.SubscriptionId,
        });

        var deployService = context.GetService<IDeployService>();
        string result = await deployService.GetAzdResourceLogsAsync(
              parameters.WorkspaceFolder,
              parameters.AzdEnvName,
              parameters.SubscriptionId,
              startTime,
              endTime,
              parameters.Limit);


        context.Response.Message = result;
        return context.Response;
    }

    // Implementation-specific error handling
    protected override string GetErrorMessage(Exception ex) => ex switch
    {
        ArgumentException argEx => $"Invalid input: {argEx.Message}",
        JsonException jsonEx => $"Invalid JSON format: {jsonEx.Message}",
        UnauthorizedAccessException => "Access denied. Verify you have Reader permissions on the subscription.",
        Azure.RequestFailedException rfEx when rfEx.Status == 404 =>
            "Subscription not found. Verify the subscription ID is correct and accessible.",
        Azure.RequestFailedException rfEx when rfEx.Status == 403 =>
            "Access forbidden. Verify you have the required permissions to read subscription resources.",
        Azure.Identity.AuthenticationFailedException authEx =>
            $"Authentication failed. Please run 'az login' to sign in. Details: {authEx.Message}",
        _ => base.GetErrorMessage(ex)
    };

    protected override int GetStatusCode(Exception ex) => ex switch
    {
        ArgumentException => 400,
        JsonException => 400,
        UnauthorizedAccessException => 403,
        Azure.RequestFailedException rfEx => rfEx.Status,
        Azure.Identity.AuthenticationFailedException => 401,
        _ => base.GetStatusCode(ex)
    };

}
