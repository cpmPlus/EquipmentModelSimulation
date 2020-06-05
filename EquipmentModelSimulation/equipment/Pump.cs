using System;
using System.Windows.Media.Animation;

namespace EquipmentModelSimulation
{
    class Pump : Equipment
    {
        private readonly double PUMP_LAG = 6;    // Seconds
        private readonly double MAX_RPM = 1220;  // rpm
        private readonly int NUMBER_OF_BEARINGS = 10;

        public equipment.Property Power;
        public equipment.Property RPM;
        public equipment.Property Temperature;
        public System.Collections.Generic.List<Bearing> Bearings;

        public bool IsRunning { get; set; }

        public long IsRunningInt { get => IsRunning ? 1 : 16; } // 1 == Open, 16 == Closed

        public Pump(string name, double nominalPower) : base(name)
        {

            Bearings = new System.Collections.Generic.List<Bearing>();

            // Properties
            // ==========

            Power = new equipment.Property(
                minValue: 0,
                maxValue: nominalPower,
                unit: "W");

            RPM = new equipment.Property(
                minValue: 0.0,
                maxValue: MAX_RPM,
                unit: "rpm");

            Temperature = new equipment.Property(
                minValue: 18.0,
                maxValue: 95.0,
                unit: "C");

            Power.AddLinearDependency(RPM);

            // Create bearings
            // ===============

            var random = new System.Random();
            for (var index = 0; index < NUMBER_OF_BEARINGS; index++)
            {
                var bearing = new Bearing($"Bearing {index}", RPM, random.NextDouble());
                Bearings.Add(bearing);
            }
        }

        public double GetPowerMultiplier()
        {
            return this.Power.CurrentValue / this.Power.MaxValue;
        }

        public double GetTemperatureDelta(double timeStep)
        {
            // Pump is cooling down
            if (Power.CurrentValue == 0) return -(1.2 / 60.0 * timeStep);

            // Calculate additional heat
            // TODO: Clarify this magic
            var progress = 1 - Temperature.GetPercent();
            return 30.0 / 60.0 * progress * Power.GetPercent() * timeStep;
        }

        public void Act(double timeStep)
        {
            var step = (Power.MaxValue - Power.MinValue) / (PUMP_LAG / timeStep);

            if (IsRunning)
                Power.CurrentValue += step;

            else
                Power.CurrentValue -= step;

            Temperature.CurrentValue += GetTemperatureDelta(timeStep);

            foreach (var bearing in Bearings)
                bearing.Act(timeStep);
        }
    }
}
