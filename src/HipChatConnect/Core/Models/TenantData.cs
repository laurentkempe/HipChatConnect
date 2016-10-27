using System.Collections.Generic;
using Nubot.Plugins.Samples.HipChatConnect.Models;

namespace HipChatConnect.Models
{
    public class TenantData
    {
        public TenantData()
        {
            Store = new Dictionary<string, string>();
        }

        public InstallationData InstallationData { get; set; }

        public ExpiringAccessToken Token { get; set; }

        public Dictionary<string, string> Store { get; set; }
    }
}
