
using NDesk.Options;

namespace EquipmentModelSimulation
{
    class Arguments
    {
        public static int NumberOfSites { get; set; } = 1;
        public static string Host { get; set; }
        public static string Username { get; set; }
        public static string Password { get; set; }
        public static string ToplevelHierarchyPrefix { get; set; } = "Example site";
        public static double WriteToFutureSeconds { get; set; } = 0;

        public static void ParseArgs(string[] args)
        {
            var p = new OptionSet()
            {
                { "s|sites=", "Number of sites", v => NumberOfSites = int.Parse(v) },
                { "h|host=", "Hostname", v => Host = v },
                { "u|user=", "", v => Username = v },
                { "p|password=", "", v => Password = v },
                { "t|topLevelPrefix=", "", v => ToplevelHierarchyPrefix = v },
                { "f|writeToFuture=", "", v => WriteToFutureSeconds = double.Parse(v) },
            };

            p.Parse(args);

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
    }
}
