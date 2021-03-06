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

        private readonly string RTDBHost;
        private readonly string RTDBUsername;
        private readonly string RTDBPassword;

        private readonly int numberOfSites;

        private readonly string toplevelHierarchyPrefix;

        public DBConnection(int _numberOfSites, string _RTDBHost, string _RTDBUsername, string _RTDBPassword, string _toplevelHierarchyPrefix)
        {
            numberOfSites = _numberOfSites;
            RTDBHost = _RTDBHost;
            RTDBUsername = _RTDBUsername;
            RTDBPassword = _RTDBPassword;
            toplevelHierarchyPrefix = _toplevelHierarchyPrefix;
        }

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

        private void writeVariableHistoryData<T>(string name, List<System.DateTime> timestamps, List<T> values)
        {
            cGraphFetchParameters parameters = cGraphFetchParameters.CreateScalarFetch(
                RTDBDriver.Classes["ProcessHistory"],
                DateTime.UtcNow,
                "Variable=?",
                name);

            var gdi = RTDBDriver.GetGraphDataIterator(parameters);

            for (var i = 0; i < timestamps.Count; i++)
            {
                gdi.Write(timestamps[i], values[i], cValueStatus.OK);
            }

            gdi.Close();
        }

        public void WriteSimulationHistoryData(SimulationHistory history)
        {
            if (numberOfSites > 1)
            {
                for (var site = 1; site <= numberOfSites; site++)
                {
                    writeHistoryDataForSite(site, history);
                }
            }
            else
            {
                writeHistoryDataForSite(null, history);
            }

            writeVariableHistoryData("CalcTutorial_A", history.Timestamps, history.VariableA);
            writeVariableHistoryData("CalcTutorial_B", history.Timestamps, history.VariableB);
        }

        private void writeHistoryDataForSite(int? site, SimulationHistory history)
        {
            var topLevelHierarchy = getTopLevelHierarchyName(site);

            writePropertyHistoryData($"{topLevelHierarchy}.Pump section.Pump", "Power", history.Timestamps, history.PumpPower);
            writePropertyHistoryData($"{topLevelHierarchy}.Pump section.Pump", "Power state", history.Timestamps, history.PumpIsPowered);

            writePropertyHistoryData($"{topLevelHierarchy}.Tank area.Source tank", "Level", history.Timestamps, history.SourceTankLevel);

            writePropertyHistoryData($"{topLevelHierarchy}.Tank area.Target tank", "Level", history.Timestamps, history.TargetTankLevel);

            writePropertyHistoryData($"{topLevelHierarchy}.Pipe", "Flow", history.Timestamps, history.PipeWithPumpFlow);

            writePropertyHistoryData($"{topLevelHierarchy}.Flowback pipe", "Flow", history.Timestamps, history.FlowbackPipeFlow);
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

        private void clearVariableHistory(string name)
        {
            cGraphFetchParameters parameters = cGraphFetchParameters.CreateRawFetch(
                RTDBDriver.Classes["ProcessHistory"],
                DateTime.UtcNow.AddDays(-1000),
                DateTime.UtcNow,
                int.MaxValue - 1,
                "Variable=?",
                name);

            var gdi = RTDBDriver.GetGraphDataIterator(parameters);

            gdi.DeleteRange();

            gdi.Close();
        }

        private void writePropertyCurrentValue<T>(string path, string property, T value, DateTime time)
        {
            var instance = RTDBDriver
                        .Classes["Path"]
                        .Instances
                        .GetInstanceByName(path);

            var cvId = instance.Class.GetProperty(property).GetLowLevelValue(instance);

            var cv = (cDbCurrentValue)RTDBDriver.Classes["CurrentValue"].Instances[cvId].BeginUpdate();
            cv.Value = value;
            cv.TimeUTC = time;
            cv.Status = cValueStatus.OK;
            cv.CommitChanges();
        }

        public void WriteSimulationCurrentValues(Simulation simulation)
        {
            if (numberOfSites > 1)
            {
                for (var site = 1; site <= numberOfSites; site++)
                {
                    writeCurrentValuesForSite(site, simulation);
                }
            }
            else
            {
                writeCurrentValuesForSite(null, simulation);
            }

            writeCurrentValueForVariable("CalcTutorial_A", simulation.VariableA.CurrentValue, simulation.SimulateTime);
            writeCurrentValueForVariable("CalcTutorial_B", simulation.VariableB.CurrentValue, simulation.SimulateTime);
        }

        private void writeCurrentValueForVariable(string name, double currentValue, DateTime time)
        {
            var variable = (cDbVariable)RTDBDriver
                .Classes["Variable"]
                .Instances[name];

            var cv = variable.pCurrentValue.BeginUpdate();
            cv.Value = currentValue;
            cv.TimeUTC = time;
            cv.Status = cValueStatus.OK;
            cv.CommitChanges();
        }

        private void writeCurrentValuesForSite(int? site, Simulation simulation)
        {
            var topLevelHierarchy = getTopLevelHierarchyName(site);

            writePropertyCurrentValue($"{topLevelHierarchy}.Pump section.Pump", "Power", simulation.Pump.Power.CurrentValue, simulation.SimulateTime);
            writePropertyCurrentValue($"{topLevelHierarchy}.Pump section.Pump", "Power state", simulation.Pump.IsPoweredInt, simulation.SimulateTime);

            writePropertyCurrentValue($"{topLevelHierarchy}.Tank area.Source tank", "Level", simulation.SourceTank.Level.CurrentValue, simulation.SimulateTime);

            writePropertyCurrentValue($"{topLevelHierarchy}.Tank area.Target tank", "Level", simulation.TargetTank.Level.CurrentValue, simulation.SimulateTime);

            writePropertyCurrentValue($"{topLevelHierarchy}.Pipe", "Flow", simulation.PipeWithPump.Flow.CurrentValue, simulation.SimulateTime);

            writePropertyCurrentValue($"{topLevelHierarchy}.Flowback pipe", "Flow", simulation.FlowbackPipe.Flow.CurrentValue, simulation.SimulateTime);
        }

        public void ClearHistory()
        {
            if (numberOfSites > 1)
            {
                for (var site = 1; site <= numberOfSites; site++)
                {
                    clearHistoryForSite(site);
                }
            }
            else
            {
                clearHistoryForSite(null);
            }

            clearVariableHistory("CalcTutorial_A");
            clearVariableHistory("CalcTutorial_B");
            clearVariableHistory("CalcTutorial_C");
        }

        private void clearHistoryForSite(int? site)
        {
            var topLevelHierarchy = getTopLevelHierarchyName(site);

            clearPropertyHistory($"{topLevelHierarchy}.Pump section.Pump", "Power");
            clearPropertyHistory($"{topLevelHierarchy}.Pump section.Pump", "Power state");

            clearPropertyHistory($"{topLevelHierarchy}.Tank area.Source tank", "Level");
            clearPropertyHistory($"{topLevelHierarchy}.Tank area.Source tank", "ContentsVolume");

            clearPropertyHistory($"{topLevelHierarchy}.Tank area.Target tank", "Level");
            clearPropertyHistory($"{topLevelHierarchy}.Tank area.Target tank", "ContentsVolume");

            clearPropertyHistory($"{topLevelHierarchy}.Pipe", "Flow");

            clearPropertyHistory($"{topLevelHierarchy}.Flowback pipe", "Flow");
        }

        private string getTopLevelHierarchyName(int? site)
        {
            return $"{toplevelHierarchyPrefix}{(site != null ? " " + site : "")}";
        }
    }
}
