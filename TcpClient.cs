namespace MattsChat
{
    using System.Net.Sockets;

    public class TcpClient : Baseclient
    {
        public TcpClient(Socket socket)
        {
            this.Socket = socket;
        }

        public Socket Socket { get; private set; }

        public override void Send(OutboundMessage message)
        {
            this.Socket.Send(message.ToBytes());
        }

        public override void Send(byte[] message)
        {
            this.Socket.Send(message);
        }

        public override void Disconnect()
        {
            if (!this.Socket.Connected)
            {
                return;
            }

            lock (this.Socket)
            {
                // Always Shutdown before closing
                this.Socket.Shutdown(SocketShutdown.Both);
                this.Socket.Close();
            }
        }
    }
}