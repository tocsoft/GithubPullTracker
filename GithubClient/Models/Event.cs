using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubClient.Models
{

    public class Event
    {
        public int id { get; set; }
        public string url { get; set; }
        public User actor { get; set; }
        public string _event { get; set; }
        public string commit_id { get; set; }
        public string commit_url { get; set; }
        public DateTime created_at { get; set; }
    }

}
