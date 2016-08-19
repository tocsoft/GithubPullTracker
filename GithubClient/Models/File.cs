using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubClient.Models
{

    public class File
    {
        public string type { get; set; }
        public string encoding { get; set; }
        public int size { get; set; }
        public string name { get; set; }
        public string path { get; set; }
        public string content { get; set; }
        public string sha { get; set; }
        public string url { get; set; }
        public string git_url { get; set; }
        public string html_url { get; set; }
        public string download_url { get; set; }
        public Dictionary<string, string> _links { get; set; }
    }

}
