using AzureMcp.Areas.Extension.Commands;
using AzureMcp.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.Deploy;

internal sealed class DeploySetup : IAreaSetup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // No additional services needed for Extension area
    }

    public void RegisterCommands(CommandGroup rootGroup, ILoggerFactory loggerFactory)
    {
        var extension = new CommandGroup("deploy", "deploy commands for deploy application to Azure");
        rootGroup.AddSubGroup(extension);

        extension.AddCommand("generate_architecture_diagram", new AzCommand(loggerFactory.CreateLogger<AzCommand>()));
    }
}
