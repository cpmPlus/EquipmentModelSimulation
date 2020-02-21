namespace EquipmentModelSimulation
{
    class Bearing : Equipment
    {
        private readonly double MAX_VIBRATION_VELOCITY = 2.7;  // mm / s
        private readonly double Phase;

        private equipment.Property RPM;
        public equipment.Property Vibration;

        // Keep track of the angle
        // > 0 = 0 deg, 1 = 360 deg
        private double rotation = 0;

        public Bearing(string name, equipment.Property rpmProperty, double phase = 0) : base(name)
        {
            Phase = phase * System.Math.PI;

            RPM = rpmProperty;
            Vibration = new equipment.Property(
                minValue: 0,
                maxValue: MAX_VIBRATION_VELOCITY,
                unit: "mm / s");
        }

        // Vibration profile per one rotation
        public double GetCurrentVibration(double rotation, double percent) =>
            (1.0 - System.Math.Abs(System.Math.Cos(Phase + rotation * 2.0 * System.Math.PI))) * MAX_VIBRATION_VELOCITY * percent;

        public void Act(double timeStep)
        {

            // Skip any calculation if pump is not even running
            if (RPM.CurrentValue == 0)
                Vibration.CurrentValue = 0;
            else
            {
                // Update vibration
                Vibration.CurrentValue = GetCurrentVibration(rotation, RPM.GetPercent());

                // Keep track of the angle
                double rotationPerSecond = RPM.CurrentValue / 60.0;
                rotation += rotationPerSecond * timeStep;
            }
        }
    }
}
