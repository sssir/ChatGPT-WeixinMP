using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatGPT_Weixin.Core.ChatGPT
{
    public interface IChatGPTClient
    {
        Task<ChatMessage> SendMessageAsync(string text, SendMessageOptions? opts = null);
    }
}
