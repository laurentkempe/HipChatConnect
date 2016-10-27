using HipChatConnect.Controllers.Listeners.TeamCity.Models;
using MediatR;

namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    internal class TeamcityBuildNotification : INotification
    {
        public TeamCityModel TeamCityModel { get; }

        public TeamcityBuildNotification(TeamCityModel teamCityModel)
        {
            TeamCityModel = teamCityModel;
        }
    }
}