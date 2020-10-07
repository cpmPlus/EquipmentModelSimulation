using NDesk.Options;

namespace EquipmentModelSimulation
{
    class Program
    {
        private static readonly double DATA_POINTS_PER_SECOND = 5;          // Times per second
        private static readonly int POPULATED_HISTORY_LENGTH = 1 * 24 * 60 * 60;            // Seconds (2592000 s = 30 days)
        private static readonly int MAX_RENDERS_PER_SECOND = 5;

        private static void Main(string[] args)
        {
            // Initialize the interface
            ConsoleGUI gui = new ConsoleGUI();

            gui.Log();
            gui.Log("Welcome to the Equipment Model simulation!");
            gui.Log("------------------------------------------");
            gui.Log();

            // Calculate the length of a singe time step
            double timeStep = 1.0 / DATA_POINTS_PER_SECOND;

            gui.Clear();
            gui.Log();
            gui.Log("Generating simulated history data...");

            Simulation simulation = new Simulation(
                simulateFrom: System.DateTime.UtcNow.AddSeconds(-POPULATED_HISTORY_LENGTH));

            // This variable stores time of the last render
            System.DateTime lastRender = System.DateTime.MinValue;
            System.DateTime loopStart;

            int numberOfSites = 1;
            string RTDBHost = "wss://10.58.44.108/history";
            string RTDBUsername = ".\\testmaindbadmin";
            string RTDBPassword = "fM5Yhv76Of0Z*O";
            string toplevelHierarchyPrefix = "Example site";

            var p = new OptionSet()
            {
                { "s|sites=", "Number of sites", v => numberOfSites = int.Parse(v) },
                { "h|host=", "Hostname", v => RTDBHost = v },
                { "u|user=", "", v => RTDBUsername = v },
                { "p|password=", "", v => RTDBPassword = v },
                { "t|topLevelPrefix=", "", v => toplevelHierarchyPrefix = v },
            };

            p.Parse(args);

            using (DBConnection dbConnection = new DBConnection(numberOfSites, RTDBHost, RTDBUsername, RTDBPassword, toplevelHierarchyPrefix))
            {
                dbConnection.ConnectOrThrow();

                bool simulationHistoryDataWritten = false;

                // Clear all previous history data for the equipment properties
                dbConnection.ClearHistory();

                while (true)
                {
                    loopStart = System.DateTime.UtcNow;

                    // Run one simulation loop
                    simulation.RunLoop(timeStep);

                    // Case: History calculation finished
                    if (simulation.CurrentTimeReached)
                    {
                        // Case: History data has not been written to the database
                        if (!simulationHistoryDataWritten)
                        {
                            dbConnection.WriteSimulationHistoryData(simulation.History);
                            simulationHistoryDataWritten = true;
                        }

                        // Case: Enough time has been passed since the last render
                        // > Render again
                        if (System.DateTime.UtcNow.Subtract(lastRender).TotalMilliseconds > 1000 / MAX_RENDERS_PER_SECOND)
                        {
                            gui.DrawSystem(simulation);
                            lastRender = System.DateTime.UtcNow;
                        }

                        // Update the current values in the database to match the simulation values
                        dbConnection.WriteSimulationCurrentValues(simulation);

                        // Get loop duration
                        var loopDuration = (System.DateTime.UtcNow - loopStart).TotalMilliseconds;

                        // Sleep for a while, max out to zero
                        var sleepTime = System.Math.Max(timeStep * 1000 - loopDuration, 0);

                        System.Threading.Thread.Sleep((int)sleepTime);
                    }
                }
            }
        }
    }
}
