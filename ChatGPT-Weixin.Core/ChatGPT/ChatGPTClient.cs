using OpenAI_API.Completions;
using OpenAI_API.Models;
using System.Text.RegularExpressions;

namespace ChatGPT_Weixin.Core.ChatGPT
{
    public class ChatGPTClient : IChatGPTClient
    {

        protected readonly int _maxModelTokens;
        protected readonly int _maxResponseTokens;

        protected readonly string _userLabel = "User";
        protected readonly string _assistantLabel = "ChatGPT";

        protected readonly string _endToken = "<|im_end|>";
        protected readonly string _sepToken = "<|im_sep|>";
        protected readonly Model CHATGPT_MODEL = Model.DavinciText;
        protected readonly string[] _stop;


        protected readonly Dictionary<string, ChatMessage> _messageStore;
        protected readonly GPT_3_Encoder_Sharp.Encoder _encoder = GPT_3_Encoder_Sharp.Encoder.Get_Encoder();
        private readonly string _apiKey;

        public ChatGPTClient(string apiKey)
        {
            _apiKey = apiKey;
            _messageStore = new Dictionary<string, ChatMessage>();
            _maxModelTokens = 4096;
            _maxResponseTokens = 1000;

            if (IsChatGPTModel)
            {
                _endToken = "<|im_end|>";
                _sepToken = "<|im_sep|>";
                _stop = new string[] { _endToken, _sepToken };
            }
            else
            {
                _endToken = "<|endoftext|>";
                _sepToken = _endToken;
                _stop = new string[] { _endToken };

            }
        }


        public async Task<ChatMessage> SendMessageAsync(string text, SendMessageOptions? opts = default)
        {
            var conversationId = opts?.ConversationId ?? Guid.NewGuid().ToString();
            var parentMessageId = opts?.ParentMessageId;

            var messageId = Guid.NewGuid().ToString();
            var timeout = opts?.Timeout ?? default;

            var message = new ChatMessage
            {
                Role = Role.User,
                Id = messageId,
                ParentMessageId = parentMessageId,
                ConversationId = conversationId,
                Text = text,
            };

            await DefaultUpsertMessageAsync(message);

            (string prompt, int maxTokens) = await BuildPromptAsync(text, opts);
            var api = new OpenAI_API.OpenAIAPI(_apiKey);

            CompletionResult? response = default;

            var task = Task.Run(async () =>
            response = await api.Completions.CreateCompletionAsync(new CompletionRequest(prompt, model: CHATGPT_MODEL, max_tokens: maxTokens, temperature: 0.8, top_p: 1, presencePenalty: 1, stopSequences: new string[] { _endToken })));

            if (timeout != default && !task.Wait(timeout))
            {
                throw new TimeoutException();
            }

            response = await task;


            var result = new ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                Role = Role.Assistent,
                ParentMessageId = messageId,
                ConversationId = conversationId,
                Text = string.Empty
            };
            if (response is null)
            {
                throw new Exception("ChatGPT error");
            }
            if (string.IsNullOrWhiteSpace(response.Id) == false)
            {
                result.Id = response.Id;
            }
            if (response.Completions.Any())
            {
                result.Text = response.Completions[0].Text;
            }
            else
            {
                throw new Exception($"ChatGPT error");
            }

            await DefaultUpsertMessageAsync(message);
            return result;
        }

        protected async Task<(string prompt, int maxTokens)> BuildPromptAsync(string message, SendMessageOptions? opts)
        {
            string currentDate = DateTime.Now.ToString("yyyy-MM-dd");

            var promptPrefix = $"Instructions:\nYou are {_assistantLabel}, a large language model trained by OpenAI.\nCurrent date: {currentDate}{_sepToken}\n\n";

            var promptSuffix = $"\n\nChatGPT:\n";

            var maxNumTokans = _maxModelTokens - _maxResponseTokens;
            var parentMessageId = opts?.ParentMessageId;
            var nextPromptBody = $"User:\n\n{message}{_endToken}";
            string prompt = default!;
            int numTokens = 0;

            do
            {
                var nextPrompt = $"{promptPrefix}{nextPromptBody}{promptSuffix}";
                var nextNumTokens = await GetTokenCountAsync(nextPrompt);
                var isValidPrompt = nextNumTokens <= maxNumTokans;

                if (string.IsNullOrEmpty(prompt) == false && isValidPrompt == false)
                {
                    break;
                }

                string? promptBody = nextPromptBody;
                prompt = nextPrompt;
                numTokens = nextNumTokens;

                if (!isValidPrompt)
                {
                    break;
                }

                if (parentMessageId == null)
                {
                    break;
                }

                var parentMessage = await DefaultGetMessageById(parentMessageId);
                if (parentMessage == null)
                {
                    break;
                }

                var parentMessageRole = parentMessage.Role;
                var parentMessageRoleDesc = parentMessageRole == Role.User ? _userLabel : _assistantLabel;

                var parentMessageString = $"{parentMessageRoleDesc}:\n\n{parentMessage.Text}{_endToken}\n\n";
                nextPromptBody = $"{parentMessageString}{promptBody}";
                parentMessageId = parentMessage.ParentMessageId;
            } while (true);

            var maxTokens = Math.Max(1, Math.Min(_maxModelTokens - numTokens, _maxResponseTokens));
            return (prompt, maxTokens);
        }

        protected virtual Task<ChatMessage?> DefaultGetMessageById(string id)
        {
            _messageStore.TryGetValue(id, out var message);
            return Task.FromResult(message);
        }

        protected virtual Task DefaultUpsertMessageAsync(ChatMessage message)
        {
            _messageStore[message.Id] = message;
            return Task.CompletedTask;
        }


        protected Task<int> GetTokenCountAsync(string text)
        {
            if (IsChatGPTModel)
            {
                text = Regex.Replace(text, @"<\|im_end\|>", "<|endoftext|>");
                text = Regex.Replace(text, @"<\|im_sep\|>", "<|endoftext|>");
            }

            return Task.FromResult(_encoder.Encode(text).Count);
        }

        protected bool IsChatGPTModel =>
            CHATGPT_MODEL.ToString()!.StartsWith("text-chat") ||
            CHATGPT_MODEL.ToString()!.StartsWith("text-davinci-002-render");

    }
}
