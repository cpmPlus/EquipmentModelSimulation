namespace EquipmentModelSimulation.equipment
{
    class Property
    {
        private readonly System.Collections.Generic.List<Property> linearDependents;
        private double historyLength = 0;

        public Property(
            double minValue,
            double maxValue,
            string unit = null)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            Unit = unit;

            linearDependents = new System.Collections.Generic.List<Property>();
        }

        public string Unit { get; set; }

        public double MaxValue { get; set; }

        public double MinValue { get; set; }

        private double currentValue;

        public double CurrentValue {
            get
            {
                return currentValue;
            }
            set
            {
                // Add one to counter
                historyLength++;

                // Cap value between min and max
                currentValue = System.Math.Max(value, MinValue);
                currentValue = System.Math.Min(currentValue, MaxValue);

                // Update Max
                if (Max == null || currentValue > Max)
                    Max = currentValue;

                // Update Min
                if (Min == null || currentValue < Min)
                    Min = currentValue;

                // Update Avg
                Avg += (currentValue - Avg) / historyLength;

                // Calculate linear dependents
                foreach (var dependent in linearDependents)
                    dependent.SetPercent(GetPercent());
            }
        }

        public double Min { get; set; }

        public double Max { get; set; }

        public double Avg { get; set; }

        public void AddLinearDependency(equipment.Property property)
        {
            linearDependents.Add(property);
        }

        public double GetPercent()
        {
            return (CurrentValue - MinValue) / (MaxValue - MinValue);
        }

        public void SetPercent(double percent)
        {
            CurrentValue = (MaxValue - MinValue) * percent + MinValue;
        }

        public string GetFormattedValue()
        {
            return $"{CurrentValue.ToString("0.00")} {Unit ?? ""}";
        }
    }
}
