using System;

namespace EquipmentModelSimulation
{
    class Tank: Equipment
    {
        public equipment.Property Level;

        public Tank(string name, double maximumLevel, double currentLevel = 0)
            : base(name)
        {
            Level = new equipment.Property(
                minValue: 0,
                maxValue: maximumLevel,
                unit: "l");
            Level.CurrentValue = currentLevel;
        }

        public double TakeWater(double amount)
        {
            // No negative amounts, or amounts larger
            // than tank current water level
            amount = Math.Max(amount, 0);
            amount = Math.Min(amount, Level.CurrentValue);

            // Update current amount
            Level.CurrentValue -= amount;

            return amount;
        }

        public void AddWater(double amount)
        {
            // No negative amounts
            amount = Math.Max(amount, 0);

            // Add water, make sure not to overfill. Note that
            // excessive water will be spilled out of the system.
            Level.CurrentValue = Math.Min(Level.CurrentValue + amount, Level.MaxValue);
        }

        public double MoveWaterInto(Tank anotherTank, double amount)
        {
            // Move water from tank to tank
            amount = TakeWater(amount);
            anotherTank.AddWater(amount);

            // Return how much water were moved
            return amount;
        }
    }
}
