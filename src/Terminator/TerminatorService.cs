using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Timers;

namespace Terminator
{
    public partial class TerminatorService : ServiceBase
    {
        private static readonly double _defaultTimeToKillMs = 20 * 60 * 1000;
        private static readonly double _defaultTimerResolutionMs = 60 * 1000;

        private readonly ReadOnlyCollection<string> _processesToWatch;
        private readonly TimeSpan _timeToKill;
        private readonly Timer _timer;

        public TerminatorService()
        {
            InitializeComponent();
            ServiceName = "Process Terminator";
            EventLog.Log = "Application";
            CanStop = true;

            var settings = ConfigurationManager.AppSettings;
            
            _timeToKill = TimeSpan.FromMilliseconds(settings.ParseDouble("KillAfter") ?? _defaultTimeToKillMs);
            _processesToWatch = new ReadOnlyCollection<string>(settings.ParseValues("ProcessNames", ';'));

            _timer = new Timer { Interval = settings.ParseDouble("CheckTimerInterval") ?? _defaultTimerResolutionMs };
            _timer.Elapsed += OnTimerTick;
        }

        public void Start()
        {
            OnStart(new string[0]);
        }

        protected override void OnStart(string[] args)
        {
            _timer.Start();
            EventLog.WriteEntry(String.Format(
                "Terminator started. Timer interval: {0}. Kill after: {1}. Watching: {2}",
                _timer.Interval, _timeToKill, String.Join(",", _processesToWatch)));
        }

        protected override void OnStop()
        {
            _timer.Stop();
        }

        private void OnTimerTick(object sender, ElapsedEventArgs e)
        {
            var processes = Process.GetProcesses().Where(x => _processesToWatch.Contains(x.ProcessName, StringComparer.InvariantCultureIgnoreCase));

            var sb = new StringBuilder();

            foreach (var process in processes)
            {
                var now = DateTimeOffset.UtcNow;
                var started = process.StartTime.ToUniversalTime();
                var runningFor = now - started;

                sb.AppendFormat("{1} have been running for {2}.{0}", Environment.NewLine, process.ProcessName,
                    runningFor);

                if (runningFor >= _timeToKill)
                {
                    sb.AppendFormat("Killing {1}{0}", Environment.NewLine, process.ProcessName);

                    try
                    {
                        process.Kill();
                    }
                    catch (Exception ex)
                    {
                        EventLog.WriteEntry(
                            String.Format("Unable to kill process {0}. Reason: {1}", process.ProcessName, ex),
                            EventLogEntryType.Error);
                    }
                }

                sb.AppendLine();
            }

            var logEntry = sb.ToString().Trim();
            if (logEntry.Length > 0)
            {
                EventLog.WriteEntry(logEntry);
            }
        }
    }
}
