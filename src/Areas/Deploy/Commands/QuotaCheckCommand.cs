// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Areas.Deploy.Options;
using AzureMcp.Areas.Deploy.Services;
using AzureMcp.Commands;
using AzureMcp.Commands.Subscription;
using AzureMcp.Services.Telemetry;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.Deploy.Commands.Quota;

public class QuotaCheckCommand(ILogger<QuotaCheckCommand> logger) : BaseCommand()
{
    private const string CommandTitle = "Check Available Azure Quota for Regions";
    private readonly ILogger<QuotaCheckCommand> _logger = logger;

    private readonly Option<string> _regionOption = DeployOptionDefinitions.QuotaCheck.Region;
    private readonly Option<string> _resourceTypesOption = DeployOptionDefinitions.QuotaCheck.ResourceTypes;

    public override string Name => "quota-check";

    public override string Description =>
        """
        This tool will check the Azure quota availability for the resources that are going to be deployed.
        """;

    public override string Title => CommandTitle;

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.AddOption(_regionOption);
        command.AddOption(_resourceTypesOption);
    }

    protected QuotaCheckOptions BindOptions(ParseResult parseResult)
    {
        var options = new QuotaCheckOptions();
        options.Region = parseResult.GetValueForOption(_regionOption) ?? string.Empty;
        options.ResourceTypes = parseResult.GetValueForOption(_resourceTypesOption) ?? string.Empty;
        options.SubscriptionId = options.Subscription ?? string.Empty;
        return options;
    }

    [McpServerTool(
        Destructive = false,
        ReadOnly = true,
        Title = CommandTitle)]
    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
    {
        var options = BindOptions(parseResult);

        if (string.IsNullOrWhiteSpace(options.SubscriptionId))
        {
            throw new ArgumentException("Subscription ID cannot be null or empty.", nameof(options.SubscriptionId));
        }
        if (string.IsNullOrWhiteSpace(options.Region))
        {
            throw new ArgumentException("Region cannot be null or empty.", nameof(options.Region));
        }
        if (string.IsNullOrWhiteSpace(options.ResourceTypes))
        {
            throw new ArgumentException("Resource types is empty.", nameof(options.ResourceTypes));
        }

        _logger.LogInformation("Successfully parsed QuotaCheckOptions");

        try
        {
            context.Activity?.WithSubscriptionTag(options);
            var ResourceTypes = options.ResourceTypes.Split(',')
                .Select(rt => rt.Trim())
                .Where(rt => !string.IsNullOrWhiteSpace(rt))
                .ToList();
            var deployService = context.GetService<IDeployService>();
            string toolResult = await deployService.GetAzureQuotaAsync(
                ResourceTypes,
                options.SubscriptionId,
                options.Region);

            _logger.LogInformation("Quota check result: {ToolResult}", toolResult);

            context.Response.Message = toolResult;
            context.Response.Status = 200;
            return context.Response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Azure quota");
            HandleException(context, ex);
            return context.Response;
        }
    }

    // Implementation-specific error handling
    protected override string GetErrorMessage(Exception ex) => ex switch
    {
        ArgumentException argEx => $"Invalid input: {argEx.Message}",
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
        UnauthorizedAccessException => 403,
        Azure.RequestFailedException rfEx => rfEx.Status,
        Azure.Identity.AuthenticationFailedException => 401,
        _ => base.GetStatusCode(ex)
    };

}
