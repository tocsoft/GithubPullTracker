using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GithubPullTracker.DataStore
{
    public class WebhookReceiverConfig
    {
        public string AuthorizationKey { get; }
        public bool IsEnabled { get; }
    }
}