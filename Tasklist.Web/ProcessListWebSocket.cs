using System;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;

using Tasklist.Background;
using Tasklist.Middleware.Websocket;

namespace Tasklist.Web
{

    public class ProcessListWebSocket : WebSocketHandler
    {
        private readonly IProcessRepository _processRepository;
        private CancellationTokenSource _stoppingToken;
        private readonly int _refreshRateInMS;
        public ProcessListWebSocket(WebSocketConnectionManager connectionManager, IProcessRepository processRepository, IConfiguration configuration) : base(connectionManager)
        {
            _processRepository = processRepository;
            int.TryParse(configuration["TasklistRefreshRateMS"], out _refreshRateInMS);
            if (_refreshRateInMS == 0)
            {
                _refreshRateInMS = 50;
            }
        }

        public override async Task OnConnected(WebSocket socket)
        {
            await base.OnConnected(socket);
            _stoppingToken = new CancellationTokenSource();
            await Task.Run(async () =>
             {
                 while (!_stoppingToken.IsCancellationRequested)
                 {
                     if (_processRepository.ProcessInformation.Any())
                     {
                         await SendMessageAsync(socket, JsonSerializer.Serialize(_processRepository.ProcessInformation,
                             new JsonSerializerOptions
                             {
                                 PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                             }));
                     }

                     await Task.Delay(TimeSpan.FromMilliseconds(_refreshRateInMS));
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
