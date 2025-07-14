// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Areas.Deploy.Services.Util;
using AzureMcp.Areas.Deploy.Models;
using AzureMcp.Areas.Deploy.Options;
using AzureMcp.Areas.Deploy.Services;
using AzureMcp.Commands;
using AzureMcp.Models.Command;
using AzureMcp.Options;
using AzureMcp.Services.Telemetry;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.Deploy.Commands.Region;

public sealed class RegionCheckCommand(ILogger<RegionCheckCommand> logger)
    : BaseCommand()
{
    private const string CommandTitle = "Check Available Azure Regions";
    private readonly ILogger<RegionCheckCommand> _logger = logger;

    private readonly Option<string> _rawMcpToolInputOption = new(
        $"--{DeployOptionDefinitions.RawMcpToolInput.RawMcpToolInputName}",
        AzureRegionCheckParametersSchema.Schema.ToJsonString()
    )
    {
        IsRequired = true
    };

    public override string Name => "region-check";

    public override string Description =>
        """
        Checks available Azure regions for deployment and optionally their capabilities.
        Returns a list of regions with metadata including geography, location, and paired regions.
        Provide parameters as JSON matching the AzureRegionCheckParameters schema.
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
    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
    {
        var options = BindOptions(parseResult);
        var rawMcpToolInput = options.RawMcpToolInput;
        if (string.IsNullOrWhiteSpace(rawMcpToolInput))
        {
            throw new ArgumentException("Input cannot be null or empty.", nameof(options.RawMcpToolInput));
        }
        AzureRegionCheckParameters? parameters;
        try
        {
            parameters = JsonSerializer.Deserialize<AzureRegionCheckParameters>(
                         rawMcpToolInput, DeployJsonContext.Default.AzureRegionCheckParameters)
                         ?? throw new ArgumentException("Failed to deserialize input.", nameof(rawMcpToolInput));

            _logger.LogInformation("Successfully parsed AzureRegionCheckParameters");
            if (parameters == null)
            {
                throw new ArgumentException("Parsed parameters cannot be null.", nameof(rawMcpToolInput));
            }
            if (string.IsNullOrWhiteSpace(parameters.SubscriptionId))
            {
                throw new ArgumentException("Subscription ID cannot be null or empty.", nameof(parameters.SubscriptionId));
            }
            if (parameters.ResourceTypes is null || !parameters.ResourceTypes.Any())
            {
                throw new ArgumentException("Resource types is empty.", nameof(parameters.ResourceTypes));
            }
            context.Activity?.WithSubscriptionTag(new SubscriptionOptions
            {
                Subscription = parameters.SubscriptionId,
            });

            var deployService = context.GetService<IDeployService>();
            List<string> toolResult = await deployService.GetAvailableRegionsForResourceTypesAsync(
                parameters.ResourceTypes,
                parameters.SubscriptionId,
                parameters.CognitiveServiceProperties);

            _logger.LogInformation("Region check result: {ToolResult}", toolResult);

            context.Response.Results = toolResult?.Count > 0 ?
                ResponseResult.Create(
                    new RegionCheckCommandResult(toolResult),
                    DeployJsonContext.Default.RegionCheckCommandResult) :
                null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception occurred checking available Azure regions.");
            HandleException(context, ex);
        }

        return context.Response;
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

    internal record RegionCheckCommandResult(List<string> AvailableRegions);

}
