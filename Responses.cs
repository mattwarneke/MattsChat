using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MattsChat
{
    public static class Responses
    {
        public static byte[] NewClientEntersRoomMsg(Client client)
        {
            return GetResponseBytesWithLeadingNewLine("* new user joined chat: " + client.Nickname);
        }

        public static byte[] EndOfListBytes()
        {
            return GetResponseBytes("end of list.");
        }
       
        public static byte[] GetResponseBytesWithLeadingNewLine(string msg)
        {
            return Encoding.ASCII.GetBytes(Environment.NewLine + "<= " + msg + Environment.NewLine + "=>");
        }
        public static byte[] GetResponseBytes(string msg)
        {
            return Encoding.ASCII.GetBytes("<= " + msg + Environment.NewLine + "=>");
        }
    }
}
