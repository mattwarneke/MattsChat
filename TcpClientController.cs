namespace MattsChat
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;

    public class TcpClientController
    {
        private const int BufferSize = 2048;
        private const int Port = 443;
        private readonly Socket _serverSocket;
        private readonly List<IClient> Clients = new List<IClient>();
        private readonly byte[] Buffer = new byte[BufferSize];
        private ClientService ClientService { get; set; }

        public TcpClientController(ClientService clientService)
        {
            Console.WriteLine("Setting up server...");
            this._serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, Port);
            _serverSocket.Bind(endPoint);
            _serverSocket.Listen(15);
            _serverSocket.BeginAccept(AcceptCallback, null);

            this.ClientService = clientService;

            Console.WriteLine("Server setup complete");
        }

        /// <summary>
        ///     Close all connected client (we do not need to shutdown the server socket as its connections
        ///     are already closed with the clients)
        /// </summary>
        public void CloseAllSockets()
        {
            List<TcpClient> iClients = Clients.OfType<TcpClient>().ToList();
            foreach (Socket socket in iClients.Select(c => c.Socket))
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            this._serverSocket.Close();
        }

        private void AcceptCallback(IAsyncResult AR)
        {
            Socket newSocket;

            try
            {
                newSocket = this._serverSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException) // I cannot seem to avoid this (on exit when properly closing sockets)
            {
                return;
            }

            TcpClient newClient = new TcpClient(newSocket);
            this.ClientService.AddClient(newClient);

            newClient.Send(new OutboundMessage("Welcome to the matts chat server"));

            this.ClientService.PromptForNickname(newClient);

            newSocket.BeginReceive(Buffer, 0, BufferSize, SocketFlags.None, this.Listen, newClient);

            this._serverSocket.BeginAccept(AcceptCallback, null);
        }

        private void Listen(IAsyncResult AR)
        {
            IClient client = (IClient)AR.AsyncState;
            TcpClient tcpClient = (TcpClient)client;

            try
            {
                //Grab our buffer and count the number of bytes receives
                int bytesRead = tcpClient.Socket.EndReceive(AR);

                //make sure we've read something, if we haven't it means that the client disconnected
                if (bytesRead > 0)
                {
                    byte[] recBuf = new byte[bytesRead];
                    Array.Copy(Buffer, recBuf, bytesRead);

                    client.AppendBytes(recBuf);

                    if (recBuf.Contains((byte)10))//newline
                    {
                        InboundMessage message = new InboundMessage(client.CurrentBytesSentWithoutNewLine.ToArray());
                        Console.WriteLine("Received Text: " + message.StringMessage);
                        client.ClearBytes();

                        if (message.StringMessage == "/quit") // Client wants to exit gracefully
                        {
                            client.Send(new OutboundMessage("BYE"));
                            CloseClientConnection(client);
                            return;
                        }

                        this.ClientService.ProcessMessage(client, message);
                    }

                    //Queue the next receive
                    tcpClient.Socket.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, this.Listen, client);
                }
                else
                {
                    //Callback run but no data, close the connection - TIMEOUT
                    Console.WriteLine("Timeout: " + client.Nickname);
                    this.CloseClientConnection(client);
                }
            }
            catch (SocketException)
            {
                //Something went terribly wrong, which shouldn't have happened
                this.CloseClientConnection(client);
            }
            catch (ObjectDisposedException)
            {
                this.CloseClientConnection(client);
            }
        }

        private void CloseClientConnection(IClient client)
        {
            Console.WriteLine("close connection " + client.Nickname);
            this.ClientService.LeaveChatRoom(client);
            this.ClientService.DisconnectClient(client);

            client.Disconnect();
        }
    }
}
