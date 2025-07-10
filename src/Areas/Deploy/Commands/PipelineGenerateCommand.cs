// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

using AzureMcp.Areas.Deploy.Models;
using AzureMcp.Areas.Deploy.Options;
using AzureMcp.Commands;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.Deploy.Commands;

public sealed class PipelineGenerateCommand(ILogger<PipelineGenerateCommand> logger)
    : BaseCommand()
{
    private const string CommandTitle = "Generate Azure Deployment Pipeline";
    private readonly ILogger<PipelineGenerateCommand> _logger = logger;

    private readonly Option<string> _rawMcpToolInputOption = new(
        $"--{DeployOptionDefinitions.RawMcpToolInput.RawMcpToolInputName}",
        PipelineGenerateParametersSchema.Schema.ToJsonString()
    )
    {
        IsRequired = true
    };

    public override string Name => "generate";

    public override string Description =>
        """
        Guidance to create a CI/CD pipeline which provision Azure resources and build and deploy applications to Azure. Use this tool BEFORE generating/creating a Github actions workflow file for DEPLOYMENT on Azure. Infrastructure files should be ready and the application should be ready to be containerized.
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
        ReadOnly = false,
        Title = CommandTitle)]
    public override Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
    {
        var options = BindOptions(parseResult);
        var rawMcpToolInput = options.RawMcpToolInput;
        if (string.IsNullOrWhiteSpace(rawMcpToolInput))
        {
            throw new ArgumentException("Input cannot be null or empty.", nameof(options.RawMcpToolInput));
        }

        PipelineGenerateParameters? parameters;
        try
        {
            parameters = JsonSerializer.Deserialize<PipelineGenerateParameters>(
                          rawMcpToolInput, DeployJsonContext.Default.PipelineGenerateParameters)
                          ?? throw new ArgumentException("Failed to deserialize input.", nameof(rawMcpToolInput));
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid JSON format: {ex.Message}", nameof(rawMcpToolInput), ex);
        }

        _logger.LogInformation("Successfully parsed PipelineGenerateParameters");
        if (parameters == null)
        {
            throw new ArgumentException("Parsed parameters cannot be null.", nameof(rawMcpToolInput));
        }

        try
        {
            // TODO: Implement pipeline generation logic
            var result = GeneratePipeline(parameters);

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

    private static string GeneratePipeline(PipelineGenerateParameters parameters)
    {
        // TODO: Implement actual pipeline generation logic
        return "Pipeline generation logic not yet implemented.";
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
