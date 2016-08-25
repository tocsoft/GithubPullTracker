using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubClient.Models
{
    public class WebhokSettings
    {
        public string type { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public bool active { get; set; }
        public string[] events { get; set; }
        public Config config { get; set; }
        public DateTime updated_at { get; set; }
        public DateTime created_at { get; set; }
        public string url { get; set; }
        public string test_url { get; set; }
        public string ping_url { get; set; }
        public Last_Response last_response { get; set; }
    }

    public class Config
    {
        public string content_type { get; set; }
        public string insecure_ssl { get; set; }
        public string secret { get; set; }
        public string url { get; set; }
    }

    public class Last_Response
    {
        public int code { get; set; }
        public string status { get; set; }
        public string message { get; set; }
    }

}
