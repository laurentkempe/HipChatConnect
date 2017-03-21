using System.Collections.Generic;

namespace HipChatConnect.Core.Models
{
    public class TeamCityConfigurationViewModel
    {
        public TeamCityConfigurationViewModel()
        {
            BuildConfigurations = new List<BuildConfigurationViewModel>(5);
        }

        public string ServerUrl { get; set; }

        public List<BuildConfigurationViewModel> BuildConfigurations { get; set; }

        public string JwtToken { get; set; }
    }
}

public class BuildConfigurationViewModel
{
    public string BuildConfigurationIds { get; set; }

    public double MaxWaitDurationInMinutes { get; set; }
}