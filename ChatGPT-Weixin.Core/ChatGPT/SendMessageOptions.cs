using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ChatGPT_Weixin.Core.ChatGPT
{
    public class SendMessageOptions
    {
        public string? ConversationId { get; set; }

        public string? ParentMessageId { get; set; }


        public TimeSpan? Timeout { get; set; }


    }
}
