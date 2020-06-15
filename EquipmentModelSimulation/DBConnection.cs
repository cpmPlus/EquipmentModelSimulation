using System;
using ABB.Vtrin.Drivers;
using ABB.Vtrin;
using ABB.Vtrin.Util;
using System.Collections.Generic;
using System.Linq;

namespace EquipmentModelSimulation
{
    class DBConnection : IDisposable
    {
        private readonly cDataLoader dataloader = new cDataLoader();
        private cDriverSkeleton RTDBDriver;

        private static readonly string RTDBHost = "wss://localhost/history";
        private static readonly string RTDBUsername = "username";
        private static readonly string RTDBPassword = "password";

        private cDbEnumerationMember binaryTextOpen;
        private cDbEnumerationMember binaryTextClosed;

        public void ConnectOrThrow()
        {
            // Set up a memory stream to catch exceptions
            using (System.IO.MemoryStream memoryStream = new System.IO.MemoryStream())
            {
                var listener = new System.Diagnostics.TextWriterTraceListener(memoryStream, "connectlistener");
                System.Diagnostics.Trace.Listeners.Add(listener);

                // Set connection options
                dataloader.ConnectOptions =
                    ABB.Vtrin.cDataLoader.cConnectOptions.AcceptNewServerKeys
                    | ABB.Vtrin.cDataLoader.cConnectOptions.AcceptServerKeyChanges;

                // Initialize the database driver
#pragma warning disable CS0618 // Type or member is obsolete
                RTDBDriver = dataloader.Connect(
                    RTDBHost,
                    RTDBUsername,
                    RTDBPassword,
                    false);
#pragma warning restore CS0618 // Type or member is obsolete

                // Unbind the connect listener
                System.Diagnostics.Trace.Listeners.Remove("connectlistener");

                // Case: driver is null, something went wrong
                // > throw an error
                if (RTDBDriver == null)
                {
                    // Read stack trace from the memorystream buffer
                    string msg = System.Text.Encoding.UTF8.GetString(memoryStream.GetBuffer());
                    throw new System.ApplicationException(msg);
                }

                cDbEnumeration binaryTextEnum = RTDBDriver.Enumerations.GetInstanceByName("Binary Text(6)");
                binaryTextOn = binaryTextEnum.Cast<cDbEnumerationMember>().Where(a => a.Text == "On").First();
                binaryTextOff = binaryTextEnum.Cast<cDbEnumerationMember>().Where(a => a.Text == "Off").First();
            }
        }

        public void Dispose()
        {
            if (dataloader != null)
                dataloader.Dispose();
        }

        private void writePropertyHistoryData<T>(string path, string property, List<System.DateTime> timestamps, List<T> values)
        {
            cGraphFetchParameters parameters = cGraphFetchParameters.CreateScalarFetch(
                RTDBDriver.Classes["EquipmentHistory"],
                DateTime.UtcNow,
                "Path=? AND Property=?",
                path,
                property);

            var gdi = RTDBDriver.GetGraphDataIterator(parameters);

            for (var i = 0; i < timestamps.Count; i++)
            {
                gdi.Write(timestamps[i], values[i], cValueStatus.OK);
            }

            gdi.Close();
        }

        public void WriteSimulationHistoryData(SimulationHistory history)
        {
            writePropertyHistoryData("Example site.Pump section.Pump", "Power", history.Timestamps, history.PumpPower);
            writePropertyHistoryData("Example site.Pump section.Pump", "Power state", history.Timestamps, history.PumpIsPowered);

            writePropertyHistoryData("Example site.Tank area.Source tank", "Level", history.Timestamps, history.SourceTankLevel);

            writePropertyHistoryData("Example site.Tank area.Target tank", "Level", history.Timestamps, history.TargetTankLevel);

            writePropertyHistoryData("Example site.Pipe", "Flow", history.Timestamps, history.PipeWithPumpFlow);

            writePropertyHistoryData("Example site.Flowback pipe", "Flow", history.Timestamps, history.FlowbackPipeFlow);
        }

        private void clearPropertyHistory(string path, string property)
        {
            cGraphFetchParameters parameters = cGraphFetchParameters.CreateRawFetch(
                RTDBDriver.Classes["EquipmentHistory"],
                DateTime.UtcNow.AddDays(-1000),
                DateTime.UtcNow,
                int.MaxValue - 1,
                "Path=? AND Property=?",
                path,
                property);

            var gdi = RTDBDriver.GetGraphDataIterator(parameters);

            gdi.DeleteRange();

            gdi.Close();
        }

        private void writePropertyCurrentValue<T>(string path, string property, T value)
        {
            var instance = RTDBDriver
                        .Classes["Path"]
                        .Instances
                        .GetInstanceByName(path);

            instance = instance.BeginUpdate();
            instance[property] = value;
            instance.CommitChanges();
        }

        public void WriteSimulationCurrentValues(Simulation simulation)
        {
            writePropertyCurrentValue("Example site.Pump section.Pump", "Power", simulation.Pump.Power.CurrentValue);
            writePropertyCurrentValue("Example site.Pump section.Pump", "Power state", simulation.Pump.IsPowered ? binaryTextOn : binaryTextOff);

            writePropertyCurrentValue("Example site.Tank area.Source tank", "Level", simulation.SourceTank.Level.CurrentValue);

            writePropertyCurrentValue("Example site.Tank area.Target tank", "Level", simulation.TargetTank.Level.CurrentValue);

            writePropertyCurrentValue("Example site.Pipe", "Flow", simulation.PipeWithPump.Flow.CurrentValue);

            writePropertyCurrentValue("Example site.Flowback pipe", "Flow", simulation.FlowbackPipe.Flow.CurrentValue);
        }

        public void ClearHistory()
        {
            clearPropertyHistory("Example site.Pump section.Pump", "Power");
            clearPropertyHistory("Example site.Pump section.Pump", "Power state");

            clearPropertyHistory("Example site.Tank area.Source tank", "Level");

            clearPropertyHistory("Example site.Tank area.Target tank", "Level");

            clearPropertyHistory("Example site.Pipe", "Flow");

            clearPropertyHistory("Example site.Flowback pipe", "Flow");
        }
    }
}
