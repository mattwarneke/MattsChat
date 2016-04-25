namespace MattsChat
{
    using System;
    using System.Net.WebSockets;
    using System.Threading;

    public class WebClient : Baseclient
    {
        public WebClient(ClientWebSocket socket)
        {
            this.Socket = socket;
        }

        public ClientWebSocket Socket {  get; private set; }

        public new void Send(OutboundMessage message)
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(message.ToBytes());
            this.Socket.SendAsync(buffer, WebSocketMessageType.Binary, true, new CancellationToken(false));
        }
    }
}