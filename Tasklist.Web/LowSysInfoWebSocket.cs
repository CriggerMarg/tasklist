using System;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Tasklist.Background;
using Tasklist.Middleware.Websocket;

namespace Tasklist.Web
{
    public class LowSysInfoWebSocket : WebSocketHandler
    {
        private readonly IProcessRepository _processRepository;
        private CancellationTokenSource _stoppingToken;

        public LowSysInfoWebSocket(WebSocketConnectionManager connectionManager, IProcessRepository processRepository) : base(connectionManager)
        {
            _processRepository = processRepository;
        }

        public override async Task OnConnected(WebSocket socket)
        {
            await base.OnConnected(socket);
            _stoppingToken = new CancellationTokenSource();
            await Task.Run(async () =>
            {
                SysInfo lastSent = new SysInfo();
                while (!_stoppingToken.IsCancellationRequested)
                {
                    var info = new SysInfo { HighCpu = _processRepository.IsCpuHigh, LowMemory = _processRepository.IsMemoryLow };
                    if (!info.Equals(lastSent))
                    {
                        lastSent = info;
                        await SendMessageAsync(socket, JsonSerializer.Serialize(info,
                            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                }
            }, _stoppingToken.Token);
        }



        public override Task OnDisconnected(WebSocket socket)
        {
            _stoppingToken.Cancel();
            return base.OnDisconnected(socket);
        }

        public override Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            throw new NotImplementedException();
        }
    }
}
