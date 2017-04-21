﻿using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HipChatConnect.Controllers.Listeners.Github.Models;
using HipChatConnect.Controllers.Listeners.TeamCity;

namespace HipChatConnect.Controllers.Listeners.Github
{
    public class GithubAggregator
    {
        private IHipChatRoom Room { get; }

        public GithubAggregator(IHipChatRoom room)
        {
            Room = room;
        }

        public async Task Handle(GithubPushNotification notification)
        {
            await SendTeamsInformationAsync(notification);

        }

        private async Task SendTeamsInformationAsync(GithubPushNotification notification)
        {
            var githubModel = notification.GithubModel;

            (var title, var text) = BuildMessage(githubModel);

            var cardData = new SuccessfulTeamsActivityCardData
            {
                Title = title,
                Text = text
            };

            await Room.SendTeamsActivityCardAsync(cardData);
        }

        private static (string Title, string Text) BuildMessage(GithubModel model)
        {
            var branch = model.Ref.Replace("refs/heads/", "");
            var authorNames = model.Commits.Select(c => c.Author.Name).Distinct();


            var title = $"**{string.Join(", ", authorNames)}** committed on [{branch}]({model.Repository.HtmlUrl + "/tree/" + branch})";

            var stringBuilder = new StringBuilder();

            foreach (var commit in model.Commits)
            {
                stringBuilder.Append($@"* {commit.Message} [{commit.Id.Substring(0, 11)}]({commit.Url})");
            }

            return (title, stringBuilder.ToString());
        }
    }
}