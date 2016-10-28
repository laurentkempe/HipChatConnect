using MediatR;

namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    public class TeamcityBuildNotificationHandler : INotificationHandler<TeamcityBuildNotification>
    {
        private TeamCityAggregator Aggregator { get; }

        public TeamcityBuildNotificationHandler(TeamCityAggregator aggregator)
        {
            Aggregator = aggregator;
        }

        public void Handle(TeamcityBuildNotification notification)
        {
            Aggregator.Handle(notification);
        }
    }
}