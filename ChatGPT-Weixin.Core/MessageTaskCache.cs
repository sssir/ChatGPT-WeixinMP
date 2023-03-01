using ChatGPT_Weixin.Core.ChatGPT;

namespace ChatGPT_Weixin.Core
{
    public class MessageTaskCache
    {
        public int RetryCount { get; set; }

        public Task<ChatMessage> ChatMessageTask { get; set; } = default!;
    }
}
