using Shelly_CLI.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shelly_CLI.Commands.Config;

public class ConfigListCommand : Command
{
    public override int Execute(CommandContext context)
    {
        var values = ConfigManager.GetAllConfigValues();
        var table = new Table();
        table.AddColumn("Key");
        table.AddColumn("Value");

        foreach (var kvp in values)
        {
            table.AddRow(kvp.Key, kvp.Value ?? "(null)");
        }

        AnsiConsole.Write(table);
        return 0;
    }
}
