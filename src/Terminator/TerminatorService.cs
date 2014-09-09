using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Text;
using System.Timers;

namespace Terminator
{
	public partial class TerminatorService : ServiceBase
	{
		private const double DefaultTimeToKillMs = 20*60*1000;
		private const double DefaultTimerResolutionMs = 60*1000;

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

			_timeToKill = TimeSpan.FromMilliseconds(settings.ParseDouble("KillAfter") ?? DefaultTimeToKillMs);
			_processesToWatch = new ReadOnlyCollection<string>(settings.ParseValues("ProcessNames", ';'));

			_timer = new Timer {Interval = settings.ParseDouble("CheckTimerInterval") ?? DefaultTimerResolutionMs};
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
			var sb = new StringBuilder();

			foreach (var p in GetMatchedProcesses())
			{
				Process process = null;

				try
				{
					process = Process.GetProcessById(int.Parse(p["ProcessId"].Value.ToString()));
				}
				catch (Exception ex)
				{
					EventLog.WriteEntry(
						String.Format("Unable to kill process {0} with ProcessId {1}. Reason: {2}", p["CommandLine"].Value, p["ProcessId"].Value, ex),
						EventLogEntryType.Error);
				}

				if(process == null)
					continue;

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

		private IEnumerable<PropertyDataCollection> GetMatchedProcesses()
		{
			var result = new List<PropertyDataCollection>();

			try
			{
				if (_processesToWatch == null || !_processesToWatch.Any())
					return result.ToArray();

				var wmiQueryBuilder = new StringBuilder("select * from Win32_Process where");
				for (int i = 0; i < _processesToWatch.Count; i++)
				{
					wmiQueryBuilder.AppendFormat("{0} CommandLine like '%{1}%'", i == 0 ? string.Empty : " or", _processesToWatch[i]);
				}

				using (var searcher = new ManagementObjectSearcher(wmiQueryBuilder.ToString()))
				{
					//execute the query
					ManagementObjectCollection processes = searcher.Get();
					if (processes.Count > 0)
					{
						result.AddRange(from ManagementObject process in processes select process.Properties);
					}

					EventLog.WriteEntry(
						string.Format("{0} process(es) matching pattent '{1}' found",
						              result.Any() ? result.Count.ToString(CultureInfo.InvariantCulture) : "No", wmiQueryBuilder),
						EventLogEntryType.Information);
				}
			}
			catch (Exception ex)
			{
				EventLog.WriteEntry(
					String.Format("Error occured while executing the query: {0}", ex),
					EventLogEntryType.Error);
				return new PropertyDataCollection[] {};
			}

			return result.ToArray();
		}
	}
}
