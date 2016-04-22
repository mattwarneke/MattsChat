namespace MattsChat
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.CompilerServices;
    using System.Text;

    public class Program
    {
        private const int bufferSize = 2048;
        private const int _PORT = 443;
        private static Socket _serverSocket;
        private static readonly List<Client> clients = new List<Client>();
        private static readonly List<ChatRoom> chatRooms = new List<ChatRoom>();
        private static readonly byte[] _buffer = new byte[bufferSize];

        private static void Main()
        {
            Console.Title = "Server";
            SetupServer();
            Console.ReadLine(); // keep app open until read an enter
            CloseAllSockets();
        }

        public static void SetupServer()
        {
            Console.WriteLine("Setting up server...");

            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, _PORT);
            _serverSocket.Bind(endPoint);
            _serverSocket.Listen(5);
            _serverSocket.BeginAccept(AcceptCallback, null);
            chatRooms.Add(new ChatRoom("chat"));
            chatRooms.Add(new ChatRoom("hottub"));
            Console.WriteLine("Server setup complete");
        }

        /// <summary>
        ///     Close all connected client (we do not need to shutdown the server socket as its connections
        ///     are already closed with the clients)
        /// </summary>
        public static void CloseAllSockets()
        {
            foreach (Socket socket in clients.Select(c => c.Socket))
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            _serverSocket.Close();
        }

        private static void CloseClientConnection(Client client)
        {
            Console.WriteLine("close connection " + client.Nickname);
            if (!client.Socket.Connected
                || !clients.Contains(client))
            {
                return;
            }

            ChatRoom chatRoom = chatRooms.FirstOrDefault(cr => cr.UniqueId == client.chatRoomUniqueId);
            if (chatRoom != null)
            {
                chatRoom.ClientLeave(client);

            }
            clients.Remove(client);

            lock (client.Socket)
            {
                // Always Shutdown before closing
                client.Socket.Shutdown(SocketShutdown.Both);
                client.Socket.Close();
            }
        }

        private static void AcceptCallback(IAsyncResult AR)
        {
            Socket newSocket;

            try
            {
                newSocket = _serverSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException) // I cannot seem to avoid this (on exit when properly closing sockets)
            {
                return;
            }

            Client newClient = new Client(newSocket);
            clients.Add(newClient);

            newSocket.Send(new OutboundMessage("Welcome to the matts chat server").ToBytes());

            PromptForNickname(newSocket);

            newSocket.BeginReceive(_buffer, 0, bufferSize, SocketFlags.None, ReceiveCallback, newClient);

            _serverSocket.BeginAccept(AcceptCallback, null);
        }

        private static void ReceiveCallback(IAsyncResult AR)
        {
            Client client = (Client)AR.AsyncState;

            try
            {
                //Grab our buffer and count the number of bytes receives
                int bytesRead = client.Socket.EndReceive(AR);

                //make sure we've read something, if we haven't it means that the client disconnected
                if (bytesRead > 0)
                {
                    var recBuf = new byte[bytesRead];
                    Array.Copy(_buffer, recBuf, bytesRead);

                    client.AppendBytes(recBuf);

                    if (recBuf.Contains((byte)10))//newline
                    {
                        InboundMessage message = new InboundMessage(client.CurrentBytesSentWithoutNewLine.ToArray());

                        Console.WriteLine("Received Text: " + message.StringMessage);

                        client.ClearBytes();

                        if (message.StringMessage == "/quit") // Client wants to exit gracefully
                        {
                            client.Socket.Send(new OutboundMessage("BYE").ToBytes());

                            CloseClientConnection(client);

                            Console.WriteLine("Client disconnected");
                            return;
                        }

                        ProcessMessage(client, message);
                    }

                    //Queue the next receive
                    client.Socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, ReceiveCallback, client);
                }
                else
                {
                    //Callback run but no data, close the connection - TIMEOUT
                    Console.WriteLine("Timeout: " + client.Nickname);
                    CloseClientConnection(client);
                }
            }
            catch (SocketException e)
            {
                //Something went terribly wrong, which shouldn't have happened
                if (client != null)
                {
                    CloseClientConnection(client);
                }
            }
            catch(ObjectDisposedException)
            {
                //Connected client didn't cleanly exit.
                if (client != null)
                {
                    CloseClientConnection(client);
                }
            }
        }

        private static void ProcessMessage(Client client, InboundMessage message)
        {
            string text = message.StringMessage;

            //ToDo: use the message class here...
            if (client.IsInChatroom)
            {
                if (text.Equals("/leave"))
                {
                    ChatRoom chatRoom = chatRooms.FirstOrDefault(cr => cr.UniqueId == client.chatRoomUniqueId);
                    if (chatRoom != null)
                    {
                        chatRoom.ClientLeave(client);
                    }
                }
                else
                {//chatroom broadcast - chat area
                    ChatRoom chatRoom = chatRooms.FirstOrDefault(cr => cr.UniqueId == client.chatRoomUniqueId);
                    if (chatRoom != null)
                    {
                        chatRoom.BroadCastMessage(new OutboundMessage(client.Nickname + ": " + text).ToBytes());
                        return;//input prompt is dealt with above
                    }
                }
            }
            else if (!client.HasNickname)
            {
                if (clients.Any(c => c.Nickname == text))
                {
                    client.Socket.Send(new OutboundMessage("Sorry, name taken.").ToBytes());
                    PromptForNickname(client.Socket);
                    return;
                }

                client.SetNickname(text);
                client.Socket.Send(new OutboundMessage("Welcome " + text + "!").ToBytes());
                client.Socket.Send(new OutboundMessage("To see available chatrooms type: /rooms").ToBytes());
            }
            else if (text == "/rooms")
            {
                SendActiveRoomsTo(client.Socket);
            }
            else if (text.StartsWith("/join "))
            {
                string roomName = text.ToLower().Replace("/join ", "");
                ChatRoom chatroomToEnter = chatRooms.FirstOrDefault(cr => cr.Name.Equals(roomName));
                if (chatroomToEnter != null)
                {
                    client.Socket.Send(new OutboundMessage("entering room: " + chatroomToEnter.Name).ToBytes());
                    chatroomToEnter.ClientJoin(client);
                    client.EnterChatroom(chatroomToEnter.UniqueId);

                    foreach (Client chatroomClient in chatroomToEnter.Clients)
                    {
                        string clientNameStr = 
                            client.Equals(chatroomClient)
                            ? "* " + chatroomClient.Nickname + "(** this is you)"
                            : "* " + chatroomClient.Nickname;
                        client.Socket.Send(new OutboundMessage(clientNameStr).ToBytes());
                    }

                    client.Socket.Send(new OutboundMessage("end of list.").ToBytes());
                    client.Socket.Send(new OutboundMessage("To leave type: /leave").ToBytes());
                }
                else
                {
                    client.Socket.Send(new OutboundMessage("Sorry, no such room.").ToBytes());
                    SendActiveRoomsTo(client.Socket);
                }
            }
            else
            {
                client.Socket.Send(new OutboundMessage("Invalid command, join a chatroom to start chatting!").ToBytes());

                SendActiveRoomsTo(client.Socket);
            }

            SendClientInputPrompt(client.Socket);
        }

        private static void PromptForNickname(Socket clientSocket)
        {
            clientSocket.Send(new OutboundMessage("Login Name?").ToBytes());
            SendClientInputPrompt(clientSocket);
        }

        private static void SendClientInputPrompt(Socket clientSocket)
        {
            clientSocket.Send(Encoding.ASCII.GetBytes("=> "));
        }

        public static void SendActiveRoomsTo(Socket clientSocket)
        {
            try
            {
                clientSocket.Send(new OutboundMessage("Active rooms are:").ToBytes());

                foreach (ChatRoom chatRoom in chatRooms)
                {
                    OutboundMessage message = new OutboundMessage(
                        "* " + chatRoom.Name + " (" + chatRoom.Clients.Count + ")");
                    clientSocket.Send(message.ToBytes());
                }

                clientSocket.Send(new OutboundMessage("end of list.").ToBytes());
                clientSocket.Send(new OutboundMessage("type: /join [chatroomname]").ToBytes());
            }
            catch (SocketException)
            {
                return;
                throw;
            }
        }
    }
}