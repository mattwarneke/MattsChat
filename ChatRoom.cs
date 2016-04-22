using System;
using System.Collections.Generic;
using System.Text;

namespace MattsChat
{
    using System.Linq;
    using System.Net.Sockets;

    class ChatRoom
    {
        public ChatRoom(string name)
        {
            this.Name = name;
            this.Clients = new List<Client>();
            this.UniqueId = Guid.NewGuid();
        }

        public Guid UniqueId { get; private set; }

        public string Name { get; private set; }

        public ICollection<Client> Clients { get; private set; }

        public void ClientJoin(Client client)
        {
            //broadcast entry before client joins, so we don't have to exclude them
            this.BroadCastMessage(new OutboundMessage("* new user joined chat: " + client.Nickname).ToBytes());

            this.Clients.Add(client);
        }

        public void ClientLeave(Client client)
        {
            //remove client first so we can tailor message
            this.Clients.Remove(client);

            client.LeaveChatroom();

            try
            {
                this.BroadCastMessage(new OutboundMessage(
                    "* user has left chat: " + client.Nickname).ToBytes());

                lock (client.Socket)
                {
                    //we use a blocking mode send, no async on the outgoing
                    //since this is primarily a multithreaded application, shouldn't cause problems to send in blocking mode
                    client.Socket.Send(new OutboundMessage(
                        "* user has left chat: " + client.Nickname + " (** this is you)").ToBytes());
                }
            }
            catch (SocketException)
            {
                //Something went horribly wrong
                return;
            }
        }

        public void BroadCastMessage(byte[] byteMsg)
        {
            foreach (Client receiver in this.Clients)
            {
                lock (receiver.Socket)
                {
                    //clear the users current input and print
                    //this will keep the chat feeling async and easier to read
                    receiver.Socket.Send(Encoding.ASCII.GetBytes("\x1b[2K"));//clear line
                    receiver.Socket.Send(Encoding.ASCII.GetBytes("\r"));//move cursor to start of line
                    
                    receiver.Socket.Send(byteMsg);

                    char soundNotification = (char)7;//makes a sound letting user know chatroom has new dialog
                    //reprint the users input
                    receiver.Socket.Send(Encoding.ASCII.GetBytes(soundNotification + "\r=> "));
                    receiver.Socket.Send(receiver.CurrentBytesSentWithoutNewLine.ToArray());
                }
                
            }
        }
    }
}
