namespace MattsChat
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.CompilerServices;
    using System.Text;

    internal class Program
    {
        private const int bufferSize = 2048;
        private const int _PORT = 9399;
        private static Socket _serverSocket;
        private static readonly List<Client> clients = new List<Client>();
        private static readonly List<ChatRoom> chatRooms = new List<ChatRoom>();
        private static readonly byte[] _buffer = new byte[bufferSize];

        private static void Main()
        {
            Console.Title = "Server";
            SetupServer();
            Console.ReadLine(); // When we press enter close everything
            CloseAllSockets();
        }

        private static void SetupServer()
        {
            Console.WriteLine("Setting up server...");
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, _PORT));
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
        private static void CloseAllSockets()
        {
            foreach (Socket socket in clients.Select(c => c.Socket))
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            _serverSocket.Close();
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

            clients.Add(new Client(newSocket));

            byte[] data = Encoding.ASCII.GetBytes("<= Welcome to the XYZ chat server\r\n");
            newSocket.Send(data);

            PromptForNickName(newSocket);

            newSocket.BeginReceive(_buffer, 0, bufferSize, SocketFlags.None, ReceiveCallback, newSocket);

            _serverSocket.BeginAccept(AcceptCallback, null);
        }

        private static void PromptForNickName(Socket newSocket)
        {
            byte[] data2 = Encoding.ASCII.GetBytes("<= Login Name?\r\n=> ");
            newSocket.Send(data2);
        }

        private static void ReceiveCallback(IAsyncResult AR)
        {
            var current = (Socket)AR.AsyncState;
            Client client = clients.First(c => c.Socket.Equals(current));

            try
            {
                //Grab our buffer and count the number of bytes receives
                int bytesRead = current.EndReceive(AR);

                //make sure we've read something, if we haven't it means that the client disconnected
                if (bytesRead > 0)
                {
                    var recBuf = new byte[bytesRead];
                    Array.Copy(_buffer, recBuf, bytesRead);

                    //PacketProtocol packet = new PacketProtocol(2048);
                    //packet.DataReceived(recBuf);

                    //  \u0003 -- end of message?
                    //object message = Util.Deserialize(recBuf);

                    // Handle the message
                    //StringMessage stringMessage = message as StringMessage;
                    //if (stringMessage != null)
                    //{
                    //    Console.WriteLine("Socket read got a string message from " + current.RemoteEndPoint.ToString() + ": " + stringMessage.Message + Environment.NewLine);
                    //    return;
                    //}

                    client.AppendBytes(recBuf);

                    string currentInput = Encoding.ASCII.GetString(recBuf);

                    if (currentInput.Contains("\r\n")
                        || currentInput.Contains("\n"))
                    {
                        string fullMessage = Encoding.ASCII.GetString(client.CurrentBytesSentWithoutNewLine.ToArray());
                        if (fullMessage.EndsWith("\r\n"))
                        {
                            fullMessage = fullMessage.Remove(fullMessage.Length - 2, 2);
                        }
                        Console.WriteLine("Received Text: " + fullMessage);

                        client.ClearBytes();
                        ProcessMessage(current, client, fullMessage);
                    }

                    //Queue the next receive
                    current.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, ReceiveCallback, current);
                }
                else
                {
                    //Callback run but no data, close the connection
                    //supposadly means a disconnect
                    //and we still have to close the socket, even though we throw the event later
                    current.Close();
                    lock (clients.Select(c => c.Socket))
                    {
                        clients.Remove(client);
                    }
                }
            }
            catch (SocketException e)
            {
                //Something went terribly wrong
                //which shouldn't have happened
                if (current != null)
                {
                    current.Close();
                    lock (clients.Select(c => c.Socket))
                    {
                        clients.Remove(client);
                    }
                }
            }
            catch(ObjectDisposedException)
            {
                //Connected client didn't cleanly exit.
                if (current != null)
                {
                    current.Close();
                    lock (clients.Select(c => c.Socket))
                    {
                        clients.Remove(client);
                    }
                }
            }
        }

        private static void ProcessMessage(Socket current, Client client, string text)
        {
            text = text.ToLower();
            //ToDo: use the message class here...
            if (client.IsInChatroom)
            {
                if (text.Equals("/leave"))
                {
                    ChatRoom chatRoom = chatRooms.First(cr => cr.UniqueId == client.chatRoomUniqueId);
                    chatRoom.ClientLeave(client);
                }
                else
                {
                    ChatRoom chatRoom = chatRooms.First(cr => cr.UniqueId == client.chatRoomUniqueId);
                    byte[] message = Encoding.ASCII.GetBytes(
                        Environment.NewLine + "<= " + client.Nickname + ": "
                        + text + Environment.NewLine + "=>");
                    //ToDo: reprint all of the clients bytes so it feels like they keep typing?
                    chatRoom.BroadCastMessage(message);
                }
            }
            else if (!client.HasNickname)
            {
                if (clients.Any(c => c.Nickname == text))
                {
                    byte[] data = Encoding.ASCII.GetBytes("<= Sorry, name taken." + Environment.NewLine + "=> ");
                    current.Send(data);
                    PromptForNickName(current);
                    return;
                }

                client.SetNickname(text);
                byte[] welcomeReply = Encoding.ASCII.GetBytes("<= Welcome " + text + "!" + Environment.NewLine + "=> ");
                current.Send(welcomeReply);
            }
            else if (text.ToLower() == "/rooms")
            {
                SendActiveRoomsTo(current);
            }
            else if (text.ToLower().StartsWith("/join "))
            {
                string roomName = text.ToLower().Replace("/join ", "");
                ChatRoom chatroomToEnter = chatRooms.FirstOrDefault(cr => cr.Name.Equals(roomName));
                if (chatroomToEnter != null)
                {
                    byte[] enteringRoomBytes = Encoding.ASCII.GetBytes("<= entering room: " + chatroomToEnter.Name + Environment.NewLine);
                    current.Send(enteringRoomBytes);

                    chatroomToEnter.ClientJoin(client);

                    client.EnterChatroom(chatroomToEnter.UniqueId);

                    foreach (Client chatroomClient in chatroomToEnter.Clients)
                    {
                        string clientNameStr = 
                            client.Equals(chatroomClient)
                            ? "<= * " + client.Nickname + "(** this is you)" + Environment.NewLine
                            : "<= * " + client.Nickname + Environment.NewLine;
                        byte[] clientNameBytes = Encoding.ASCII.GetBytes(clientNameStr);
                        current.Send(clientNameBytes);
                    }

                    current.Send(Responses.EndOfListBytes());
                }
                else
                {
                    byte[] noSuchRoomMsg = Encoding.ASCII.GetBytes("<= Sorry, no such room.\r\n");
                    current.Send(noSuchRoomMsg);

                    SendActiveRoomsTo(current);
                }
            }
            else if (text.ToLower() == "/quit") // Client wants to exit gracefully
            {
                byte[] quitMsg = Encoding.ASCII.GetBytes("<= BYE\r\n");
                current.Send(quitMsg);
                // Always Shutdown before closing
                current.Shutdown(SocketShutdown.Both);
                current.Close();
                clients.Remove(client);
                Console.WriteLine("Client disconnected");
                return;
            }
            else
            {
                byte[] noSuchCommandMsg = Encoding.ASCII.GetBytes("<= Invalid command, join a chatroom to start chatting!\r\n");
                current.Send(noSuchCommandMsg);

                SendActiveRoomsTo(current);
            }

            current.BeginReceive(_buffer, 0, bufferSize, SocketFlags.None, ReceiveCallback, current);
        }

        //private void ChildSocket_PacketArrived(SimpleServerChildTcpSocket socket, AsyncResultEventArgs<byte[]> e)
        //{
        //    try
        //    {
        //        // Check for errors
        //        if (e.Error != null)
        //        {
        //            textBoxLog.AppendText("Client socket error during Read from " + socket.RemoteEndPoint.ToString() + ": [" + e.Error.GetType().Name + "] " + e.Error.Message + Environment.NewLine);
        //            ResetChildSocket(socket);
        //        }
        //        else if (e.Result == null)
        //        {
        //            // PacketArrived completes with a null packet when the other side gracefully closes the connection
        //            textBoxLog.AppendText("Socket graceful close detected from " + socket.RemoteEndPoint.ToString() + Environment.NewLine);

        //            // Close the socket and remove it from the list
        //            ResetChildSocket(socket);
        //        }
        //        else
        //        {
        //            // At this point, we know we actually got a message.

        //            // Deserialize the message
        //            object message = Messages.Util.Deserialize(e.Result);

        //            // Handle the message
        //            Messages.StringMessage stringMessage = message as Messages.StringMessage;
        //            if (stringMessage != null)
        //            {
        //                textBoxLog.AppendText("Socket read got a string message from " + socket.RemoteEndPoint.ToString() + ": " + stringMessage.Message + Environment.NewLine);
        //                return;
        //            }

        //            Messages.ComplexMessage complexMessage = message as Messages.ComplexMessage;
        //            if (complexMessage != null)
        //            {
        //                textBoxLog.AppendText("Socket read got a complex message from " + socket.RemoteEndPoint.ToString() + ": (UniqueID = " + complexMessage.UniqueID.ToString() +
        //                    ", Time = " + complexMessage.Time.ToString() + ", Message = " + complexMessage.Message + ")" + Environment.NewLine);
        //                return;
        //            }

        //            textBoxLog.AppendText("Socket read got an unknown message from " + socket.RemoteEndPoint.ToString() + " of type " + message.GetType().Name + Environment.NewLine);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        textBoxLog.AppendText("Error reading from socket " + socket.RemoteEndPoint.ToString() + ": [" + ex.GetType().Name + "] " + ex.Message + Environment.NewLine);
        //        ResetChildSocket(socket);
        //    }
        //    finally
        //    {
        //        RefreshDisplay();
        //    }
        //}

        public static void SendActiveRoomsTo(Socket clientSocket)
        {
            byte[] activeChatRoomBytes = Encoding.ASCII.GetBytes("<= Active rooms are:\r\n");
            clientSocket.Send(activeChatRoomBytes);

            foreach (ChatRoom chatRoom in chatRooms)
            {
                byte[] chatRoomNameBytes = Encoding.ASCII.GetBytes(
                    "<= * " + chatRoom.Name 
                    + " (" + chatRoom.Clients.Count + ")" + "\r\n");
                clientSocket.Send(chatRoomNameBytes);
            }

            clientSocket.Send(Responses.EndOfListBytes());
        }

        public static void SendData(IAsyncResult asyncResult)
        {
            try
            {
                _serverSocket.EndSend(asyncResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendData Error: " + ex.Message);
            }
        }
    }
}