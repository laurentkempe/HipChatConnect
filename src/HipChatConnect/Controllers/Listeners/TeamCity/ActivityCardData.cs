using System;

namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    public class ActivityCardData
    {
        public ActivityCardData()
        {
            Id = Guid.NewGuid().ToString();
        }

        private string Id { get; }

        public string ActivityIconUrl { get; set; }
        public string ActivityHtml { get; set; }

        public string Title { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public string IconUrl { get; set; }

        public string Json => $@"
            {{
                ""style"": ""application"",
                ""url"": ""{Url}"",
                ""id"": ""{Id}"",
                ""title"": ""{Title}"",
                ""description"": ""{Description}"",
                ""icon"": {{
                ""url"": ""{IconUrl}""
                }},
                ""attributes"": [                
                ],
                ""activity"": {{
                ""icon"": ""{ActivityIconUrl}"",
                ""html"": ""{ActivityHtml}""
                }}
            }}";

    }

    public class TeamCityActivityCardData : ActivityCardData
    {
        public TeamCityActivityCardData(string baseUrl)
        {
            IconUrl = $"{baseUrl}/nubot/TC_activity.png";
        }
    }

    public class SuccessfulTeamCityBuildActivityCardData : TeamCityActivityCardData
    {
        public SuccessfulTeamCityBuildActivityCardData(string baseUrl) : base(baseUrl)
        {
            ActivityIconUrl = $"{baseUrl}/nubot/TC_activity_success.png";
        }
    }

    public class FailedTeamCityBuildActivityCardData : TeamCityActivityCardData
    {
        public FailedTeamCityBuildActivityCardData(string baseUrl) : base(baseUrl)
        {
            ActivityIconUrl = $"{baseUrl}/nubot/TC_activity_failure.png";
        }
    }
}