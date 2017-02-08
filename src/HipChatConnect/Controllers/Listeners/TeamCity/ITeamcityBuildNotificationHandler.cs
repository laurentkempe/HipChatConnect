using System;

namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    public interface ITeamcityBuildNotificationHandler
    {
        event EventHandler<TeamcityBuildNotification> NotificationReceived;
    }
}