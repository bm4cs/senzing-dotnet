using Microsoft.Extensions.Logging;
using SzConnyApp.SenzingV4.Senzing;

namespace SzConnyApp.SenzingV4.Commands;

public class RepositoryPurgerCommand(
    ISzEnvironmentWrapper szEnvironment,
    ILogger<RecordLoaderCommand> logger
) : IRepositoryPurgerCommand
{
    private readonly ILogger<RecordLoaderCommand> _logger = logger;

    public void Execute()
    {
        var diagnostic = szEnvironment.Diagnostic;
        diagnostic.PurgeRepository();
    }
}
