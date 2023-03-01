using ChatGPT_Weixin.Core.Services;
using ChatGPT_Weixin.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChatGPT_Weixin.Web.Controllers
{
    public class WXController : ControllerBase
    {
        private readonly ILogger<WXController> _logger;
        private readonly ChatGPTService _chatGPTService;

        public WXController(
            ILogger<WXController> logger,
            ChatGPTService chatGPTService)
        {
            _logger = logger;
            _chatGPTService = chatGPTService;
        }

        [HttpPost("/wx")]
        public async Task<IActionResult> Post([FromBody] WeixinMessage message, CancellationToken cancellationToken)
        {
            _logger.LogDebug("接收到请求：{@message}", message);
            var userName = message.FromUserName;

            string result;
            if (message.MsgType == "text")
            {
                try
                {
                    result = await _chatGPTService.SendAsync(userName, message.MsgId, message.Content, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("请求被取消");
                    return NoContent();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "处理异常");
                    result = "系统消息：处理异常，请联系开发人员，也就是高某。";
                }

            }
            else
            {
                result = "系统消息：暂不支持文字以外的回复。";
            }


            return Content(@$"<xml>
  <ToUserName><![CDATA[{message.FromUserName}]]></ToUserName>
  <FromUserName><![CDATA[{message.ToUserName}]]></FromUserName>
  <CreateTime>{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}</CreateTime>
  <MsgType><![CDATA[text]]></MsgType>
  <Content><![CDATA[{result}]]></Content>
</xml>", System.Net.Mime.MediaTypeNames.Text.Xml);
        }
    }
}
