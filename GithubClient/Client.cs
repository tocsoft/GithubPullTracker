using GithubClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubClient
{
    public class Client
    {
        public Client()
        {
            //send application/vnd.github.VERSION.html+json  as Accept header to recieve markdown prerendered
        }


        public Task<User> CurrentUser()
        {
            throw new NotImplementedException();
        }
        public Task<PullRequest> PullRequest(string owner, string repo, int pullRequestNumber)
        {

            throw new NotImplementedException();
        }
        public Task<Issue> Isssue(string owner, string repo, int issueNumber)
        {

            throw new NotImplementedException();
        }
        public Task<Commit> Commits(string owner, string repo, int pullRequestNumber)
        {

            throw new NotImplementedException();
        }
        public Task<IEnumerable<CommitFile>> Files(string owner, string repo, int pullRequestNumber)
        {
            throw new NotImplementedException();
        }
        public Task<IEnumerable< CommitComment>> FileComments(string owner, string repo, int pullRequestNumber)
        {
            throw new NotImplementedException();
        }
        public Task<IEnumerable<Commit>> Comments(string owner, string repo, int issueNumber)
        {
            throw new NotImplementedException();
        }
        public Task<IEnumerable<Event>> Events(string owner, string repo, int issueNumber)
        {
            throw new NotImplementedException();
        }
    }
}
