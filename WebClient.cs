namespace MattsChat
{
    using System;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;

    public class WebClient : Baseclient
    {
        public WebClient(WebSocket socket)
        {
            this.Socket = socket;
        }

        public WebSocket Socket {  get; private set; }

        public override void Send(byte[] message)
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(message);

            Socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None); 
        }

        public override void Send(OutboundMessage message)
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(message.ToBytes());

            Socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);  
        }

        public override void Disconnect()
        {
            if (this.Socket.State != WebSocketState.Open)
            {
                return;
            }

            this.Socket.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, CancellationToken.None);

            this.Socket.Dispose();
        }
    }
}