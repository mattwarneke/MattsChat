namespace MattsChat
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Sockets;
    using System.Text;

    public class ClientService
    {
        private ICollection<IClient> Clients { get; set; } 
        private ICollection<ChatRoom> Chatrooms { get; set; } 

        public ClientService()
        {
            this.Clients = new List<IClient>();
            this.Chatrooms = new List<ChatRoom>();

            this.Chatrooms.Add(new ChatRoom("chat"));
            this.Chatrooms.Add(new ChatRoom("hottub"));
        }

        public void AddClient(IClient client)
        {
            this.Clients.Add(client);
        }

        public void ProcessMessage(IClient client, InboundMessage message)
        {
            string text = message.StringMessage;

            if (client.IsInChatroom)
            {
                if (text.ToLower().Equals("/leave"))
                {
                    ChatRoom chatRoom = this.Chatrooms.FirstOrDefault(cr => cr.UniqueId == client.ChatRoomUniqueId);
                    if (chatRoom != null)
                    {
                        chatRoom.ClientLeave(client);
                    }
                }
                else
                {//chatroom broadcast - chat area
                    ChatRoom chatRoom = this.Chatrooms.FirstOrDefault(cr => cr.UniqueId == client.ChatRoomUniqueId);
                    if (chatRoom != null)
                    {
                        chatRoom.BroadCastMessage(new OutboundMessage(client.Nickname + ": " + text));
                        return;//input prompt is dealt with above
                    }
                }
            }
            else if (!client.HasNickname)
            {
                if (this.Clients.Any(c => c.Nickname == text))
                {
                    client.Send(new OutboundMessage("Sorry, name taken."));
                    PromptForNickname(client);
                    return;
                }

                client.SetNickname(text);
                client.Send(new OutboundMessage("Welcome " + text + "!"));
                client.Send(new OutboundMessage("To see available chatrooms type: /rooms"));
            }
            else if (text.ToLower() == "/rooms")
            {
                SendActiveRoomsTo(client);
            }
            else if (text.ToLower().StartsWith("/join "))
            {
                string roomName = text.ToLower().Replace("/join ", "");
                ChatRoom chatroomToEnter = this.Chatrooms.FirstOrDefault(cr => cr.Name.Equals(roomName));
                if (chatroomToEnter != null)
                {
                    client.Send(new OutboundMessage("entering room: " + chatroomToEnter.Name));
                    chatroomToEnter.ClientJoin(client);
                    client.EnterChatroom(chatroomToEnter.UniqueId);

                    foreach (IClient chatroomClient in chatroomToEnter.Clients)
                    {
                        string clientNameStr =
                            client.Equals(chatroomClient)
                            ? "* " + chatroomClient.Nickname + "(** this is you)"
                            : "* " + chatroomClient.Nickname;
                        client.Send(new OutboundMessage(clientNameStr));
                    }

                    client.Send(new OutboundMessage("end of list."));
                    client.Send(new OutboundMessage("To leave type: /leave"));
                }
                else
                {
                    client.Send(new OutboundMessage("Sorry, no such room."));
                    this.SendActiveRoomsTo(client);
                }
            }
            else
            {
                client.Send(new OutboundMessage("Invalid command, join a chatroom to start chatting!"));

                this.SendActiveRoomsTo(client);
            }

            SendClientInputPrompt(client);
        }

        private void SendClientInputPrompt(IClient client)
        {
            if (client is TcpClient)
            {
                client.Send(Encoding.ASCII.GetBytes("=> "));
            }
        }

        public void PromptForNickname(IClient client)
        {
            client.Send(new OutboundMessage("Login Name?"));
            SendClientInputPrompt(client);
        }

        public void SendActiveRoomsTo(IClient client)
        {
            try
            {
                client.Send(new OutboundMessage("Active rooms are:"));

                foreach (ChatRoom chatRoom in this.Chatrooms)
                {
                    OutboundMessage message = new OutboundMessage(
                        "* " + chatRoom.Name + " (" + chatRoom.Clients.Count + ")");
                    client.Send(message);
                }

                client.Send(new OutboundMessage("end of list."));
                client.Send(new OutboundMessage("type: /join [chatroomname]"));
            }
            catch (SocketException)
            {
                return;
                throw;
            }
        }

        public void LeaveChatRoom(IClient client)
        {
            ChatRoom chatRoom = this.Chatrooms.FirstOrDefault(cr => cr.UniqueId == client.ChatRoomUniqueId);
            if (chatRoom != null)
            {
                chatRoom.ClientLeave(client);
            }
        }

        public void DisconnectClient(IClient client)
        {
            this.Clients.Remove(client);
        }

        public List<TcpClient> GetTcpClients()
        {
            return this.Clients.OfType<TcpClient>().ToList();
        }

        public List<WebClient> GetWebClients()
        {
            return this.Clients.OfType<WebClient>().ToList();
        } 
    }
}
