﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubClient.Models
{

    public class OAuthAccessToken
    {
        public string access_token { get; set; }
        public string scope { get; set; }
        public string token_type { get; set; }
    }

}
