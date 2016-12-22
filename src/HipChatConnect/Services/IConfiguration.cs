namespace HipChatConnect.Services
{
    public interface IConfiguration<out T>
    {
        string OAuthId { get; }

        T Data { get; }
    }
}