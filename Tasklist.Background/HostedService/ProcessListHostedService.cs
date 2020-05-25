using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Tasklist.Background.HostedService
{
    public class ProcessListHostedService : ShellHostedService<IReadOnlyCollection<ProcessInformation>>
    {
        #region config strings
        // in real life production this would be in configuration
        private const string ProcessListShellQuery =
            "Get-Counter '\\Process(*)\\% Processor Time' | Select-Object -ExpandProperty countersamples| Select-Object -Property instancename, cookedvalue| ? {$_.instanceName -notmatch '^ (idle | _total | system)$'} | Sort-Object -Property cookedvalue -Descending| Select-Object -First 25| ft InstanceName,@{L = 'CPU';E={($_.Cookedvalue/100/$env:NUMBER_OF_PROCESSORS).toString('P')}} -AutoSize -HideTableHeaders";

        private const string TotalProcessName = "_total";
        private const string IdleProcessName = "idle";
        private const string SystemProcessName = "system";

        #endregion

        private readonly int _refreshRate;

        private readonly IProcessRepository _processRepository;

        #region .ctor

        public ProcessListHostedService(ILogger<ProcessListHostedService> logger, IProcessRepository processRepository, IConfiguration configuration)
        : base(logger)
        {
            _processRepository = processRepository;
            int.TryParse(configuration["TasklistRefreshRateMS"], out _refreshRate);
            if (_refreshRate == 0)
            {
                _refreshRate = 50;
            }
        }

        #endregion

        /// <summary>
        /// Read process load info from system and writes it to external component
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var rawData = RunShell(ProcessListShellQuery);
                    await _processRepository.SetProcessInfo(ParseShellQueryResult(rawData));

                    await Task.Delay(TimeSpan.FromMilliseconds(_refreshRate), stoppingToken);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error occurred executing {GetType().Name}");
                }
            }
        }


        protected override IReadOnlyCollection<ProcessInformation> ParseShellQueryResult(string stringData)
        {
            var list = new List<ProcessInformation>();
            try
            {
                var entries = stringData.Trim().Split("%");
                foreach (var entry in entries)
                {
                    if (string.IsNullOrEmpty(entry))
                    {
                        continue;
                    }
                    var els = entry.Trim().Split(' ');
                    var name = els.FirstOrDefault();
                    if (string.IsNullOrEmpty(name))
                    {
                        // we obviously not interested in empty process name
                        continue;
                    }
                    if (name.Equals(TotalProcessName, StringComparison.InvariantCultureIgnoreCase) ||
                        name.Equals(IdleProcessName, StringComparison.InvariantCultureIgnoreCase) ||
                        name.Equals(SystemProcessName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }
                    float cpu;
                    float.TryParse(els.Last(), out cpu);
                    if (cpu > 0)
                    {
                        list.Add(new ProcessInformation(name: els.First(), cpuLoad: cpu));
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Process information reading error");
            }
            return list;
        }
    }
}
