
using System.Text.RegularExpressions;
using NDesk.Options;

namespace EquipmentModelSimulation
{
    class Arguments
    {
        private static OptionSet optionSet;

        public static bool ShowHelp = false;
        public static int NumberOfSites { get; set; } = 1;
        public static string Host { get; set; }
        public static string Username { get; set; }
        public static string Password { get; set; }
        public static string ToplevelHierarchyPrefix { get; set; } = "Example site";
        public static double WriteToFutureSeconds { get; set; } = 0;
        public static int PopulateHistoryLength { get; set; } = 30 * 24 * 60 * 60;
        public static double DataPointsPerSecond { get; set; } = 1;

        public static void ParseArgs(string[] args)
        {
            optionSet = new OptionSet()
            {
                { "?|help", "Help", v => ShowHelp = v != null },
                { "s|sites=", "Number of sites", v => NumberOfSites = int.Parse(v) },
                { "h|host=", "Connection string for the History system", v => Host = v },
                { "u|user=", "Name of the user", v => Username = v },
                { "p|password=", "Password", v => Password = v },
                { "t|topLevelPrefix=", "Name of the top-most hierarchy path for the equipment model instances", v => ToplevelHierarchyPrefix = v },
                { "f|writeToFuture=", "Advance current value writing to the future by this amount (seconds)", v => WriteToFutureSeconds = double.Parse(v) },
                { "l|length=", "Length of history (examples: 5s, 5m, 5h, or 5d) Default: 30d", v =>
                    {
                        var mc = Regex.Matches(v, @"^(\d+)([smhd])$");

                        if (mc.Count == 1 && mc[0].Groups.Count == 3)
                        {
                            var match = mc[0];
                            var amount = int.Parse(match.Groups[1].ToString());
                            var unit = match.Groups[2].ToString();

                            switch (unit)
                            {
                                case "s":
                                    PopulateHistoryLength = amount;
                                    break;
                                case "m":
                                    PopulateHistoryLength = amount * 60;
                                    break;
                                case "h":
                                    PopulateHistoryLength = amount * 60 * 60;
                                    break;
                                case "d":
                                    PopulateHistoryLength = amount * 24 * 60 * 60;
                                    break;
                            }
                        }
                        else {
                            throw new System.ArgumentException($"Invalid value for parameter -l, --length: '{v}'");
                        }
                    } },
                { "d|dataPointsPerSecond=", "How many data points to write per second. Default: 1", v => DataPointsPerSecond = double.Parse(v) }
            };

            optionSet.Parse(args);

            if (Host == null)
            {
                throw new System.ArgumentException("Define hostname using -h <hostname>");
            }
            if (Username == null)
            {
                throw new System.ArgumentException("Define username using -u <username>");
            }
            if (Password == null)
            {
                throw new System.ArgumentException("Define password using -p <password>");
            }
        }

        public static void PrintHelp()
        {
            optionSet.WriteOptionDescriptions(System.Console.Out);
        }
    }
}
