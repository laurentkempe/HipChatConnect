using System.Threading.Tasks;

namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    internal interface IHipChatRoom
    {
        Task SendMessageAsync(string msg);
    }
}