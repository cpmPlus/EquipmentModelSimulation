namespace EquipmentModelSimulation
{
    class ConsoleGUI
    {
        public static readonly int PROGRESS_LENGTH = 60;
        public static readonly int MAXIMUM_ROW_LENGTH = 100;

        public void Log(string message = "", bool lineBreak = true, bool? endPadding = null)
        {
            var pad = System.Console.CursorLeft == 0 ? "   " : "";
            var end = lineBreak ? "\n" : "";
            var row = $"{pad}{message}";

            var endOfRowLength = System.Math.Max(System.Console.WindowWidth - row.Length - 1, 0);
            var endPaddingStr = endPadding ?? lineBreak ? new string(' ', endOfRowLength) : "";

            System.Console.Write($"{row}{endPaddingStr}{end}");
        }

        public void GetPropertyProgress(string name, equipment.Property property)
        {
            // Get progress bar and name with some padding
            var bar = GetProgressBar(property.MinValue, property.MaxValue, property.CurrentValue);
            name += new string(' ', 20 - name.Length);
            Log($"{name} {bar}    {property.GetFormattedValue()}");
        }

        public string GetProgressBar(double min, double max, double current)
        {
            // Get progress
            double progress = (max - min) / (current - min);

            int barCount = (int)System.Math.Round(PROGRESS_LENGTH / progress);
            int emptyCount = PROGRESS_LENGTH - barCount;

            // Return progress bar
            return $"«{new string('■', barCount)}{new string('·', emptyCount)}»";
        }

        public void LogElapsedTime(Simulation simulation)
        {
            var msg = "Simulation time elapsed";

            var timeSpan = simulation.GetElapsedTime();

            if (timeSpan.Days > 0)
                msg += $" {timeSpan.Days} days,";

            if (timeSpan.Hours > 0)
                msg += $" {timeSpan.Hours} hours,";

            if (timeSpan.Minutes > 0)
                msg += $" {timeSpan.Minutes} minutes,";

            msg += $" {timeSpan.Seconds} seconds";

            Log(msg);
        }

        public void Clear()
        {
            System.Console.Clear();
        }

        public void DrawSystem(Simulation simulation)
        {
            // Clean the canvas
            System.Console.SetCursorPosition(0, 0);

            Log();
            Log("Example Site - Real time statistics");
            Log();

            GetPropertyProgress("Pump power", simulation.Pump.Power);
            GetPropertyProgress("Pump temperature", simulation.Pump.Temperature);

            Log();
            GetPropertyProgress("Source tank level", simulation.SourceTank.Level);
            GetPropertyProgress("Target tank level", simulation.TargetTank.Level);

            Log();
            GetPropertyProgress("Flowback pipe flow", simulation.FlowbackPipe.Flow);
            GetPropertyProgress("Motorized pipe flow", simulation.PipeWithPump.Flow);

            Log();
            GetPropertyProgress("Variable A", simulation.VariableA);
            GetPropertyProgress("Variable B", simulation.VariableB);

            Log();
            LogElapsedTime(simulation);
        }
    }
}
