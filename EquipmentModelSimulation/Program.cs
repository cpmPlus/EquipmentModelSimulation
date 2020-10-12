using System;
using NDesk.Options;

namespace EquipmentModelSimulation
{
    class Program
    {
        private static readonly double DATA_POINTS_PER_SECOND = 1;          // Times per second
        //private static readonly int POPULATED_HISTORY_LENGTH = 1 * 24 * 60 * 60;            // Seconds (2592000 s = 30 days)
        private static readonly int POPULATED_HISTORY_LENGTH = 1  * 60;            // Seconds (2592000 s = 30 days)
        private static readonly int MAX_RENDERS_PER_SECOND = 5;

        private static void Main(string[] args)
        {
            int numberOfSites = 1;
            string RTDBHost = null;
            string RTDBUsername = null;
            string RTDBPassword = null;
            string toplevelHierarchyPrefix = "Example site";
            double writeToFutureSeconds = 0;

            var p = new OptionSet()
            {
                { "s|sites=", "Number of sites", v => numberOfSites = int.Parse(v) },
                { "h|host=", "Hostname", v => RTDBHost = v },
                { "u|user=", "", v => RTDBUsername = v },
                { "p|password=", "", v => RTDBPassword = v },
                { "t|topLevelPrefix=", "", v => toplevelHierarchyPrefix = v },
                { "f|writeToFuture=", "", v => writeToFutureSeconds = double.Parse(v) },
            };

            p.Parse(args);

            if (RTDBHost == null)
            {
                throw new System.ArgumentException("Define hostname using -h <hostname>");
            }
            if (RTDBUsername == null)
            {
                throw new System.ArgumentException("Define username using -u <username>");
            }
            if (RTDBPassword == null)
            {
                throw new System.ArgumentException("Define password using -p <password>");
            }

            // Initialize the interface
            ConsoleGUI gui = new ConsoleGUI();

            gui.Log();
            gui.Log("Welcome to the Equipment Model simulation!");
            gui.Log("------------------------------------------");
            gui.Log();

            // Calculate the length of a singe time step
            double timeStep = 1.0 / DATA_POINTS_PER_SECOND;

            Simulation simulation = new Simulation(
                simulateFrom: DateTime.UtcNow.AddSeconds(-POPULATED_HISTORY_LENGTH));

            // This variable stores time of the last render
            DateTime lastRender = DateTime.MinValue;
            DateTime loopStart;

            gui.Log();
            Console.Write("Connecting to the database...", false);

            using (DBConnection dbConnection = new DBConnection(numberOfSites, RTDBHost, RTDBUsername, RTDBPassword, toplevelHierarchyPrefix))
            {
                dbConnection.ConnectOrThrow();
                Console.WriteLine("Done");

                bool simulationHistoryDataWritten = false;

                Console.Write("Clearing previous history values...", false);

                // Clear all previous history data for the equipment properties
                dbConnection.ClearHistory();
                Console.WriteLine("Done");

                Console.Write("Generating history data...", false);

                while (true)
                {
                    loopStart = DateTime.UtcNow;

                    // Run one simulation loop
                    simulation.RunLoop(timeStep);

                    // Case: History calculation finished
                    if (simulation.CurrentTimeReached)
                    {
                        // Case: History data has not been written to the database
                        if (!simulationHistoryDataWritten)
                        {
                            Console.WriteLine("Done");

                            Console.Write("Writing history data...", false);

                            dbConnection.WriteSimulationHistoryData(simulation.History);
                            simulationHistoryDataWritten = true;

                            Console.WriteLine("Done");
                        }

                        // Case: Enough time has been passed since the last render
                        // > Render again
                        if (DateTime.UtcNow.Subtract(lastRender).TotalMilliseconds > 1000 / MAX_RENDERS_PER_SECOND)
                        {
                            gui.DrawSystem(simulation);
                            lastRender = DateTime.UtcNow;
                        }

                        // Update the current values in the database to match the simulation values
                        dbConnection.WriteSimulationCurrentValues(simulation);

                        var nextPointInFuture = simulation.IsNextStepAfter(DateTime.UtcNow, timeStep, writeToFutureSeconds);

                        if (nextPointInFuture)
                        {
                            // Get loop duration
                            var loopDuration = (DateTime.UtcNow - loopStart).TotalMilliseconds;

                            // Sleep for a while, max out to zero
                            var sleepTime = System.Math.Max(timeStep * 1000 - loopDuration, 0);

                            System.Threading.Thread.Sleep((int)sleepTime);
                        }
                    }
                }
            }
        }
    }
}
