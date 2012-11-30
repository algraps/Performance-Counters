using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;

namespace SysCounters
{
    public class PerformanceCounters
    {
        private string machineName = System.Environment.MachineName;

        private PerformanceCounter _cpuPerfCounter = null;
        private PerformanceCounter _ramPerfCounter = null;
        private PerformanceCounter _pagePerfCounter = null;
        private PerformanceCounter[] _nicPerfCounter = null;

        #region properties
        public PerformanceCounter CpuPerfCounter
        {
            get { return _cpuPerfCounter; }
            set { _cpuPerfCounter = value; }
        }

        public PerformanceCounter RamPerfCounter
        {
            get { return _ramPerfCounter; }
            set { _ramPerfCounter = value; }
        }

        public PerformanceCounter PagePerfCounter
        {
            get { return _pagePerfCounter; }
            set { _pagePerfCounter = value; }
        }

        public PerformanceCounter[] NicPerfCounter
        {
            get { return _nicPerfCounter; }
            set { _nicPerfCounter = value; }
        }
        #endregion

        #region public
        public void InizializePerfCounters()
        {
            try
            {
                CpuPerfCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", machineName);
                //ramCounter = new PerformanceCounter("Memory", "Available MBytes", String.Empty, machineName);
                RamPerfCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use", String.Empty, machineName);
                PagePerfCounter = new PerformanceCounter("Paging File", "% Usage", "_Total", machineName);
                // there can be multiple network interfaces
                NicPerfCounter = GetNICCounters();
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Unable to access computer '{0}'\r\nPlease check spelling and verify this computer is connected to the network", this.machineName));
            }
        }

        public void DisposePerfCounters()
        {
            try
            {
                // dispose of the counters
                if (CpuPerfCounter != null)
                { CpuPerfCounter.Dispose(); }
                if (RamPerfCounter != null)
                { RamPerfCounter.Dispose(); }
                if (PagePerfCounter != null)
                { PagePerfCounter.Dispose(); }
                if (NicPerfCounter != null)
                {
                    foreach (PerformanceCounter counter in NicPerfCounter)
                    { counter.Dispose(); }
                }
            }
            finally
            { PerformanceCounter.CloseSharedResources(); }
        }

        // machinename defaults to local computer, but we can
        // specify remote computer to monitor as well via command line param
        public void GetMachineName()
        {
            string[] cmdArgs = System.Environment.GetCommandLineArgs();
            if ((cmdArgs != null) && (cmdArgs.Length > 1))
            { this.machineName = cmdArgs[1]; }
        }

        // ping the remote computer
        public bool VerifyRemoteMachineStatus(string machineName)
        {
            try
            {
                using (Ping ping = new Ping())
                {
                    PingReply reply = ping.Send(machineName);
                    if (reply.Status == IPStatus.Success)
                    { return true; }
                }
            }
            catch (Exception ex)
            {
                // return false for any exception encountered
                // we'll probably want to just shut down anyway
            }
            return false;
        }

        // machine can have multiple nic cards (and laptops usually have wifi & wired)
        // don't want to get into figuring out which one to show, just get all
        // can enumerate network card other ways (System.Net.NetworkInformation)

        // PerformanceCounters can return string[] of available network interfaces
        public string[] GetNICInstances(string machineName)
        {
            string filter = "MS TCP Loopback interface";
            List<string> nics = new List<string>();
            PerformanceCounterCategory category = new PerformanceCounterCategory("Network Interface", machineName);
            if (category.GetInstanceNames() != null)
            {
                foreach (string nic in category.GetInstanceNames())
                {
                    if (!nic.Equals(filter, StringComparison.InvariantCultureIgnoreCase))
                    { nics.Add(nic); }
                }
            }
            return nics.ToArray();
        }

        // create a Performance Counter for each network interface
        public PerformanceCounter[] GetNICCounters()
        {
            string[] nics = GetNICInstances(this.machineName);
            List<PerformanceCounter> nicCounters = new List<PerformanceCounter>();
            foreach (string nicInstance in nics)
            {
                nicCounters.Add(new PerformanceCounter("Network Interface", "Bytes Total/sec", nicInstance, this.machineName));
            }
            return nicCounters.ToArray();
        }

        
        #endregion
        
  

    }
}