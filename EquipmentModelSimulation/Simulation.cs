using System;

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
         * Pump is pumping water from the source tank
         * to the target tank. Water is freely flowing
         * back from target tank to source tank using the 
         * flowback pipe.
         * 
         * | source | >>>>> pump >>>>> | target |
         * |  tank  |                  |  tank  |
         * |________| <<< flowback <<< |________|
         */

        // Equipment
        public Tank SourceTank;
        public Tank TargetTank;
        public Pipe FlowbackPipe;
        public Pipe PipeWithPump;
        public Pump Pump;

        public equipment.Property VariableA;
        public equipment.Property VariableB;

        public SimulationHistory History = new SimulationHistory();

        public Simulation(System.DateTime simulateFrom)
        {
            SimulateFrom = simulateFrom;
            SimulateTime = simulateFrom;

            // Create two tanks
            SourceTank = new Tank("Source tank", maximumLevel: TANK_VOLUME, currentLevel: TANK_VOLUME);
            TargetTank = new Tank("Target tank", maximumLevel: TANK_VOLUME, currentLevel: 0);

            // Create pump
            Pump = new Pump("Pump", nominalPower: PUMP_NOMINAL_POWER);

            // Create flowback pipe
            FlowbackPipe = new Pipe(
                "Flowback pipe",
                flow: FLOWBACK_PIPE_FLOW,
                sourceTank: TargetTank,
                targetTank: SourceTank);

            // Create the main pipe and attach the pump
            PipeWithPump = new Pipe(
                "Pipe with pump",
                flow: PUMP_MAX_FLOW,
                sourceTank: SourceTank,
                targetTank: TargetTank,
                pump: Pump);

            VariableA = new equipment.Property(0, 1);
            VariableB = new equipment.Property(0, 1);
        }

        public System.TimeSpan GetElapsedTime()
        {
            return SimulateTime.Subtract(SimulateFrom);
        }

        public bool IsRealTime()
        {
            return SimulateTime >= System.DateTime.UtcNow;
        }

        public DateTime GetNextPointTime(double timeStep)
        {
            return SimulateTime.AddSeconds(timeStep);
        }

        public bool IsNextStepAfter(DateTime moment, double timeStep)
        {
            var next = GetNextPointTime(timeStep);

            return moment.CompareTo(next) < 0;
        }

        private void addCurrentValuesToHistory()
        {
            History.Timestamps.Add(SimulateTime);

            History.PumpPower.Add(Pump.Power.CurrentValue);
            History.PumpIsPowered.Add(Pump.IsPoweredInt);

            History.SourceTankLevel.Add(SourceTank.Level.CurrentValue);

            History.TargetTankLevel.Add(TargetTank.Level.CurrentValue);

            History.FlowbackPipeFlow.Add(FlowbackPipe.Flow.CurrentValue);

            History.PipeWithPumpFlow.Add(PipeWithPump.Flow.CurrentValue);

            History.VariableA.Add(VariableA.CurrentValue);
            History.VariableB.Add(VariableB.CurrentValue);
        }

        public void RunLoop(double timeStep)
        {
            // Make actions
            FlowbackPipe.Act(timeStep: timeStep);
            PipeWithPump.Act(timeStep: timeStep);

            // Turn pump off if tank has more than 900 liters of water
            if (SourceTank.Level.CurrentValue > 900 && !Pump.IsPowered)
                Pump.IsPowered = true;

            // Turn pump on if tank has less than 100 liters of water
            if (SourceTank.Level.CurrentValue < 100 && Pump.IsPowered)
                Pump.IsPowered = false;

            VariableA.CurrentValue = Math.Sin((SimulateTime - SimulateFrom).TotalSeconds / 3) / 2 + 0.5;
            VariableB.CurrentValue = Math.Sin((SimulateTime - SimulateFrom).TotalSeconds / 15) / 2 + 0.5;

            SimulateTime = SimulateTime.AddSeconds(timeStep);

            // Case: Still calculating history
            if (!CurrentTimeReached) {
                addCurrentValuesToHistory();

                // Check if history calculation was finished
                if (SimulateTime >= System.DateTime.UtcNow)
                    CurrentTimeReached = true;
            }
        }
    }
}
