using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GithubPullTracker.Models
{
    public class GithubUser
    {
        public string UserName { get; set; }
        public string AuthKey { get; set; }
        public string ProfileUrl { get; set; }
        public string AvartarUrl { get; set; }
    }
}