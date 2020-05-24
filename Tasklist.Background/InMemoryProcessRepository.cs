using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Tasklist.Background
{
    public class InMemoryProcessRepository : IProcessRepository
    {
        private IReadOnlyCollection<ProcessInformation> _processes = new List<ProcessInformation>();
        private WebSocket _socket;
        private static readonly object _locker = new object();

        public InMemoryProcessRepository()
        {
            IsCpuHigh = true;
            IsMemoryLow = true;
        }

        public IReadOnlyCollection<ProcessInformation> ProcessInformation
        {
            get
            {
                lock (_locker)
                { return _processes; }
            }
            private set
            {
                lock (_locker)
                {
                    _processes = value;
                }
            }
        }

        public async Task SetProcessInfo(IReadOnlyCollection<ProcessInformation> info)
        {
            ProcessInformation = info;
            await Send();
        }

        public bool IsCpuHigh
        {
            get;
            set;
        }

        public bool IsMemoryLow
        {
            get;
            set;
        }


        private async Task Send()
        {
            if (_socket == null)
            {
                return;
            }
            if (_socket.State != WebSocketState.Open)
                return;

            var message = JsonSerializer.Serialize(ProcessInformation);
            var buffer = new ArraySegment<byte>(Encoding.ASCII.GetBytes(message), 0, message.Length);

            await _socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public void AddSocket(WebSocket socket, TaskCompletionSource<object> tcs)
        {
            _socket = socket;
            tcs.SetResult(true);
        }
    }
}