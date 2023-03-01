namespace ChatGPT_Weixin.Core
{
    internal class UserContext
    {
        public string UserName { get; set; } = string.Empty;

        public string ConversationId { get; set; } = string.Empty;

        public string? FaildMessageId { get; set; }

        public string? LatestMessageId { get; set; }
    }
}
