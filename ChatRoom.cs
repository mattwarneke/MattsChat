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
            this.Clients = new List<IClient>();
            this.UniqueId = Guid.NewGuid();
        }

        public Guid UniqueId { get; private set; }

        public string Name { get; private set; }

        public ICollection<IClient> Clients { get; private set; }

        public void ClientJoin(IClient client)
        {
            //broadcast entry before client joins, so we don't have to exclude them
            this.BroadCastMessage(new OutboundMessage("* new user joined chat: " + client.Nickname));

            this.Clients.Add(client);
        }

        public void ClientLeave(IClient client)
        {
            //remove client first so we can tailor message
            this.Clients.Remove(client);

            client.LeaveChatroom();

            try
            {
                this.BroadCastMessage(new OutboundMessage(
                    "* user has left chat: " + client.Nickname));

                lock (client)
                {
                    //we use a blocking mode send, no async on the outgoing
                    //since this is primarily a multithreaded application, shouldn't cause problems to send in blocking mode
                    client.Send(new OutboundMessage(
                        "* user has left chat: " + client.Nickname + " (** this is you)"));
                }
            }
            catch (SocketException)
            {
                //Something went horribly wrong
                return;
            }
        }

        public void BroadCastMessage(OutboundMessage byteMsg)
        {
            foreach (IClient receiver in this.Clients)
            {
                lock (receiver)
                {
                    //clear the users current input and print
                    //this will keep the chat feeling async and easier to read
                    receiver.Send(Encoding.ASCII.GetBytes("\x1b[2K"));//clear line
                    receiver.Send(Encoding.ASCII.GetBytes("\r"));//move cursor to start of line
                    
                    receiver.Send(byteMsg);

                    char soundNotification = (char)7;//makes a sound letting user know chatroom has new dialog
                    //reprint the users input
                    receiver.Send(Encoding.ASCII.GetBytes(soundNotification + "\r=> "));
                    receiver.Send(receiver.CurrentBytesSentWithoutNewLine.ToArray());
                }
                
            }
        }
    }
}
