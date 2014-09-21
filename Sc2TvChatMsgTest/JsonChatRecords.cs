using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sc2TvChatMsgTest
{
    class JsonChatRecords
    {
        public List<Message> messages = new List<Message>();

        public Message GetLastMessage()
        {
            return messages[0];
        }
    }
}
