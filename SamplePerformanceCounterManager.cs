using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using log4net;

namespace SamplePeromrmanceCounter.Core.Diagnostics
{
    [Singleton]
    sealed class SamplePerformanceCounterManager : ISamplePerformanceCounterManager
    {
        //Logging
        private static readonly ILog log = LogManager.GetLogger(typeof(SamplePerformanceCounterManager));

        //Name of the Category of the PerfCounter group
        private const string PERFCOUNTER_CATEGORY_KEY = "PerfCounter Group";

        //Name of the PerfCounter
        private const string PERCOUNTER_CUSTOM1 = "Performance Counter Custom 1";
        private const string PERCOUNTER_CUSTOM2 = "Performance Counter Custom 2";

        /// <summary>
        /// True if the performance counters are configured and ready to use.
        /// If not, be sure to have ran the SetupPerformanceCounters method before access the performance counter values.
        /// </summary>
        public bool IsPerformanceCountersReady { get; private set; }

        public SamplePerformanceCounterManager()
        {
            this.IsPerformanceCountersReady = false;
        }

        #region Performance Counters

        #region PERCOUNTER_CUSTOM1

        private volatile PerformanceCounter _custom1PerfCounter;
        private readonly object _perCounterCustom1SyncLock = new object();
        private PerformanceCounter Custom1PerfCounter
        {
            get
            {
                if (_custom1PerfCounter == null)
                {
                    lock (_perCounterCustom1SyncLock)
                    {
                        if (_custom1PerfCounter == null) // double-check
                        {
                            // Gestisco l'errore perchè il counter potrebbe non essere pronto
                            try
                            {
                                _custom1PerfCounter = new PerformanceCounter(PERFCOUNTER_CATEGORY_KEY, PERCOUNTER_CUSTOM1, false);
                            }
                            catch (Exception ex)
                            {
                                _custom1PerfCounter = null;
                                log.Error(string.Format("Errore durante la creazione del performance counter '{0}'.", PERCOUNTER_CUSTOM1), ex);
                            }
                        }
                    }
                }
                return _custom1PerfCounter;
            }
        }

        /// <summary>
        /// GET/SET VALUE FOR PERF COUNTER.
        /// </summary>
        public long PerfCounterCount
        {
            get
            {
                try
                {
                    if (CheckConfiguration())
                    {
                        return this.Custom1PerfCounter.RawValue;
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex.Message, ex);
                }
                return 0;
            }
            set
            {
                try
                {
                    if (CheckConfiguration())
                    {
                        //DONE SET VALUE
                        this.Custom1PerfCounter.RawValue = value;
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex.Message, ex);
                }
            }
        }

        #endregion               

        #endregion

        private const string MUTEX_NAME = @"Global\SamplePeromrmanceCounter.Core.Diagnostics.SamplePerformanceCounters";

        /// <summary>
        /// Creates all the performance counters. If they already exists, they are destroyed and recreated.
        /// </summary>
        public void SetupPerformanceCounters()
        {
            Mutex globalLock = null;

            try
            {
                this.IsPerformanceCountersReady = false;

                // Utilizzo un lock globale di sistema, per non eseguire il setup contemporaneo tra 2 processi diversi
                globalLock = new Mutex(false, MUTEX_NAME);

                // Aspetto fino a che il lock non viene rilasciato, assegno un timeout di 30 secondi per sicurezza.
                if (!globalLock.WaitOne(TimeSpan.FromSeconds(30)))
                {
                    log.WarnFormat("Cannot setup the performance counter: timeout acquiring the lock on '{0}' mutex lock. Setup aborted.", MUTEX_NAME);

                    // Imposto il manager come se avesse installato correttamente i counter, perchè se entro in questo timeout, vuol dire che un'altra applicazione li sta installando.
                    this.IsPerformanceCountersReady = true;

                    return;
                }


                // Se la categoria non esiste, 
                if (PerformanceCounterCategory.Exists(PERFCOUNTER_CATEGORY_KEY))
                {
                    PerformanceCounterCategory.Delete(PERFCOUNTER_CATEGORY_KEY);

                    CounterCreationDataCollection counterCreationDataCollection = new CounterCreationDataCollection();

                    //Avarage Counter
                    CounterCreationData perfCouterCustom1Counter = new CounterCreationData();
                    perfCouterCustom1Counter.CounterType = PerformanceCounterType.NumberOfItems64;
                    perfCouterCustom1Counter.CounterName = PERCOUNTER_CUSTOM1;
                    perfCouterCustom1Counter.CounterHelp = "help about counter... bla bla bla";
                    counterCreationDataCollection.Add(perfCouterCustom1Counter);

                   PerformanceCounterCategory.Create(
                        PERFCOUNTER_CATEGORY_KEY,
                        "Contains the counters.",
                        PerformanceCounterCategoryType.SingleInstance,
                        counterCreationDataCollection);
                }
                this.IsPerformanceCountersReady = true;
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
            }
            finally
            {
                try
                {
                    if (globalLock != null)
                    {
                        globalLock.ReleaseMutex();
                        globalLock.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    log.Error("Error while trying to ReleaseMutex on the global lock.", ex);
                }
            }
        }

        /// <summary>
        /// Reset all the counters, set them to the default value.
        /// </summary>
        public void ResetAllCounters()
        {
            this.PerfCounterCount = 0;
        }

        private bool CheckConfiguration()
        {
            if (!this.IsPerformanceCountersReady)
            {
                log.Error("Cannot access the performance counters in this object state. You should run first the SetupPerformanceCounters method.");
            }
            return this.IsPerformanceCountersReady;
        }
    }
}
