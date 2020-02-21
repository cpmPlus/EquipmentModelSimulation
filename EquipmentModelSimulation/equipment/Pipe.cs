namespace EquipmentModelSimulation
{

    class Pipe: Equipment
    {
        // Properties
        private Tank sourceTank;
        private Tank targetTank;
        private Pump pump;

        public equipment.Property Flow;

        public Pipe(string name, double flow, Tank sourceTank, Tank targetTank, Pump pump = null)
            : base(name)
        {

            Flow = new equipment.Property(minValue: 0, maxValue: flow, unit: "l / min")
            {
                CurrentValue = flow
            };

            this.sourceTank = sourceTank;
            this.targetTank = targetTank;
            this.pump = pump;
        }

        private double FlowPerMinuteToTimeStep(double flow, double timeStep)
        {
            double flowPerSecond = (flow / 60);
            double flowPerTimeStep = flowPerSecond * timeStep;

            return flowPerTimeStep;
        }

        private double FlowPerTimeStepToMinute(double flow, double timeStep)
        {
            double flowPerSecond = flow / timeStep;
            double flowPerMinute = flowPerSecond * 60;

            return flowPerMinute;
        }

        public void Act(double timeStep)
        {

            double amount;
            double flowPerTimeStep;

            // Case: Pipe has built-in pump
            // > Pump defines the flow speed
            if (this.pump != null)
            {
                this.pump.Act(timeStep);

                flowPerTimeStep = FlowPerMinuteToTimeStep(Flow.MaxValue, timeStep);
                flowPerTimeStep *= this.pump.RPM.GetPercent();

                amount = sourceTank.MoveWaterInto(targetTank, flowPerTimeStep);
            }

            // Case: No built-in pump
            // > Use constant flow
            else
            {
                flowPerTimeStep = FlowPerMinuteToTimeStep(Flow.MaxValue, timeStep);
                amount = sourceTank.MoveWaterInto(targetTank, flowPerTimeStep);
            }

            // Update per minute flow using time step specific flow amount
            Flow.CurrentValue = FlowPerTimeStepToMinute(amount, timeStep);
        }
    }
}
