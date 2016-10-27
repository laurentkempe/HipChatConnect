using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using HipChatConnect.Controllers.Listeners.TeamCity.Models;
using HipChatConnect.Core.ReactiveExtensions;
using MediatR;

namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    internal class TeamcityBuildNotificationHandler : INotificationHandler<TeamcityBuildNotification>
    {
        private IHipChatRoom Room { get; }
        private readonly Subject<TeamCityModel> _subject;
        private readonly int _expectedBuildCount;

        public TeamcityBuildNotificationHandler(IHipChatRoom room)
        {
            Room = room;
            _subject = new Subject<TeamCityModel>();

            _expectedBuildCount = 4;

            var maxWaitDuration = TimeSpan.FromMinutes(10.0);

            var buildsPerBuildNumber = _subject.GroupBy(model => model.build.buildNumber);

            buildsPerBuildNumber.Subscribe(grp => grp.BufferUntilInactive(maxWaitDuration, Scheduler, _expectedBuildCount).Take(1).Subscribe(
                async list => await SendNotificationAsync(list)));
        }

        protected virtual IScheduler Scheduler => DefaultScheduler.Instance;

        public void Handle(TeamcityBuildNotification notification)
        {
            _subject.OnNext(notification.TeamCityModel);
        }
        private async Task SendNotificationAsync(IList<TeamCityModel> buildStatuses)
        {
            bool notify;
            var message = new TeamCityMessageBuilder(_expectedBuildCount).BuildMessage(buildStatuses, out notify);

            //todo add color of the line https://www.hipchat.com/docs/api/method/rooms/message
            //todo Background color for message. One of "yellow", "red", "green", "purple", "gray", or "random". (default: yellow)

            await Room.SendMessageAsync(message);
        }
    }
}