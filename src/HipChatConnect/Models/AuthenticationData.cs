using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nubot.Plugins.Samples.HipChatConnect.Models;

namespace HipChatConnect.Models
{
    public class AuthenticationData
    {
        public InstallationData InstallationData { get; set; }

        public ExpiringAccessToken Token { get; set; }
    }
}
