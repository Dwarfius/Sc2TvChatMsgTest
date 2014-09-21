using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sc2TvChatMsgTest
{
    class Message
    {
        public string id;
        public string uid;
        public string name;
        public string message;
        public DateTime date;
        public string channelId;
        public int[] roleIds;
        public string role;

        public string GetString()
        {
            return name + ": " + message;
        }

        public int GetId()
        {
            return int.Parse(id);
        }
    }
}
