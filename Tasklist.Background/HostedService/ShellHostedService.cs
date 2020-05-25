using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Tasklist.Background.HostedService
{
    public abstract class ShellHostedService<T> : BackgroundService
    {
        protected readonly ILogger<ShellHostedService<T>> Logger;

        protected ShellHostedService(ILogger<ShellHostedService<T>> logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Run shell with query and collect output
        /// </summary>
        /// <returns></returns>
        protected internal string RunShell(string query)
        {
            var stringData = string.Empty;
            var errorData = string.Empty;
            try
            {
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

            }
            // exceptions that reflects problem with running Process itself
            catch (Exception e)
            {
                Logger.LogError(e, "general process run error");
            }
            // exceptions and error for query
            if (!string.IsNullOrEmpty(errorData))
            {
                Logger.LogError(errorData);
            }
            return stringData;
        }

   
      
        /// <summary>
        /// Parse raw data that came from shell run
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected abstract T ParseShellQueryResult(string data);

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation($"{GetType().Name} is stopping.");

            await base.StopAsync(stoppingToken);
        }
    }
}
