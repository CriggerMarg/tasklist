using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System;
using System.Threading;
using System.Threading.Tasks;

using Tasklist.Background.Extensions;

namespace Tasklist.Background.HostedService
{
    public class SysInfoHostedService : ShellHostedService<SysInfo>
    {
        private readonly IProcessRepository _processRepository;
        private readonly int _refreshRateInMs;
        private readonly int _cpuHighValue;
        private readonly int _memoryLowValue;

        private const string SysInfoQuery = "(Get-Counter -Counter '\\Memory\\Available MBytes','\\Processor(_Total)\\% Processor Time').CounterSamples.CookedValue";

        public SysInfoHostedService(ILogger<SysInfoHostedService> logger, IProcessRepository processRepository, IConfiguration configuration)
        : base(logger)
        {
            _processRepository = processRepository;
            _refreshRateInMs = configuration.ReadIntConfigValue("ShellQueryRateMs", 50);
            _cpuHighValue = configuration.ReadIntConfigValue("cpuHighValue", 90);
            _memoryLowValue = configuration.ReadIntConfigValue("memoryLowValue", 1024);
        }

      

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var rawData = RunShell(SysInfoQuery);
                    var info = ParseShellQueryResult(rawData);
                    _processRepository.IsMemoryLow = info.LowMemory;
                    _processRepository.IsCpuHigh = info.HighCpu;

                    await Task.Delay(TimeSpan.FromMilliseconds(_refreshRateInMs), stoppingToken);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error occurred executing {GetType().Name}");
                }
            }
        }

        protected override SysInfo ParseShellQueryResult(string stringData)
        {
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
                Logger.LogError(e, "Can't parse SysInfo");
            }
            return new SysInfo();
        }
    }
}
