using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MattsChat
{
    using System.Dynamic;
    using System.Net.Sockets;

    public class Client
    {
        public Client(Socket socket)
        {
            this.Socket = socket;
            this.CurrentBytesSentWithoutNewLine = new List<byte>();
            this.chatRoomUniqueId = Guid.Empty;
        }

        //TODO: Client terminal type
        public Socket Socket { get; private set; }

        public string Nickname { get; private set; }

        public List<byte> CurrentBytesSentWithoutNewLine { get; private set; }

        public Guid chatRoomUniqueId { get; private set; }

        public void SetNickname(string nickName)
        {
            //ToDo: Alphanumberic only
            this.Nickname = nickName;
        }

        public bool HasNickname
        {
            get
            {
                return !string.IsNullOrEmpty(this.Nickname);
            }
        }

        public bool IsInChatroom
        {
            get
            {
                return !chatRoomUniqueId.Equals(Guid.Empty);
            }
        }

        public void AppendBytes(byte[] bytesToAppend)
        {
            this.CurrentBytesSentWithoutNewLine.AddRange(bytesToAppend);
        }

        public void ClearBytes()
        {
            this.CurrentBytesSentWithoutNewLine.Clear();
        }

        public void EnterChatroom(Guid chatroomUniqueId)
        {
            this.chatRoomUniqueId = chatroomUniqueId;
        }
    }
}
