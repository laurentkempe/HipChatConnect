using System.Threading.Tasks;

namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    public interface IHipChatRoom
    {
        Task SendMessageAsync(string msg, string oauthId);
    }
}