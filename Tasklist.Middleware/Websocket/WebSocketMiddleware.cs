using Microsoft.AspNetCore.Http;

using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;


namespace Tasklist.Middleware.Websocket
{
    /// <summary>
    /// Request pipeline chain to serve websockets requests
    /// </summary>
    public class WebSocketMiddleware
    {
        #region private variables

        private readonly RequestDelegate _next;
        private readonly WebSocketHandler _webSocketHandler;

        #endregion

        public WebSocketMiddleware(RequestDelegate next, WebSocketHandler webSocketHandler)
        {
            // asp.net dictates to have RequestDelegate instance in ctor
            _next = next;
            _webSocketHandler = webSocketHandler;
        }

        public async Task Invoke(HttpContext context)
        {
            // short-circuit pipeline. Always. Because this middleware only meant to work with websockets.
            // So it would be registered against ws or wss scheme and really should break request execution
            // if it is not websocket request.

            if (!context.WebSockets.IsWebSocketRequest)
                return;

            var socket = await context.WebSockets.AcceptWebSocketAsync();
            await _webSocketHandler.OnConnected(socket);

            await Receive(socket, async (result, buffer) =>
            {
                switch (result.MessageType)
                {
                    case WebSocketMessageType.Text:
                        {
                            await _webSocketHandler.ReceiveAsync(socket, result, buffer);
                            return;
                        }
                    case WebSocketMessageType.Close:
                        {
                            await _webSocketHandler.OnDisconnected(socket);
                            break;
                        }
                }
            });
        }

        private async Task Receive(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            var buffer = new byte[1024 * 4]; // that's default asp.net socket size. Consider to read from app config instead

            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                handleMessage(result, buffer);
            }
        }
    }
}