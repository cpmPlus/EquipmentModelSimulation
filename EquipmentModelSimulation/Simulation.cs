namespace EquipmentModelSimulation
{
    class Simulation
    {
        // Variables
        public System.DateTime SimulateFrom;
        public System.DateTime SimulateTime;
        public bool CurrentTimeReached = false;

        private static readonly double PUMP_NOMINAL_POWER = 1000;      // Watts
        private static readonly double PUMP_MAX_FLOW = 1000;           // Liters per minute
        private static readonly double FLOWBACK_PIPE_FLOW = 100;       // Liters per minute
        private static readonly double TANK_VOLUME = 1000;             // Liters

        /*
         * Pump is pumping water from the tank A
         * to the tank B. Water is freely flowing
         * back from tank B to tank A using the 
         * flowback pipe.
         * 
         * |       | >>>>> pump >>>>> |       |
         * | tankA |                  | tankB |
         * |_______| <<< flowback <<< |_______|
         */

        // Equipment
        public Tank TankA;
        public Tank TankB;
        public Pipe FlowbackPipe;
        public Pipe PipeWithPump;
        public Pump Pump;

        public Simulation(System.DateTime simulateFrom)
        {
            SimulateFrom = simulateFrom;
            SimulateTime = simulateFrom;

            // Create two tanks
            TankA = new Tank("Tank A", maximumLevel: TANK_VOLUME, currentLevel: TANK_VOLUME);
            TankB = new Tank("Tank B", maximumLevel: TANK_VOLUME, currentLevel: 0);

            // Create pump
            Pump = new Pump("Pump", nominalPower: PUMP_NOMINAL_POWER);

            // Create flowback pipe
            FlowbackPipe = new Pipe(
                "Flowback pipe",
                flow: FLOWBACK_PIPE_FLOW,
                sourceTank: TankB,
                targetTank: TankA);

            // Create the main pipe and attach the pump
            PipeWithPump = new Pipe(
                "Pipe with pump",
                flow: PUMP_MAX_FLOW,
                sourceTank: TankA,
                targetTank: TankB,
                pump: Pump);
        }

        public System.TimeSpan GetElapsedTime()
        {
            return SimulateTime.Subtract(SimulateFrom);
        }

        public bool IsRealTime()
        {
            return SimulateTime >= System.DateTime.UtcNow;
        }

        public void RunLoop(double timeStep)
        {
            // Make actions
            FlowbackPipe.Act(timeStep: timeStep);
            PipeWithPump.Act(timeStep: timeStep);

            // Turn pump off if tank has more than 900 liters of water
            if (TankA.Level.CurrentValue > 900 && !Pump.IsRunning)
                Pump.IsRunning = true;

            // Turn pump on if tank has less than 100 liters of water
            if (TankA.Level.CurrentValue < 100 && Pump.IsRunning)
                Pump.IsRunning = false;

            // Case: Simulating current moment
            if (CurrentTimeReached) {
                SimulateTime = System.DateTime.UtcNow;
            }
            // Case: Still calculating history
            else
            {
                SimulateTime = SimulateTime.AddSeconds(timeStep);

                // Check if history calculation was finished
                if (SimulateTime >= System.DateTime.UtcNow)
                    CurrentTimeReached = true;
            }
        }
    }
}
