using System;
using System.Collections.Generic;
using System.Text;

namespace MattsChat
{
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
            this.BroadCastMessage(Responses.NewClientEntersRoomMsg(client));

            this.Clients.Add(client);
        }

        public void ClientLeave(Client client)
        {
            //remove client first so we can tailor message
            this.Clients.Remove(client);

            this.BroadCastMessage(Responses.GetResponseBytes(" * user has left chat: " + client.Nickname));

            byte[] clientMsg = Encoding.ASCII.GetBytes("<= * user has left chat: " + client.Nickname + " (** this is you)/r/n");
            client.Socket.Send(clientMsg);
        }

        public void BroadCastMessage(byte[] byteMsg)
        {
            foreach (Client receiver in this.Clients)
            {
                receiver.Socket.Send(byteMsg);
            }
        }
    }
}
