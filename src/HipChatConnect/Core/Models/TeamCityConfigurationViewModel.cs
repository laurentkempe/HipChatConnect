namespace HipChatConnect.Core.Models
{
    public class TeamCityConfigurationViewModel
    {
        public string ServerUrl { get; set; }

        public string BuildConfigurationIds { get; set; }

        public double MaxWaitDurationInMinutes { get; set; }

        public string JwtToken { get; set; }}
}
