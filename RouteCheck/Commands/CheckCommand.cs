using RouteCheck.Commands.Settings;

using Spectre.Console.Cli;

using System.Threading;
using System.Threading.Tasks;

namespace RouteCheck.Commands;

public class CheckCommand : AsyncCommand<CheckSettings>
{
    public CheckCommand()
    {
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CheckSettings settings, CancellationToken _cancellationToken)
    {
        return 1;
    }
}