namespace Reimaginate.DataHub.Agent.BusinessCentral.Reference.Commands;

public sealed class CommandRunner(ReferenceAgentOperations operations)
{
    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (args.Length == 0 || Is(args[0], "--validate"))
        {
            Console.WriteLine("Configuration, DataHub client, Business Central client, mapper, mediator, and entity-pair registrations are valid.");
            return 0;
        }

        if (Is(args[0], "--smoke") && args.Length == 1)
        {
            await operations.SmokeAsync(cancellationToken);
            return 0;
        }

        if (Is(args[0], "--run-once") && args.Length == 1)
        {
            await operations.RunOnceAsync(cancellationToken);
            return 0;
        }

        if (Is(args[0], "--sync") && args.Length == 3)
        {
            await operations.SyncAsync(args[1], args[2], cancellationToken);
            return 0;
        }

        if (Is(args[0], "--merge") && args.Length == 3)
        {
            await operations.MergeAsync(args[1], args[2], cancellationToken);
            return 0;
        }

        WriteHelp();
        return 2;
    }

    public static void WriteHelp()
    {
        Console.Error.WriteLine("Commands:");
        Console.Error.WriteLine("  --validate");
        Console.Error.WriteLine("  --smoke");
        Console.Error.WriteLine("  --sync <Account|Product|SalesOrder|SalesOrderLine> <datahub-id>");
        Console.Error.WriteLine("  --merge <Customer|Item|SalesOrder|SalesOrderLine> <business-central-guid>");
        Console.Error.WriteLine("  --run-once");
        Console.Error.WriteLine("  --worker");
    }

    private static bool Is(string value, string command) =>
        value.Equals(command, StringComparison.OrdinalIgnoreCase);
}
