﻿using GithubClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace GithubClient
{
    public class Client
    {
        private readonly RestClient client;

        const string auth_scheme = "token";
        public string AccessToken
        {
            get
            {
                if (client.DefaultHeaders.Authorization?.Scheme == auth_scheme)
                {
                    return client.DefaultHeaders.Authorization?.Parameter;
                }

                return null;
            }
            set
            {

                if(value == null)
                {
                    client.DefaultHeaders.Authorization = null;
                }
                else
                {
                    client.DefaultHeaders.Authorization = new AuthenticationHeaderValue(auth_scheme, value);
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
            var req = new RestRequest($"/search/issues", HttpMethod.Get);
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


        public Task<File> FileContents(string owner, string repo, string path, string sha = null)
        {
            var req = new RestRequest($"/repos/{owner}/{repo}/contents/{path.TrimStart('/'):raw}", HttpMethod.Get);
            if (!string.IsNullOrWhiteSpace(sha))
            {
                req.AddQueryString("ref", sha);
            }
            return req.ExecuteWithAsync<File>(client);
        }

        public Task<OAuthAccessToken> CreateAccessToken(string clientId, string clientSecret, string code)
        {
            var req = new RestRequest($"https://github.com/login/oauth/access_token", HttpMethod.Post);
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
            var req = new RestRequest($"/user");
            return client.ExecuteAsync<User>(req);
        }

        public Task<PullRequest> PullRequest(string owner, string repo, int pullRequestNumber)
        {
            return
                new RestRequest($"/repos/{owner}/{repo}/pulls/{pullRequestNumber}", HttpMethod.Get)
                    .UpateHeaders(h => {
                        h.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.VERSION.html+json"));
                    })
                    .ExecuteWithAsync<PullRequest>(client);
        }

        public Task<IEnumerable<Issue>> Isssue(string owner, string repo, int issueNumber)
        {

            throw new NotImplementedException();
        }
        public Task<IEnumerable<Commit>> Commits(string owner, string repo, int pullRequestNumber)
        {
            return
                new RestRequest($"/repos/{owner}/{repo}/pulls/{pullRequestNumber}/commits", HttpMethod.Get)
                    .UpateHeaders(h => {
                        h.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.VERSION.html+json"));
                    })
                    .ExecuteWithAsync<IEnumerable<Commit>>(client);
        }
        public Task<IEnumerable<CommitFile>> Files(string owner, string repo, int pullRequestNumber)
        {
            return
                  new RestRequest($"/repos/{owner}/{repo}/pulls/{pullRequestNumber}/files", HttpMethod.Get)
                      .ExecuteWithAsync<IEnumerable<CommitFile>>(client);
        }
        public Task<IEnumerable< CommitComment>> FileComments(string owner, string repo, int pullRequestNumber)
        {
            return
                new RestRequest($"/repos/{owner}/{repo}/pulls/{pullRequestNumber}/comments", HttpMethod.Get)
                    .UpateHeaders(h => {
                        h.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.VERSION.html+json"));
                    })
                    .ExecuteWithAsync<IEnumerable<CommitComment>>(client);
        }
        public Task<IEnumerable<Comment>> Comments(string owner, string repo, int issueNumber)
        {
            return
                new RestRequest($"/repos/{owner}/{repo}/issues/{issueNumber}/comments", HttpMethod.Get)
                    .UpateHeaders(h => {
                        h.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.VERSION.html+json"));
                    })
                    .ExecuteWithAsync<IEnumerable<Comment>>(client);
        }
        public Task<IEnumerable<Event>> Events(string owner, string repo, int issueNumber)
        {
            return
                new RestRequest($"/repos/{owner}/{repo}/issues/{issueNumber}/events", HttpMethod.Get)
                    .UpateHeaders(h => {
                        h.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.VERSION.html+json"));
                    })
                    .ExecuteWithAsync<IEnumerable<Event>>(client);
        }

        public async Task<IEnumerable<Event>> Timeline(string owner, string repo, int issueNumber)
        {
            IEnumerable<Event> allResults = Enumerable.Empty<Event>();
            var nextUrl = $"/repos/{Uri.EscapeUriString(owner)}/{Uri.EscapeUriString(repo)}/issues/{issueNumber}/timeline";
            int counter = 0;
            while (nextUrl != null && counter < 10)
            {
                counter++;
                //get top 10 pages
                var response = await
                    new RestRequest($"{nextUrl:raw}", HttpMethod.Get)
                        .UpateHeaders(h =>
                        {
                        // h.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.VERSION.html+json"));
                        h.Accept.Clear();
                            h.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.mockingbird-preview"));
                        })
                        .ExecuteWithAsync(client);


                var responseContent = await response.Content.ReadAsStringAsync();

                var pageOfResults = JsonConvert.DeserializeObject<IEnumerable<Event>>(responseContent, client.JsonSerializerSettings);
                allResults = allResults.Union(pageOfResults);

                IEnumerable<string> links;
                if (response.Headers.TryGetValues("Link", out links))
                {
                    nextUrl = links.First().Split(',').Select(x => x.Split(';')).Where(x => x[1].Contains("\"next\"")).Select(x => x[0].Substring(1, x[0].Length - 2)).FirstOrDefault();
                }
                else { nextUrl = null; }
            }
            return allResults.ToList();
        }
    }
}
