using System.Collections.Generic;

namespace EquipmentModelSimulation
{
    class SimulationHistory
    {
        public List<System.DateTime> Timestamps = new List<System.DateTime>();

        public List<double> PumpPower = new List<double>();
        public List<short> PumpIsPowered = new List<short>();

        public List<double> SourceTankLevel = new List<double>();

        public List<double> TargetTankLevel = new List<double>();

        public List<double> FlowbackPipeFlow = new List<double>();

        public List<double> PipeWithPumpFlow = new List<double>();

        public List<double> VariableA = new List<double>();
        public List<double> VariableB = new List<double>();
    }
}
