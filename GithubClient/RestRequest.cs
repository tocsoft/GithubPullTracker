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

        #region Private Members

        /// <summary>
        /// 
        /// </summary>
        private List<UrlSegment> UrlSegments { get; set; }

        internal List<KeyValuePair<string, string>> Headers { get; set; }

        internal List<KeyValuePair<string, object>> Parameters { get; set; }
        
        public TimeSpan? Timeout { get; set; }
        #endregion

        #region Properties
        
        /// <summary>
        /// 
        /// </summary>
        public string DateFormat { get; set; }

        /// <summary>
        /// Specifies whether or not the root element in the response.
        /// </summary>
        public bool IgnoreRootElement { get; set; }

        /// <summary>
        /// When <see cref="ContentTypes.Xml"/>, specifies whether or not attributes should be ignored.
        /// </summary>
        public bool IgnoreXmlAttributes { get; set; }

        /// <summary>
        /// The HTTP method to use for the request.
        /// </summary>
        public HttpMethod Method { get; set; }

        /// <summary>
        /// A string representation of the specific resource to access, using ASP.NET MVC-like replaceable tokens.
        /// </summary>
        public string Resource { internal get; set; }

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
            Headers = new List<KeyValuePair<string, string>>();
            Method = HttpMethod.Get;
        }

        /// <summary>
        /// Creates a new RestRequest instance for a given Resource.
        /// </summary>
        /// <param name="resource">The specific resource to access.</param>
        public RestRequest(string resource)
            : this()
        {
            Resource = resource;
        }

        /// <summary>
        /// Creates a new RestRequest instance for a given Resource and Method.
        /// </summary>
        /// <param name="resource">The specific resource to access.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        public RestRequest(string resource, HttpMethod method)
            : this(resource)
        {
            Method = method;
        }

        /// <summary>
        /// Creates a new RestRequest instance for a given Resource and Method, specifying whether or not to ignore the root object in the response.
        /// </summary>
        /// <param name="resource">The URL format string of the resource to request.</param>
        /// <param name="method">The <see cref="HttpMethod"/> for the request.</param>
        /// <param name="ignoreRoot">Whether or not the root object from the response should be ignored.</param>
        public RestRequest(string resource, HttpMethod method, bool ignoreRoot)
            : this(resource, method)
        {
            IgnoreRootElement = ignoreRoot;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds an unnamed parameter to the body of the request.
        /// </summary>
        /// <param name="value"></param>
        /// <remarks>Use this method if you're not using UrlFormEncoded requests.</remarks>
        public void AddParameter(object value)
        {
            Parameters.Add(new KeyValuePair<string, object>("", value));
        }
        
        /// <summary>
        /// Adds a parameter to the body of the request.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <remarks>Note: If the ContentType is anything other than UrlFormEncoded, only the first Parameter will be serialzed to the request body.</remarks>
        public void AddParameter(string key, object value)
        {
            Parameters.Add(new KeyValuePair<string, object>(key, value));
        }

        /// <summary>
        /// Replaces tokenized segments of the URL with a desired value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <example>If <code>Resource = "{entity}/Samples.aspx"</code> and <code>someVariable.Publisher = "Disney";</code>, then
        /// <code>Resource.AddUrlSegment("entity", someVariable.Publisher);</code> becomes <code>Resource = "Disney/Samples.aspx";</code></example>
        public void AddUrlSegment(string key, string value)
        {
            UrlSegments.Add(new UrlSegment(key, value));
        }

        /// <summary>
        /// Appends a key/value pair to the end of the existing QueryString in a URI.
        /// </summary>
        /// <param name="key">The string key to append to the QueryString.</param>
        /// <param name="value">The string value to append to the QueryString.</param>
        public void AddQueryString(string key, string value)
        {
            UrlSegments.Add(new UrlSegment(key, value, true));
        }

        /// <summary>
        /// Appends a key/value pair to the end of the existing QueryString in a URI.
        /// </summary>
        /// <param name="key">The string key to append to the QueryString.</param>
        /// <param name="value">The string value to append to the QueryString.</param>
        public void AddHeader(string key, string value)
        {
            Headers.Add(new KeyValuePair<string, string>(key, value));
        }

        /// <summary>
        /// Appends a key/value pair to the end of the existing QueryString in a URI.
        /// </summary>
        /// <param name="key">The string key to append to the QueryString.</param>
        /// <param name="value">The value to append to the QueryString (we will call .ToString() for you).</param>
        public void AddQueryString(string key, object value)
        {
            AddQueryString(key, value.ToString());
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
            var currentResource = Resource;
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



        public RestRequest Clone()
        {
            return new RestRequest()
            {
                UrlSegments = this.UrlSegments.Select(x => x.Clone()).ToList(),
                Parameters = this.Parameters.Select(x => new KeyValuePair<string, object>(x.Key, x.Value)).ToList(),
                Headers = this.Headers.Select(x => new KeyValuePair<string, string>(x.Key, x.Value)).ToList(),
                DateFormat = this.DateFormat,
                IgnoreRootElement = this.IgnoreRootElement,
                IgnoreXmlAttributes = this.IgnoreXmlAttributes,
                Method = this.Method,
                Resource = this.Resource,
                ReturnRawString = this.ReturnRawString
            };
        }
    }
}
