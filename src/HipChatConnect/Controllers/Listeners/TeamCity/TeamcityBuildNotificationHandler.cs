using System;
using System.Threading.Tasks;
using MediatR;

namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    public class TeamcityBuildNotificationHandler
        : IAsyncNotificationHandler<TeamcityBuildNotification>, ITeamcityBuildNotificationHandler
    {
        public async Task Handle(TeamcityBuildNotification notification)
            => await Task.Run(() => NotificationReceived?.Invoke(this, notification));

        public event EventHandler<TeamcityBuildNotification> NotificationReceived;
    }
}