namespace EquipmentModelSimulation
{
    class Program
    {
        private static readonly double DATA_POINTS_PER_SECOND = 100;          // Times per second
        private static readonly int POPULATED_HISTORY_LENGTH = 0;            // Seconds (2592000 s = 30 days)
        private static readonly int MAX_RENDERS_PER_SECOND = 5;

        private static void Main()
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

            while (true)
            {
                loopStart = System.DateTime.UtcNow;

                // Run one simulation loop
                simulation.RunLoop(timeStep);

                // Case: History calculation finished
                if (simulation.CurrentTimeReached)
                {
                    // Case: Enough time has been passed since the last render
                    // > Render again
                    if (System.DateTime.UtcNow.Subtract(lastRender).TotalMilliseconds > 1000 / MAX_RENDERS_PER_SECOND)
                    {
                        gui.DrawSystem(simulation);
                        lastRender = System.DateTime.UtcNow;
                    }

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
