using System.Xml.Serialization;

namespace ChatGPT_Weixin.Web.Models
{
    [XmlRoot("xml")]
    public class WeixinMessage
    {
        public string ToUserName { get; set; } = string.Empty;

        public string FromUserName { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public string MsgId { get; set; } = string.Empty;

        public string MsgType { get; set; } = string.Empty;
    }
}
