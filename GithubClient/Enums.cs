using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubClient
{

    public enum RequestConnection
    {
        Involved,
        Author,
        Assigned
    }

    public enum RequestState
    {
        Open,
        Closed,
        Merged
    }

    public enum SortOrder
    {
        comments,
        created,
        updated,
        bestmatch
    }
    public enum SortOrderDirection
    {
        Decending,
        Asccending
    }
}
