using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Tasklist.Background.Extensions;

namespace Tasklist.Background
{
    public class InMemoryProcessRepository : IProcessRepository
    {
        #region private variables

        private IReadOnlyCollection<ProcessInformation> _processes = new List<ProcessInformation>();
        private static readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();

        private WebSocket _socket; 

        #endregion

        private async Task Send()
        {
            if (_socket == null)
            {
                return;
            }
            if (_socket.State != WebSocketState.Open)
                return;
            // for production system it is needed to make sure that message size is not exceeds websockets limits
            // but as I got different confusing information about its limit I decided keep it as it is in this sample
            var message = JsonSerializer.Serialize(ProcessInformation);
            var buffer = new ArraySegment<byte>(Encoding.ASCII.GetBytes(message), 0, message.Length);

            await _socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public IReadOnlyCollection<ProcessInformation> ProcessInformation
        {
            get
            {
                // There is extensions methods that hide usual ReaderWriterLockSlim flow and makes code more readable
                // Look to Extensions folder
                using (_locker.Read())
                    return _processes;
            }
            private set
            {
                using (_locker.Write())
                    _processes = value;
            }
        }

        public async Task SetProcessInfo(IReadOnlyCollection<ProcessInformation> info)
        {
            ProcessInformation = info;
            await Send();
        }

        public bool IsCpuHigh { get; set; }

        public bool IsMemoryLow { get; set; }


        public void AddSocket(WebSocket socket, TaskCompletionSource<object> tcs)
        {
            _socket = socket;
            tcs.SetResult(true);
        }
    }
}