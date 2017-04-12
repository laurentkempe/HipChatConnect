using System.Threading.Tasks;

namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    public interface IHipChatRoom
    {
        Task SendMessageAsync(MessageData messageData, string oauthId);

        Task SendActivityCardAsync(HipChatActivityCardData hipChatActivityCardData, string oauthId);

        Task SendTeamsActivityCardAsync(TeamsActivityCardData teamsActivityCardData);
    }
}