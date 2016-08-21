using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GithubClient
{
    public class RestClient : RestClientBase
    {
        public static Func<HttpMessageHandler> HandlerBuilder
        {
            get;set;
        } = () => {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true
            };
            if (handler.SupportsAutomaticDecompression)
            {
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }
            return handler;
        };
        

        #region Private Members

        private Dictionary<long, HttpClient> _clients = new Dictionary<long, HttpClient>();
        private object _clientsLock = new object();

        private HttpClient GetClient(TimeSpan? timeout)
        {
            long key = timeout.HasValue ? timeout.Value.Ticks : -1;

            lock (_clientsLock)
            {
                if (!_clients.ContainsKey(key))
                {
                    var client = new HttpClient(Handler);
                    if (timeout.HasValue)
                    {
                        client.Timeout = timeout.Value;
                    }
                    _clients.Add(key, client);
                    return client;
                }
                return _clients[key];
            }

        }

        #endregion

        #region Properties


        #endregion
        HttpMessageHandler _handler;
        public HttpMessageHandler Handler
        {
            get
            {
                if (_handler == null)
                {
                    Handler = HandlerBuilder();
                }

                return _handler;
            }

            set
            {
                _handler = value;
                lock (_clientsLock)
                {
                    _clients.Clear();
                }
            }
        }

        #region Methods

        /// <summary>
        /// Creates a new instance of the RestClient class.
        /// </summary>
        public RestClient() : base()
        {
        }


        public async Task<T> ExecuteAsync<T>(RestRequest restRequest)
        {
            var response = await BaseExecuteAsync(restRequest);

            var responseContent = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<T>(responseContent, JsonSerializerSettings);
        }

        public async Task<IEnumerable<T>> ExecutePagesAsync<T>(RestRequest restRequest, int maxPages)
        {
            IEnumerable<T> allResults = Enumerable.Empty<T>();

            int counter = 0;
            var hasNextPage = true;
            while (hasNextPage && counter < maxPages)
            {

                var response = await BaseExecuteAsync(restRequest);

                var responseContent = await response.Content.ReadAsStringAsync();

                var pageOfResults = JsonConvert.DeserializeObject<IEnumerable<T>>(responseContent, JsonSerializerSettings);
                allResults = allResults.Union(pageOfResults);

                IEnumerable<string> links;
                if (response.Headers.TryGetValues("Link", out links))
                {
                    var nextUrl = links.First().Split(',').Select(x => x.Split(';')).Where(x => x[1].Contains("\"next\"")).Select(x => x[0].Substring(1, x[0].Length - 2)).FirstOrDefault();
                    hasNextPage = nextUrl !=null;
                    restRequest.Resource = $"{nextUrl:raw}";
                }
                else { hasNextPage = false; }
            }

            return allResults;
        }

        public override async Task<Stream> ExecuteStreamAsync(RestRequest restRequest)
        {
            var req = await BaseExecuteAsync(restRequest);

            return await req.Content.ReadAsStreamAsync();
        }

        public Task<HttpResponseMessage> ExecuteAsync(RestRequest restRequest)
        {
            return  BaseExecuteAsync(restRequest);
        }

        protected virtual async Task<HttpResponseMessage> BaseExecuteAsync(RestRequest restRequest)
        {
            var _client = GetClient(restRequest.Timeout);

            return await InnerExecuteAsync(restRequest, _client);
        }

        protected HttpRequestMessage GenerateRequestMessage(RestRequest restRequest)
        {
            var _client = GetClient(restRequest.Timeout);

            if (string.IsNullOrWhiteSpace(restRequest.DateFormat) && !string.IsNullOrWhiteSpace(DateFormat))
            {
                restRequest.DateFormat = DateFormat;
            }

            var uri = restRequest.GetResourceUri(BaseUrl);
            var message = new HttpRequestMessage(new System.Net.Http.HttpMethod(restRequest.Method.Method), uri);
            

            var rId = message.GetHashCode();

            LogTo.Trace("Building Request - {0}", rId);
            LogTo.Trace("Building Request - {0} - Method: {1}", rId, restRequest.Method.Method);
            LogTo.Trace("Building Request - {0} - URI: {1}", rId, uri);

            if (!string.IsNullOrWhiteSpace(UserAgent) && !message.Headers.Contains("user-agent"))
            {
                message.Headers.Add("user-agent", UserAgent);
                LogTo.Trace("Building Request - {0} - UserAgent: {1}", rId, UserAgent);
            }

            foreach (var header in DefaultHeaders)
            {
                IEnumerable<string> values;
                if(message.Headers.TryGetValues(header.Key, out values))
                {
                    message.Headers.Remove(header.Key);
                    message.Headers.Add(header.Key, values.Union(header.Value));
                }
                else
                {
                    message.Headers.Add(header.Key, header.Value);
                }
            }

            foreach (var header in restRequest.Headers)
            {
                IEnumerable<string> values;
                if (message.Headers.TryGetValues(header.Key, out values))
                {
                    message.Headers.Remove(header.Key);
                    message.Headers.Add(header.Key, values.Union(header.Value));
                }
                else
                {
                    message.Headers.Add(header.Key, header.Value);
                }
            }

            if (restRequest.Method != HttpMethod.Get && restRequest.Method != HttpMethod.Head && restRequest.Method != HttpMethod.Trace)
            {
                var json = restRequest.Parameters.Count > 0 ? JsonConvert.SerializeObject(restRequest.Parameters[0].Value, JsonSerializerSettings) : "";

                LogTo.Trace("Building Request - {0} - SetBody: {1}", rId, json);

                message.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            return message;
        }

        private async Task<HttpResponseMessage> InnerExecuteAsync(RestRequest restRequest, HttpClient _client)
        {
            var message = GenerateRequestMessage(restRequest);

            var rId = message.GetHashCode();
            LogTo.Trace("Sending Request - {0}", rId);

            HttpResponseMessage response = await _client.SendAsync(message).ConfigureAwait(false);

            LogTo.Trace("Received Response - {0}", rId);

            LogTo.Trace("Response - {0} - Status : {1}", rId, response.StatusCode);

            //run this in a func to prevent reading when not required
            LogTo.Trace(() => string.Format("Response - {0} - Content : {1}", rId, response.Content.ReadAsStringAsync().GetAwaiter().GetResult()));

            if (!response.IsSuccessStatusCode)
            {
                throw new RestException( response, this);
            }
            return response;
        }

        #endregion
    }


    public partial class RestException : Exception
    {
        private RestClient _client;
        public HttpResponseMessage Response { get; private set; }

        internal RestException(HttpResponseMessage response, RestClient client)
            : base("Error with request")
        {
            Response = response;
            _client = client;
        }

        public async Task<T> DeserializeBody<T>()
        {
            var json = await Response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(json, _client.JsonSerializerSettings);
        }
    }

    public abstract class RestClientBase 
    {
        /// <summary>
        /// Gets or sets the json serializer settings.
        /// </summary>
        /// <value>
        /// The json serializer settings.
        /// </value>
        public JsonSerializerSettings JsonSerializerSettings { get; set; }

        /// <summary>
        /// The base URL for the resource this client will access.
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string DateFormat { get; set; }

        /// <summary>
        /// The User Agent string to pass back to the API.
        /// </summary>
        public string UserAgent { get; set; }

        public HttpRequestHeaders DefaultHeaders { get; }

        /// <summary>
        /// Creates a new instance of the RestClient class.
        /// </summary>
        public RestClientBase()
        {
            HttpRequestMessage msg = new HttpRequestMessage();

            DefaultHeaders = msg.Headers;
        }

        ///// <summary>
        ///// Adds a header for a given string key and string value.
        ///// </summary>
        ///// <param name="key">The header to add.</param>
        ///// <param name="value">The value of the header being added.</param>
        //public void SetHeader(string key, string value)
        //{
        //    RemoveHeader(key);
        //    Headers.Add(new KeyValuePair<string, string>(key, value));
        //}

        ///// <summary>
        ///// Adds a header for a given string key and string value.
        ///// </summary>
        ///// <param name="key">The header to add.</param>
        ///// <param name="value">The value of the header being added.</param>
        //public void RemoveHeader(string key)
        //{
        //    foreach (var kvp in Headers.Where(x => x.Key == key).ToArray())
        //    {
        //        Headers.Remove(kvp);
        //    }
        //}
        
            
        public abstract Task<Stream> ExecuteStreamAsync(RestRequest restRequest);
        

    }

    internal static class LogTo
    {
        public static void Trace(string str, params object[] args) { }
        public static void Trace(Func<string> fnc)
        {

        }
    }
}

