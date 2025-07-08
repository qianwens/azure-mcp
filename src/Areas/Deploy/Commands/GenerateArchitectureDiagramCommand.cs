// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;
using AzureMcp.Commands;
using AzureMcp.Helpers;
using Microsoft.Extensions.Logging;
using AzureMcp.Areas.Deploy.Options;
using System.Text.Json.Nodes;

namespace AzureMcp.Areas.Deploy.Commands;

public sealed class GenerateArchitectureDiagramCommand(ILogger<GenerateArchitectureDiagramCommand> logger) : GlobalCommand<RawMcpToolInputOptions>()
{
    private const string CommandTitle = "Generate Architecture Diagram";
    private readonly ILogger<GenerateArchitectureDiagramCommand> _logger = logger;

    public override string Name => "generate_architecture_diagram";

    private readonly Option<string> _rawMcpToolInputOption = DeployOptionDefinitions.RawMcpToolInput.RawMcpToolInputOption;

    public override string Description =>
        "Returns best practices for secure, production-grade Azure SDK usage. " +
        "Call this command and follow its guidance before generating or suggesting any Azure SDK code. " +
        "This command returns the content of the markdown file as a string array.";

    public override string Title => "Generate Architecture Diagram";

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.AddOption(_rawMcpToolInputOption);
    }

    protected override RawMcpToolInputOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.RawMcpToolInput = parseResult.GetValueForOption(_rawMcpToolInputOption);
        return options;
    }

    [McpServerTool(Destructive = false, ReadOnly = true, Title = CommandTitle)]
    public override Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
    {
        var options = BindOptions(parseResult);
        var rawMcpToolInput = options.RawMcpToolInput;
        if (string.IsNullOrWhiteSpace(rawMcpToolInput))
        {
            throw new ArgumentException("App topology cannot be null or empty.", nameof(options.RawMcpToolInput));
        }

        AppTopology appTopology;
        try
        {
            appTopology = JsonSerializer.Deserialize(rawMcpToolInput, DeployJsonContext.Default.AppTopology) 
                ?? throw new ArgumentException("Failed to deserialize app topology.", nameof(rawMcpToolInput));
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid JSON format: {ex.Message}", nameof(rawMcpToolInput), ex);
        }

        // TODO: Use the appTopology object to generate architecture diagram
        _logger.LogInformation("Successfully parsed app topology with {ServiceCount} services", appTopology.Services.Length);

        return Task.FromResult(context.Response);
    }
}
