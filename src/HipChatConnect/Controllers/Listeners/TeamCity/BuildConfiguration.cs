namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    public class BuildConfiguration
    {
        public BuildConfiguration()
        {
            MaxWaitDurationInMinutes = 0.0;
            BuildConfigurationIds = string.Empty;
        }

        public double MaxWaitDurationInMinutes { get; set; }

        public string BuildConfigurationIds { get; set; }
    }
}