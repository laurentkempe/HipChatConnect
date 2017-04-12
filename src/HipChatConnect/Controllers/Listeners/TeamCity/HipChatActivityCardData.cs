using System;

namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    public class HipChatActivityCardData
    {
        public HipChatActivityCardData()
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

    public class TeamCityHipChatActivityCardData : HipChatActivityCardData
    {
        public TeamCityHipChatActivityCardData(string baseUrl)
        {
            IconUrl = $"{baseUrl}/nubot/TC_activity.png";
        }
    }

    public class SuccessfulTeamCityHipChatBuildActivityCardData : TeamCityHipChatActivityCardData
    {
        public SuccessfulTeamCityHipChatBuildActivityCardData(string baseUrl) : base(baseUrl)
        {
            ActivityIconUrl = $"{baseUrl}/nubot/TC_activity_success.png";
        }
    }

    public class FailedTeamCityHipChatBuildActivityCardData : TeamCityHipChatActivityCardData
    {
        public FailedTeamCityHipChatBuildActivityCardData(string baseUrl) : base(baseUrl)
        {
            ActivityIconUrl = $"{baseUrl}/nubot/TC_activity_failure.png";
        }
    }

    public class TeamsActivityCardData
    {
        public string Title { get; set; }
        public string Text { get; set; }
        public string Color { get; set; }

        public string Json => $@"
            {{
                ""title"": ""{Title}"",
                ""text"": ""{Text}"",
                ""themeColor"": ""{Color}""
            }}";
    }

    public class SuccessfulTeamsActivityCardData : TeamsActivityCardData
    {
        public SuccessfulTeamsActivityCardData()
        {
            Color = "00FF00";
        }
    }

    public class FailedTeamsActivityCardData : TeamsActivityCardData
    {
        public FailedTeamsActivityCardData()
        {
            Color = "FF0000";
        }
    }
}