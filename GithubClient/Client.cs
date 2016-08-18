using GithubClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace GithubClient
{
    public class Client
    {
        private readonly RestClient client;

        const string auth_prefix = "token ";
        public string AccessToken
        {
            get
            {
                var row = client.GetHeader("Authorization");
                if (row != null && row.StartsWith(auth_prefix))
                {
                    return row.Substring(auth_prefix.Length);
                }
                return null;
            }
            set
            {
                if(value == null)
                {
                    client.RemoveHeader("Authorization");
                }else
                {
                    client.SetHeader("Authorization", auth_prefix+value);
                }
            }
        }

        public Client(string useragent)
        {
            this.client = new RestClient()
            {
                BaseUrl = "https://api.github.com",
                UserAgent = useragent,
            };
            //send application/vnd.github.VERSION.html+json  as Accept header to recieve markdown prerendered
        }

        //public Task<SearchResult> SearchPullRequestsByRepo(int page, int pagesize, RequestState state, SortOrder order, SortOrderDirection direction, string owner, string terms)
        //{
        //    return SearchPullRequestsByRepo(page, pagesize, state, order, direction, owner, null, terms);
        //}
        //     public Task<SearchResult> SearchPullRequestsByRepo(int page, int pagesize, RequestState state, SortOrder order, SortOrderDirection direction, string terms)
        //{
        //    var req = new RestRequest("/search/issues", HttpMethod.Get);
        //    StringBuilder query = new StringBuilder();
        //    query.Append("type:pr");
        //    query.AppendFormat($" is:{state.ToString().ToLower()}");
            
            

        //    if (!string.IsNullOrWhiteSpace(repository))
        //    {
        //        query.AppendFormat($" repo:{owner}/{repository}");
        //    }
        //    else
        //    {
        //        query.AppendFormat($" user:{owner}");
        //    }
            
        //    if (order != SortOrder.bestmatch)
        //    {
        //        req.AddQueryString("sort", order.ToString());
        //    }
        //    if (direction != SortOrderDirection.Decending)
        //    {
        //        req.AddQueryString("order", "asc");
        //    }
        //    req.AddQueryString("per_page", pagesize);
        //    if (page != 1)
        //    {
        //        req.AddQueryString("page", page);
        //    }


        //    if (!string.IsNullOrWhiteSpace(terms))
        //    {
        //        query.Append(" ");
        //        query.Append(terms);
        //    }

        //    req.AddQueryString("q", query.ToString());
        //    return client.ExecuteAsync<SearchResult>(req);
        //}

        //public Task<IEnumerable<PullRequest>> PullRequests(string owner, string repo)
        //{
        //    var req = new RestRequest("/repos/{owner}/{repo}/pull");
        //    req.AddUrlSegment("owner", owner);
        //    req.AddUrlSegment("repo", repo);
        //}

        public Task<SearchResult> SearchPullRequests(int page, int pagesize, RequestState state, SortOrder order, SortOrderDirection direction, RequestConnection? connection, string login, string terms, string owner, string repository)
        {
            var req = new RestRequest("/search/issues", HttpMethod.Get);
            StringBuilder query = new StringBuilder();
            query.Append("type:pr");
            query.AppendFormat($" is:{state.ToString().ToLower()}");
            if (connection.HasValue)
            {
                switch (connection.Value)
                {
                    case RequestConnection.Involved:
                        query.AppendFormat($" involves:{login}");
                        break;
                    case RequestConnection.Author:
                        query.AppendFormat($" author:{login}");
                        break;
                    case RequestConnection.Assigned:
                        query.AppendFormat($" assignee:{login}");
                        break;
                    default:
                        throw new InvalidOperationException("invalid connection type");
                }
            }


            if (!string.IsNullOrWhiteSpace(owner))
            {
                if (!string.IsNullOrWhiteSpace(repository))
                {
                    query.AppendFormat($" repo:{owner}/{repository}");
                }
                else
                {
                    query.AppendFormat($" user:{owner}");
                }
            }

            if (order != SortOrder.bestmatch)
            {
                req.AddQueryString("sort", order.ToString());
            }
            if (direction != SortOrderDirection.Decending)
            {
                req.AddQueryString("order", "asc");
            }
            req.AddQueryString("per_page", pagesize);
            if (page != 1)
            {
                req.AddQueryString("page", page);
            }


            if (!string.IsNullOrWhiteSpace(terms))
            {
                query.Append(" ");
                query.Append(terms);
            }

            req.AddQueryString("q", query.ToString());
            return client.ExecuteAsync<SearchResult>(req);
        }

        public Task<OAuthAccessToken> CreateAccessToken(string clientId, string clientSecret, string code)
        {
            var req = new RestRequest("https://github.com/login/oauth/access_token", HttpMethod.Post);
            req.AddParameter(new
            {
                client_id = clientId,
                client_secret = clientSecret,
                code = code
            });

            return client.ExecuteAsync<OAuthAccessToken>(req);
        }

        public Task<User> CurrentUser()
        {
            var req = new RestRequest("/user");
            return client.ExecuteAsync<User>(req);
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
