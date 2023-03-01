using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatGPT_Weixin.Core.ChatGPT
{
    public class ChatMessage
    {
        public string Id { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public Role Role { get; set; }

        public string? ParentMessageId { get; set; }

        public string? ConversationId { get; set; }

    }

    public enum Role
    {
        User,

        Assistent
    }
}
