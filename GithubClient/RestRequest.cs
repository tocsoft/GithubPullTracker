using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GithubClient
{
    public class RestRequest
    {
        static QuerystringFormatProvider qsfp = new QuerystringFormatProvider();

        #region Private Members

        /// <summary>
        /// 
        /// </summary>
        private List<UrlSegment> UrlSegments { get; set; }

        public System.Net.Http.Headers.HttpRequestHeaders Headers { get; }

        internal List<KeyValuePair<string, object>> Parameters { get; set; }

        public TimeSpan? Timeout { get; set; }
        #endregion

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public string DateFormat { get; set; }
        
        /// <summary>
        /// The HTTP method to use for the request.
        /// </summary>
        public HttpMethod Method { get; set; }

        /// <summary>
        /// A string representation of the specific resource to access, using ASP.NET MVC-like replaceable tokens.
        /// </summary>
        public FormattableString Resource { internal get; set; }

        /// <summary>
        /// Tells the RestClient to skip deserialization and return the raw result.
        /// </summary>
        public bool ReturnRawString { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new RestRequest instance, assuming the request will be an HTTP GET.
        /// </summary>
        public RestRequest()
        {
            UrlSegments = new List<UrlSegment>();
            Parameters = new List<KeyValuePair<string, object>>();
            Headers = new HttpRequestMessage().Headers;
            Method = HttpMethod.Get;

            Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Creates a new RestRequest instance for a given Resource.
        /// </summary>
        /// <param name="resource">The specific resource to access.</param>
        public RestRequest(FormattableString resource)
            : this()
        {
            Resource = resource;

        }

        /// <summary>
        /// Creates a new RestRequest instance for a given Resource and Method.
        /// </summary>
        /// <param name="resource">The specific resource to access.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        public RestRequest(FormattableString resource, HttpMethod method)
            : this(resource)
        {
            Method = method;
        }
        

        #endregion

        public RestRequest UpateHeaders(Action<System.Net.Http.Headers.HttpRequestHeaders> action)
        {
            action(Headers);
            return this;
        }


        #region Public Methods

        /// <summary>
        /// Adds an unnamed parameter to the body of the request.
        /// </summary>
        /// <param name="value"></param>
        /// <remarks>Use this method if you're not using UrlFormEncoded requests.</remarks>
        public RestRequest AddParameter(object value)
        {
            Parameters.Add(new KeyValuePair<string, object>("", value));
            return this;
        }

        /// <summary>
        /// Adds a parameter to the body of the request.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <remarks>Note: If the ContentType is anything other than UrlFormEncoded, only the first Parameter will be serialzed to the request body.</remarks>
        public RestRequest AddParameter(string key, object value)
        {
            Parameters.Add(new KeyValuePair<string, object>(key, value));
            return this;
        }

        /// <summary>
        /// Appends a key/value pair to the end of the existing QueryString in a URI.
        /// </summary>
        /// <param name="key">The string key to append to the QueryString.</param>
        /// <param name="value">The string value to append to the QueryString.</param>
        public RestRequest AddQueryString(string key, string value)
        {
            UrlSegments.Add(new UrlSegment(key, value, true));
            return this;
        }


        /// <summary>
        /// Appends a key/value pair to the end of the existing QueryString in a URI.
        /// </summary>
        /// <param name="key">The string key to append to the QueryString.</param>
        /// <param name="value">The value to append to the QueryString (we will call .ToString() for you).</param>
        public RestRequest AddQueryString(string key, object value)
        {
            AddQueryString(key, value.ToString());
            return this;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <returns></returns>
        internal Uri GetResourceUri(string baseUrl)
        {
            var currentResource = Resource.ToString(qsfp);

            foreach (var segment in UrlSegments.Where(c => !c.IsQueryString))
            {
                currentResource = currentResource.Replace("{" + segment.Key + "}", Uri.EscapeUriString(segment.Value));
            }

            if (UrlSegments.Any(c => c.IsQueryString))
            {
                var queryString = UrlSegments.Where(c => c.IsQueryString)
                    .Aggregate(new StringBuilder(),
                        (current, next) =>
                            current.Append(string.Format("&{0}={1}", Uri.EscapeUriString(next.Key), Uri.EscapeDataString(next.Value))))
                    .ToString();

                currentResource = string.Format(currentResource.Contains("?") ? "{0}{1}" : "{0}?{1}", currentResource, queryString.TrimStart('&'));
            }

            if (!string.IsNullOrEmpty(currentResource) && currentResource.StartsWith("/"))
            {
                currentResource = currentResource.Substring(1);
            }
            var uri = new Uri(currentResource, UriKind.RelativeOrAbsolute);

            if (!string.IsNullOrEmpty(baseUrl))
            {
                if (!uri.IsAbsoluteUri)
                {
                    uri = new Uri(string.IsNullOrEmpty(currentResource) ? baseUrl : string.Format("{0}/{1}", baseUrl, currentResource), UriKind.RelativeOrAbsolute);
                }
            }
            return uri;
        }



        #endregion



        //public RestRequest Clone()
        //{
        //    return new RestRequest()
        //    {
        //        UrlSegments = this.UrlSegments.Select(x => x.Clone()).ToList(),
        //        Parameters = this.Parameters.Select(x => new KeyValuePair<string, object>(x.Key, x.Value)).ToList(),
        //        Headers = this.Headers.Select(x => new KeyValuePair<string, string>(x.Key, x.Value)).ToList(),
        //        DateFormat = this.DateFormat,
        //        IgnoreRootElement = this.IgnoreRootElement,
        //        IgnoreXmlAttributes = this.IgnoreXmlAttributes,
        //        Method = this.Method,
        //        Resource = this.Resource,
        //        ReturnRawString = this.ReturnRawString
        //    };
        //}
    }

    public static class RestRequestExtension
    {
        public static Task<T> ExecuteWithAsync<T>(this RestRequest req, RestClient client)
        {
            return client.ExecuteAsync<T>(req);
        }
        public static Task<IEnumerable<T>> ExecutePagesWithAsync<T>(this RestRequest req, RestClient client, int maxPagesToGet = 10)
        {
            return client.ExecutePagesAsync<T>(req, maxPagesToGet);
        }
        public static Task<HttpResponseMessage> ExecuteWithAsync(this RestRequest req, RestClient client)
        {
            return client.ExecuteAsync(req);
        }
    }
}
