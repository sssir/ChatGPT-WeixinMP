using ChatGPT_Weixin.Core.ChatGPT;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ChatGPT_Weixin.Core.Services
{
    public class ChatGPTService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IChatGPTClient _chatGPT;
        private readonly ILogger<ChatGPTService> _logger;

        public ChatGPTService(
            IMemoryCache memoryCache,
            IChatGPTClient chatGPT,
            ILogger<ChatGPTService> logger)
        {
            _memoryCache = memoryCache;
            _chatGPT = chatGPT;
            _logger = logger;
        }



        public async Task<string> SendAsync(string userName, string messageId, string text, CancellationToken cancellationToken)
        {
            string userContextKey = $"user_context_{ userName}";
            UserContext userContext = _memoryCache.GetOrCreate(userContextKey, entry =>
            {
                _logger.LogDebug("创建用户上下文：{uerName}", userName);
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

                return new UserContext
                {
                    ConversationId = Guid.NewGuid().ToString(),
                    UserName = userName
                };
            })!;

            if (text.Contains("重置"))
            {
                _memoryCache.Remove(userContextKey);
                return "系统消息：上下文已重置。";
            }

            if (text.Trim() == "继续" && userContext.FaildMessageId != null)
            {
                _logger.LogDebug("继续处理为返回的消息，FaildMessageId：{faildMessageId}", userContext.FaildMessageId);
                messageId = userContext.FaildMessageId;
            }

            var key = $"message_processor_{messageId}";
            if (_memoryCache.TryGetValue<MessageTaskCache>(key, out var chatGPTProcessor))
            {
                chatGPTProcessor!.RetryCount++;
                _logger.LogDebug("当前消息为重试消息，重试次数：{retryCount}", chatGPTProcessor.RetryCount);
            }
            else
            {
                _logger.LogDebug("发起新消息");
                chatGPTProcessor = new MessageTaskCache
                {
                    ChatMessageTask = _chatGPT.SendMessageAsync(text, new SendMessageOptions
                    {
                        ConversationId = userContext.ConversationId,
                        ParentMessageId = userContext.LatestMessageId
                    })
                };
                _memoryCache.Set(key, chatGPTProcessor, TimeSpan.FromDays(1));
            }

            try
            {
                var result = await chatGPTProcessor.ChatMessageTask.WaitAsync(TimeSpan.FromSeconds(4.5));
                userContext.LatestMessageId = result.ParentMessageId;
                userContext.FaildMessageId = null;
                return result.Text.Trim();
            }
            catch (TimeoutException)
            {
                _logger.LogDebug("已重试次数：{retryCount}", chatGPTProcessor.RetryCount);
                if (chatGPTProcessor.RetryCount < 2)
                {
                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
                else
                {
                    _logger.LogDebug("超过重试次数：{retryCount}", chatGPTProcessor.RetryCount);
                    chatGPTProcessor.RetryCount = 0;
                    userContext.FaildMessageId = messageId;
                    return "系统消息：ChatGPT回复时间大于公众号限制，请稍后回复“继续”获取结果。";
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "出现异常");
                throw;
            }
        }
    }
}
