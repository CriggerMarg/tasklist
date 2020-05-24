using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Tasklist.Background.Extensions;

namespace Tasklist.Background
{
    public class SysInfoHostedService : BackgroundService
    {
        private readonly ILogger<SysInfoHostedService> _logger;
        private readonly IProcessRepository _processRepository;
        private readonly int _refreshRateInMs;
        private readonly int _cpuHighValue;
        private readonly int _memoryLowValue;
        public SysInfoHostedService(ILogger<SysInfoHostedService> logger, IProcessRepository processRepository, IConfiguration configuration)
        {
            _logger = logger;
            _processRepository = processRepository;
            _refreshRateInMs = configuration.ReadIntConfigValue("TasklistRefreshRateMS", 50);
            _cpuHighValue = configuration.ReadIntConfigValue("cpuHighValue", 90);
            _memoryLowValue = configuration.ReadIntConfigValue("memoryLowValue", 1024);
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
                    var info = ReadSysInfo();
                    _processRepository.IsMemoryLow = info.LowMemory;
                    _processRepository.IsCpuHigh = info.HighCpu;

                    await Task.Delay(TimeSpan.FromMilliseconds(_refreshRateInMs), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error occurred executing {GetType().Name}");
                }
            }
        }

        private SysInfo ReadSysInfo()
        {
            var query = "(Get-Counter -Counter '\\Memory\\Available MBytes','\\Processor(_Total)\\% Processor Time').CounterSamples.CookedValue";
            // powershell may not work on machine where it would be ran. So consider yo use WMI too
            var stringData = string.Empty;
            var errorData = string.Empty;
            using (var process = new Process())
            {
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = $"-NoProfile -ExecutionPolicy unrestricted \"{query }\"";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.OutputDataReceived += (sender, data) =>
                {
                    if (!string.IsNullOrEmpty(data.Data))
                    {
                        stringData += data.Data + Environment.NewLine;
                    }
                };
                process.ErrorDataReceived += (sender, data) => errorData += data.Data;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit(1000 * 10);
            }
            if (!string.IsNullOrEmpty(errorData))
            {
                _logger.LogError(errorData);
            }
            try
            {
                if (!string.IsNullOrEmpty(stringData))
                {
                    var data = stringData.Split(Environment.NewLine);
                    float memory;
                    float.TryParse(data[0], out memory);

                    float cpu;
                    float.TryParse(data[1], out cpu);

                    return new SysInfo
                    {
                        HighCpu = cpu > _cpuHighValue,
                        LowMemory = memory < _memoryLowValue
                    };
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "oops");
            }

            return new SysInfo();
        }
    }
}
