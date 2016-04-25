namespace MattsChat
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public interface IClient
    {
        void Send(OutboundMessage message);

        void Send(byte[] message);

        void Listen();

        string Nickname { get; }

        bool HasNickname { get; }

        void SetNickname(string nickName);

        bool IsInChatroom { get; }

        Guid ChatRoomUniqueId { get; }

        void EnterChatroom(Guid chatroomUniqueId);

        void LeaveChatroom();

        void Disconnect();

        List<byte> CurrentBytesSentWithoutNewLine { get; }

        void AppendBytes(byte[] bytesToAppend);

        void ClearBytes();
    }

    public abstract class Baseclient : IClient
    {
        public Baseclient()
        {
            this.ChatRoomUniqueId = Guid.Empty;
            this.CurrentBytesSentWithoutNewLine = new List<byte>();
        }

        public string Nickname { get; private set; }

        public Guid ChatRoomUniqueId { get; private set; }

        public List<byte> CurrentBytesSentWithoutNewLine { get; private set; } 

        public void SetNickname(string nickName)
        {
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
                return !this.ChatRoomUniqueId.Equals(Guid.Empty);
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
            this.ChatRoomUniqueId = chatroomUniqueId;
        }

        public void LeaveChatroom()
        {
            this.ChatRoomUniqueId = Guid.Empty;
        }

        public virtual void Send(OutboundMessage message)
        {
            throw new NotImplementedException();
        }

        public virtual void Send(byte[] message)
        {
            throw new NotImplementedException();
        }

        public virtual void Listen()
        {
            throw new NotImplementedException();
        }

        public virtual void Disconnect()
        {
            throw new NotImplementedException();
        }
    }
}
