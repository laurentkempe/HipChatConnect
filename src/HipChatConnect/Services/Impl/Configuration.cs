namespace HipChatConnect.Services.Impl
{
    public class Configuration<T> : IConfiguration<T>
    {
        public Configuration(string oauthId, T data)
        {
            OAuthId = oauthId;
            Data = data;
        }

        public string OAuthId { get; }

        public T Data { get; }
    }
}