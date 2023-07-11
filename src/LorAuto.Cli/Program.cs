using System.CommandLine;
using LorAuto.Cli.Commands;
using Serilog;

Console.CancelKeyPress += (_, eventArgs) =>
{
    Log.Logger.Warning("Please note that the application's shutdown process has been initiated and may take a few moments to complete");
    eventArgs.Cancel = true;
};

var rootCommand = new BotCommand();
int ret = await rootCommand.InvokeAsync(args);

return ret;