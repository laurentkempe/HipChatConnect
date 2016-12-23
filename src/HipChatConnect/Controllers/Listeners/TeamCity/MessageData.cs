namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    public class MessageData
    {
        public MessageData()
        {
            Color = "gray";
        }

        public string Color { get; set; }
        public string Message { get; set; }

        public string Json => $@"
        {{
            ""color"": ""{Color}"",
            ""message"": ""{Message}"",
            ""message_format"": ""html""
            }}
        }}";
    }
}