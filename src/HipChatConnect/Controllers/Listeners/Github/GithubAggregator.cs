using System.Linq;
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


            var title = string.Format(
                    "<b>{0}</b> committed on <a href='{1}'>{2}</a><br/>",
                    string.Join(", ", authorNames),
                    model.Repository.HtmlUrl + "/tree/" + branch,
                    branch
                );

            var stringBuilder = new StringBuilder();

            foreach (var commit in model.Commits)
            {
                stringBuilder
                    .AppendFormat(
                        @"- {0} (<a href='{1}'>{2}</a>)<br/>",
                        commit.Message,
                        commit.Url,
                        commit.Id.Substring(0, 11));
            }

            return (title, stringBuilder.ToString());
        }
    }
}