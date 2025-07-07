// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;
using AzureMcp.Commands;
using AzureMcp.Helpers;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.Deploy.Commands;

public sealed class GenerateArchitectureDiagramCommand(ILogger<GenerateArchitectureDiagramCommand> logger) : GlobalCommand<AppTopologyOptions>()
{
    private const string CommandTitle = "Generate Architecture Diagram";
    private readonly ILogger<GenerateArchitectureDiagramCommand> _logger = logger;

    public override string Name => "generate_architecture_diagram";

    public override string Description =>
        "Returns best practices for secure, production-grade Azure SDK usage. " +
        "Call this command and follow its guidance before generating or suggesting any Azure SDK code. " +
        "This command returns the content of the markdown file as a string array.";

    public override string Title => "Generate Architecture Diagram";

    [McpServerTool(Destructive = false, ReadOnly = true, Title = CommandTitle)]
    public override Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
    {
        
        return Task.FromResult(context.Response);
    }
}
