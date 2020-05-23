using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Tasklist.Background
{
    public class ProcessListHostedService : BackgroundService
    {
        private readonly ILogger<ProcessListHostedService> _logger;
        private readonly IProcessRepository _processRepository;
        private readonly int _refreshRateInMS;

        public ProcessListHostedService(ILogger<ProcessListHostedService> logger, IProcessRepository processRepository, IConfiguration configuration)
        {
            _logger = logger;
            _processRepository = processRepository;
            int.TryParse(configuration["TasklistRefreshRateMS"], out _refreshRateInMS);
            if (_refreshRateInMS == 0)
            {
                _refreshRateInMS = 50;
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{GetType().Name} is stopping.");

            await base.StopAsync(stoppingToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await BackgroundProcessing(stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _processRepository.ProcessInformation = ReadProcessInfo();

                    await Task.Delay(TimeSpan.FromMilliseconds(_refreshRateInMS), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error occurred executing {GetType().Name}");
                }
            }
        }

        private IReadOnlyCollection<ProcessInformation> ReadProcessInfo()
        {
            var list = new List<ProcessInformation>();
            var query = "Get-Counter '\\Process(*)\\% Processor Time' | Select-Object -ExpandProperty countersamples| Select-Object -Property instancename, cookedvalue| ? {$_.instanceName -notmatch '^ (idle | _total | system)$'} | Sort-Object -Property cookedvalue -Descending| Select-Object -First 25| ft InstanceName,@{L = 'CPU';E={($_.Cookedvalue/100/$env:NUMBER_OF_PROCESSORS).toString('P')}} -AutoSize -HideTableHeaders";


            // powershell may not work on machine where it would be ran. So consider yo use WMI too
            var stringData = string.Empty;
            var errorData = string.Empty;
            using (var process = new Process())
            {
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = $"-NoProfile -ExecutionPolicy unrestricted \"{query}\"";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.OutputDataReceived += (sender, data) => stringData += data.Data;
                process.ErrorDataReceived += (sender, data) => errorData += data.Data;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit(1000 * 10);
            }

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
                    var name = els.First();

                    if (name == "_total" || name == "idle" || name == "system")
                    {
                        continue;
                    }
                    float cpu = 0;
                    float.TryParse(els.Last(), out cpu);
                    list.Add(new ProcessInformation()
                    {
                        Name = els.First(),
                        CPULoad = cpu
                    });
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "oops");
            }

            if (!string.IsNullOrEmpty(errorData))
            {
                _logger.LogError(errorData);
            }
            return list;
        }
    }
}
