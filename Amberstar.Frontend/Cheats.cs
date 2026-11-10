using System.Linq;
using System.Text;
using Amberstar.GameData;

namespace Amberstar.Frontend;

using G = Game.Game;

public static class Cheats
{
    record Parameter(string Name)
    {
        public Parameter(string name, string description)
            : this(name)
        {
            Description = description;
        }

        public string Description { get; } = "";

        public static implicit operator Parameter(string str) => new(str);
    }

    static readonly Dictionary<string, (Action<G, string[]> Action, string Description, int RequiredParameterCount, Parameter[] Parameters)> commands = [];
    static readonly Queue<string> commandQueue = [];

    static Cheats()
    {
        static void AddCommand(string name, Action<G, string[]> action, string description, int requiredParameterCount, params Parameter[] parameters)
            => commands.Add(name, (action, description, requiredParameterCount, parameters));

        AddCommand("help", Help, "Shows the help (for some command)", 0, "command");
        AddCommand("teleport", Teleport, "Teleports to a new location", 1, "mapIndex", "x", "y", new("dir", "0: Up, 1: Right, 2: Down, 3: Left"));
        AddCommand("maps", Maps, "Shows all maps", 0, "partialMapNameOrIndex");
        AddCommand("where", Where, "Shows current location", 0);
        AddCommand("events", Events, "Shows the events of a map with their positions and raw data", 0, "mapIndex");
        AddCommand("gold", Gold, "Gives gold", 1, "amount");
        AddCommand("food", Food, "Gives food", 1, "amount");
        AddCommand("items", Items, "Shows all items", 0, "partialItemName");
        AddCommand("item", Item, "Gives an item", 1, "itemIndexOrName", "count", new("slot", "1 to 6, default is the active party member"));
        AddCommand("noclip", NoClip, "Moves through walls and other obstacles on 2D and 3D maps\n(like the super chicken of the original)", 0, new Parameter("active", "1: on, 0: off, default is to switch it"));
        AddCommand("members", Members, "Shows all characters who can join the party", 0, "partialName");
        AddCommand("invite", Invite, "Adds a character to the party like asking to join\n(the person is removed from its map)", 1, "characterIndexOrName");
    }

    public static void Init()
    {
        Help(null!, []);
        EndCommand();
    }

    public static void EnqueueCommand(string input)
    {
        lock (commandQueue)
        {
            commandQueue.Enqueue(input);
        }
    }

    public static void HandleQueuedCommands(G game)
    {
        lock (commandQueue)
        {
            while (commandQueue.Count > 0)
            {
                string command = commandQueue.Dequeue();

                ProcessCheatCommand(game, command);
                EndCommand();
            }
        }
    }

    /// <summary>
    /// Executes a cheat command directly (e.g. from a scenario script).
    /// </summary>
    public static void Execute(G game, string command) => ProcessCheatCommand(game, command);

    private static void ProcessCheatCommand(G game, string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return;

        var parts = command.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (!commands.TryGetValue(parts[0].ToLower(), out var commandInfo))
        {
            Console.WriteLine($"Invalid command: '{parts[0]}'");
            return;
        }

        int paramCount = parts.Length - 1;

        if (paramCount < commandInfo.RequiredParameterCount)
        {
            Console.WriteLine($"Command '{parts[0]}' needs at least {commandInfo.RequiredParameterCount} parameter(s). Use 'help {parts[0]}'");
            return;
        }
        else if (paramCount > commandInfo.Parameters.Length && parts[0].ToLower() != "help")
        {
            Console.WriteLine($"Command '{parts[0]}' supports at max {commandInfo.Parameters.Length} parameter(s). Use 'help {parts[0]}'");
            return;
        }

        commandInfo.Action(game, parts[1..]);
    }

    private static void EndCommand()
    {
        Console.WriteLine();
        Console.Write("> ");
    }

    static bool PrintCommandInfo(string name)
    {
        if (!commands.TryGetValue(name, out var command))
            return false;

        var lineBuilder = new StringBuilder();

        lineBuilder.Append("  ");
        lineBuilder.Append(name);

        for (int i = 0; i < command.Parameters.Length; i++)
            lineBuilder.Append(i < command.RequiredParameterCount ? $" <{command.Parameters[i].Name}>" : $" ({command.Parameters[i].Name})");

        lineBuilder.Append(" - ");

        int offset = lineBuilder.Length;
        var indent = new string(' ', offset);
        var description = command.Description.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd('\n').Replace("\n", "\n" + indent);

        lineBuilder.AppendLine(description);

        foreach (var param in command.Parameters.Where(p => p.Description.Length > 0))
            lineBuilder.AppendLine($"    {param.Name} - {param.Description}");

        Console.Write(lineBuilder.ToString());

        return true;
    }

    internal static void Help(G _, string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Available commands:");
            Console.WriteLine();

            foreach (var command in commands.Keys.Order())
                Console.WriteLine(command);
        }
        else
        {
            if (!PrintCommandInfo(args[0].ToLower()))
                Console.WriteLine($"Unknown command: '{args[0]}'");
        }
    }

    private static int ParseNumber(string arg, int defaultValue) => int.TryParse(arg, out int n) ? n : defaultValue;

    // Enum.TryParse also accepts any number, even if the enum has no such value.
    private static TEnum ParseEnum<TEnum>(string arg, TEnum defaultValue) where TEnum : struct, Enum => Enum.TryParse(arg, true, out TEnum n) && Enum.IsDefined(n) ? n : defaultValue;

    private static void Maps(G game, string[] args)
    {
        IEnumerable<(int Index, string Name)> maps = game.GetMaps();

        if (args.Length > 0)
        {
            if (int.TryParse(args[0], out var partialIndex))
            {
                var partialIndexString = partialIndex.ToString();
                maps = maps.Where(map => map.Index.ToString().Contains(partialIndexString) || map.Name.ToLower().Contains(partialIndexString));
            }
            else
            {
                var partialName = args[0].ToLower();
                maps = maps.Where(map => map.Name.ToLower().Contains(partialName));
            }
        }

        if (!maps.Any())
        {
            Console.WriteLine("No matching maps found.");
            return;
        }

        foreach (var map in maps.OrderBy(map => map.Index))
        {
            Console.WriteLine($"{map.Index:000}: {map.Name}");
        }
    }

    private static void Teleport(G game, string[] args)
    {
        int mapIndex = ParseNumber(args[0], -1);

        if (mapIndex == -1)
        {
            Console.WriteLine("Invalid map index. Use 'maps'");
            return;
        }

        var mapSize = game.GetMapSize(mapIndex);

        if (mapSize == null)
        {
            Console.WriteLine($"Map {mapIndex} does not exist. Use 'maps'");
            return;
        }

        int x = args.Length < 2 ? G.Random(1, mapSize.Value.Width) : ParseNumber(args[1], -1);
        int y = args.Length < 3 ? G.Random(1, mapSize.Value.Height) : ParseNumber(args[2], -1);
        var direction = args.Length < 4 ? (Direction)G.Random(0, 3) : ParseEnum(args[3], Direction.Keep);

        if (x == -1)
        {
            Console.WriteLine("Invalid x value");
            return;
        }

        if (y == -1)
        {
            Console.WriteLine("Invalid y value");
            return;
        }

        if (direction == Direction.Keep)
        {
            Console.WriteLine("Invalid direction, using 'keep'");
        }

        var error = game.ScriptTeleport(mapIndex, x, y, direction);

        if (error != null)
            Console.WriteLine(error);
    }

    private static void Where(G game, string[] args)
    {
        var (position, mapIndex, direction) = game.GetCurrentLocation();
        var mapName = game.GetMaps().FirstOrDefault(map => map.Index == mapIndex).Name;

        Console.WriteLine($"At {1 + position.X}, {1 + position.Y} looking {direction} on map {mapIndex:000} ({mapName})");
    }

    private static void Events(G game, string[] args)
    {
        int mapIndex = args.Length == 0 ? game.GetCurrentLocation().MapIndex : ParseNumber(args[0], -1);
        var events = game.ScriptDescribeMapEvents(mapIndex);

        if (events == null)
        {
            Console.WriteLine($"Map {mapIndex} does not exist. Use 'maps'");
            return;
        }

        if (events.Count == 0)
            Console.WriteLine($"Map {mapIndex} has no events.");

        foreach (var description in events)
            Console.WriteLine(description);
    }

    private static IEnumerable<(uint Index, string Name)> FindItems(G game, string partialName) =>
        game.GetItems().Where(item => item.Name.Contains(partialName, StringComparison.OrdinalIgnoreCase)).OrderBy(item => item.Index);

    private static void Items(G game, string[] args)
    {
        var items = args.Length == 0 ? game.GetItems().OrderBy(item => item.Index) : FindItems(game, string.Join(" ", args));

        if (!items.Any())
        {
            Console.WriteLine("No matching items found.");
            return;
        }

        foreach (var (index, name) in items)
            Console.WriteLine($"{index:000}: {name}");
    }

    private static void Item(G game, string[] args)
    {
        uint itemIndex;

        if (uint.TryParse(args[0], out var parsedIndex))
        {
            itemIndex = parsedIndex;
        }
        else
        {
            var items = FindItems(game, args[0]).ToList();
            var exactMatches = items.Where(item => item.Name.Equals(args[0], StringComparison.OrdinalIgnoreCase)).ToList();

            if (exactMatches.Count != 0)
                items = exactMatches;

            if (items.Count != 1)
            {
                Console.WriteLine(items.Count == 0 ? "No matching items found." : "Multiple items match. Use the item index:");
                items.ForEach(item => Console.WriteLine($"{item.Index:000}: {item.Name}"));
                return;
            }

            itemIndex = items[0].Index;
        }

        int count = args.Length > 1 ? ParseNumber(args[1], -1) : 1;

        if (count < 1 || count > 99)
        {
            Console.WriteLine("Invalid value for parameter count. Provide a valid number in range 1 to 99.");
            return;
        }

        int slot = args.Length > 2 ? ParseNumber(args[2], -1) : game.ScriptActivePartyMember;
        var error = game.ScriptGiveItem(slot, itemIndex, count);

        Console.WriteLine(error ?? $"Gave {count} x item {itemIndex} to the party member in slot {slot}");
    }

    private static void NoClip(G game, string[] args)
    {
        game.NoClip = args.Length == 0 ? !game.NoClip : ParseNumber(args[0], 1) != 0;

        Console.WriteLine(game.NoClip ? "You can now move through walls." : "You can no longer move through walls.");
    }

    private static IEnumerable<(int CharacterIndex, string Name, bool InParty)> FindPartyMembers(G game, string partialName) =>
        game.GetPossiblePartyMembers().Where(member => member.Name.Contains(partialName, StringComparison.OrdinalIgnoreCase));

    private static void Members(G game, string[] args)
    {
        var members = args.Length == 0 ? game.GetPossiblePartyMembers() : FindPartyMembers(game, string.Join(" ", args));

        if (!members.Any())
        {
            Console.WriteLine("No matching characters found.");
            return;
        }

        foreach (var (index, name, inParty) in members)
            Console.WriteLine($"{index:000}: {name}{(inParty ? " (in the party)" : "")}");
    }

    private static void Invite(G game, string[] args)
    {
        int characterIndex;

        if (int.TryParse(args[0], out var parsedIndex))
        {
            characterIndex = parsedIndex;
        }
        else
        {
            // characters who are already in the party don't count (e.g. the hero with the same name)
            var members = FindPartyMembers(game, args[0]).Where(member => !member.InParty).ToList();
            var exactMatches = members.Where(member => member.Name.Equals(args[0], StringComparison.OrdinalIgnoreCase)).ToList();

            if (exactMatches.Count != 0)
                members = exactMatches;

            if (members.Count != 1)
            {
                Console.WriteLine(members.Count == 0 ? "No matching characters outside of the party found." : "Multiple characters match. Use the character index:");
                members.ForEach(member => Console.WriteLine($"{member.CharacterIndex:000}: {member.Name}"));
                return;
            }

            characterIndex = members[0].CharacterIndex;
        }

        var error = game.InvitePartyMember(characterIndex);
        var name = game.GetPossiblePartyMembers().FirstOrDefault(member => member.CharacterIndex == characterIndex).Name;

        Console.WriteLine(error ?? $"{name} joined the party.");
    }

    private static void Gold(G game, string[] args)
    {
        if (!int.TryParse(args[0], out int amount) || amount < 0 || amount > short.MaxValue)
        {
            Console.WriteLine($"Invalid value for parameter amount. Provide a valid number in range 0 to {short.MaxValue}.");
            return;
        }

        if (amount == 0)
            return;

        int remaining = game.DistributeGold(amount);

        game.RefreshShownInventory();

        if (remaining == 0)
            Console.WriteLine($"Distributed {amount} gold");
        else
            Console.WriteLine($"Distributed {amount - remaining} of {amount} gold");
    }

    private static void Food(G game, string[] args)
    {
        if (!int.TryParse(args[0], out int amount) || amount < 0 || amount > short.MaxValue)
        {
            Console.WriteLine($"Invalid value for parameter amount. Provide a valid number in range 0 to {short.MaxValue}.");
            return;
        }

        if (amount == 0)
            return;

        int remaining = game.DistributeFood(amount);

        game.RefreshShownInventory();

        if (remaining == 0)
            Console.WriteLine($"Distributed {amount} food");
        else
            Console.WriteLine($"Distributed {amount - remaining} of {amount} food");
    }
}
