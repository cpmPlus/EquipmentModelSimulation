﻿using System.Collections.Generic;

namespace EquipmentModelSimulation
{
    class SimulationHistory
    {
        public List<System.DateTime> Timestamps = new List<System.DateTime>();

        public List<double> PumpPower = new List<double>();
        public List<bool> PumpIsRunning = new List<bool>();

        public List<double> SourceTankLevel = new List<double>();

        public List<double> TargetTankLevel = new List<double>();

        public List<double> FlowbackPipeFlow = new List<double>();

        public List<double> PipeWithPumpFlow = new List<double>();
    }
}