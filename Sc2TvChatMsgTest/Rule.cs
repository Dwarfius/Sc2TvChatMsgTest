using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sc2TvChatMsgTest
{
    class Rule
    {
        public string Command { get; set; }
        public string Reply { get; set; }

        public string GetPreParsedReply(string name)
        {
            return Reply.Replace("#Name", name).Replace("#Time", DateTime.Now.ToShortTimeString());
        }
    }
}
