using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using GithubClient;
using GithubClient.Models;

namespace GithubPullTracker.Models
{
    public class UserSearchResults : SearchResults
    {
        public UserSearchResults(IEnumerable<Repo> repos, int page, int pagesize, SearchResult results, RequestState state, SortOrder order, SortOrderDirection dir, RequestConnection connection, string term) :base(page, pagesize, results, state, order, dir, connection, term) 
        {
            this.Repos = repos;
        }

        public IEnumerable<Repo> Repos { get; private set; }

        public override string GenerateUrl(int? page, RequestState? state, SortOrder? order, SortOrderDirection? dir, RequestConnection? connection, string term)
        {
            StringBuilder sb = new StringBuilder();
            
            state = state ?? State;
            order = order ?? Order;
            dir = dir ?? Direction;
            connection = connection ?? ConnectionType;
            term = term ?? SearchTerm;
            page = page ?? Page;
            if (page > 1)
            {
                sb.AppendFormat($"&page={page}");
            }
            if (state != RequestState.Open)//skip defaults
            {
                sb.AppendFormat($"&state={state}");
            }
            if (order != SortOrder.bestmatch)//skip defaults
            {
                sb.AppendFormat($"&order={order}");
            }
            if (dir != SortOrderDirection.Decending)//skip defaults
            {
                sb.AppendFormat($"&dir={dir}");
            }
            if (connection.HasValue && connection.Value != RequestConnection.Assigned)
            {
                sb.AppendFormat($"&type={connection.Value}");
            }
            return "/?"+sb.ToString().TrimStart('&');
        }
    }

    public class RepoSearchResults : SearchResults
    {
        public RepoSearchResults(Repo repo, int page, int pagesize, SearchResult results, RequestState state, SortOrder order, SortOrderDirection dir, RequestConnection connection, string term)
            : base(page, pagesize, results, state, order, dir, connection, term)
        {
            this.Repo = repo;
            
        }

        public Repo Repo { get; private set; }

        public override string GenerateUrl(int? page, RequestState? state, SortOrder? order, SortOrderDirection? dir, RequestConnection? connection, string term)
        {
            StringBuilder sb = new StringBuilder();

            state = state ?? State;
            order = order ?? Order;
            dir = dir ?? Direction;
            connection = connection ?? ConnectionType;
            term = term ?? SearchTerm;
            page = page ?? Page;
            if (page > 1)
            {
                sb.AppendFormat($"&page={page}");
            }
            if (state != RequestState.Open)//skip defaults
            {
                sb.AppendFormat($"&state={state}");
            }
            if (order != SortOrder.bestmatch)//skip defaults
            {
                sb.AppendFormat($"&order={order}");
            }
            if (dir != SortOrderDirection.Decending)//skip defaults
            {
                sb.AppendFormat($"&dir={dir}");
            }
            if (connection.HasValue && connection.Value != RequestConnection.All)
            {
                sb.AppendFormat($"&type={connection.Value}");
            }
            var qs = sb.ToString().TrimStart('&');
            if (qs.Length > 0)
            {
                qs = "?" + qs;
            }
            return "/" + Repo.owner.login + "/" + Repo.name + qs;
        }
    }

    public class OwnerSearchResults : SearchResults
    {
        public OwnerSearchResults(User owner, IEnumerable<Repo> repos, int page, int pagesize, SearchResult results, RequestState state, SortOrder order, SortOrderDirection dir, RequestConnection connection, string term)
            : base(page, pagesize, results, state, order, dir, connection, term)
        {
            this.Repos = repos;
            this.Owner = owner;
        }

        public User Owner { get; private set; }
        public IEnumerable< Repo> Repos { get; private set; }

        public override string GenerateUrl(int? page, RequestState? state, SortOrder? order, SortOrderDirection? dir, RequestConnection? connection, string term)
        {
            StringBuilder sb = new StringBuilder();

            state = state ?? State;
            order = order ?? Order;
            dir = dir ?? Direction;
            connection = connection ?? ConnectionType;
            term = term ?? SearchTerm;
            page = page ?? Page;
            if (page > 1)
            {
                sb.AppendFormat($"&page={page}");
            }
            if (state != RequestState.Open)//skip defaults
            {
                sb.AppendFormat($"&state={state}");
            }
            if (order != SortOrder.bestmatch)//skip defaults
            {
                sb.AppendFormat($"&order={order}");
            }
            if (dir != SortOrderDirection.Decending)//skip defaults
            {
                sb.AppendFormat($"&dir={dir}");
            }
            if (connection.HasValue && connection.Value != RequestConnection.All)
            {
                sb.AppendFormat($"&type={connection.Value}");
            }
            var qs = sb.ToString().TrimStart('&');
            if (qs.Length > 0)
            {
                qs = "?" + qs;
            }
            return "/" + Owner.login + qs;
        }
    }

    public abstract class SearchResults
    {
        public SearchResults(int page, int pagesize, SearchResult results, RequestState state,  SortOrder order, SortOrderDirection dir, RequestConnection? connection, string term)
        {
            SearchTerm = term;
            Page = page;
            PageSize = pagesize;
            if (state == RequestState.Open)
            {
                OpenHits = results.total_count;
            }
            if (state == RequestState.Closed)
            {
                ClosedHits = results.total_count;
            }

            PageCount = (int)Math.Ceiling(results.total_count / (float)pagesize);

            Items = results.items.Select(x => new PullRequestItem(x)).ToList();
            this.State = state;
            this.Order = order;
            this.Direction = dir;
            ConnectionType = connection;
        }
        public string GeneratePageUrl(int page)
        {
            return GenerateUrl(page, null, null, null, null, null);
        }
        public string GenerateStateUrl(RequestState state)
        {
            return GenerateUrl(1, state, null, null, null, null);
        }
        public string GenerateConnectionUrl(RequestConnection connection)
        {
            return GenerateUrl(1, null, null, null, connection, null);
        }
        public virtual string GenerateUrl(int? page, RequestState? state, SortOrder? order, SortOrderDirection? dir, RequestConnection? connection, string term)
        {
            StringBuilder sb = new StringBuilder();

            state = state ?? State;
            order = order ?? Order;
            dir = dir ?? Direction;
            connection = connection ?? ConnectionType;
            term = term ?? SearchTerm;
            page = page ?? Page;

            if (page > 1)
            {
                sb.AppendFormat($"&page={page}");
            }
            if (state != RequestState.Open)//skip defaults
            {
                sb.AppendFormat($"&state={state}");
            }
            if (order != SortOrder.bestmatch)//skip defaults
            {
                sb.AppendFormat($"&order={order}");
            }
            if (dir != SortOrderDirection.Decending)//skip defaults
            {
                sb.AppendFormat($"&dir={dir}");
            }
            if (connection.HasValue)
            {
                sb.AppendFormat($"&type={connection.Value}");
            }

            return "/?" + sb.ToString().TrimStart('&');
        }

        public RequestConnection? ConnectionType { get; private set; }

        public int PageSize = int.MaxValue;
        public int PageCount = 1;
        public int Page = 1;

        public IEnumerable<PullRequestItem> Items { get; set; }
        public RequestState State { get; private set; }
        public SortOrder Order { get; private set; }
        public SortOrderDirection Direction { get; private set; }
        public int? OpenHits { get; private set; }
        public int? ClosedHits { get; private set; }
        public string SearchTerm { get; private set; }
    }

    public class PullRequestItem
    {
        public PullRequestItem(SearchResultItem issue)
        {
            var path = new Uri(issue.html_url).GetComponents(UriComponents.Path, UriFormat.SafeUnescaped);

            var parts = path.Split('/');
            this.Title = issue.title;
            this.Number = int.Parse(parts[3]);
            this.RepositoryOwner = parts[0];
            this.RepositoryName = parts[1];

            CreateBy = issue.user;
            if (issue.assignee != null) { Assignee = issue.assignee; }

            ClosedAt = issue.closed_at;
            CreatedAt = issue.created_at;
            Comments = issue.comments;
            Labels = issue.labels ?? Enumerable.Empty<Label>();
        }
        
        

        public string Title { get; private set; }
        public int Number { get; private set; }

        public string RepositoryOwner { get; private set; }

        public string RepositoryName { get; private set; }

        public User Assignee { get; private set; }
        public User CreateBy { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset? ClosedAt { get; private set; }
        
        public int Comments { get; private set; }
        public IEnumerable<Label> Labels { get; private set; }
    }
    public enum Status
    {
        Open,
        Closed,
        Merged
    }
}