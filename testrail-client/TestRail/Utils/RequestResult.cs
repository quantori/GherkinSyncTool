using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace TestRail.Utils
{
    /// <summary>
    /// This class is used to simplify a HttpWebResponse and
    /// automatically deserialize the JSON returned.
    /// </summary>
    /// <typeparam name="T">The class to deserialized the JSON that is returned.</typeparam>
    public class RequestResult<T>
    {
        /// <summary>The type response code returned.</summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>The deserialized object from the JSON returned.</summary>
        public T Payload { get; }

        /// <summary>The exception that was throw when the response was received.</summary>
        public Exception ThrownException { get; }

        /// <summary>The raw JSON returned. </summary>
        public string RawJson { get; }

        /// <summary>Constructor for creating a new container for a web response.</summary>
        /// <param name="status">The type response code returned.</param>
        /// <param name="rawJson">The raw JSON to deserialize.</param>
        /// <param name="thrownException">The exception that was throw when the response was received.</param>
        public RequestResult(HttpStatusCode status, string rawJson = null, Exception thrownException = null)
        {
            RawJson = rawJson;
            StatusCode = status;
            ThrownException = thrownException;
            
            if (!string.IsNullOrEmpty(rawJson?.Trim()) && thrownException == null)
            {
                // Welcome to the nightmare zone
                var parseType = typeof(T);
                var isList = typeof(System.Collections.IEnumerable).IsAssignableFrom(typeof(T));
                if (isList)
                {
                    parseType = typeof(T).GetGenericArguments().Single();
                }

                var staticConstructionMethod = parseType.GetMethod(nameof(Types.Case.Parse));
                if(staticConstructionMethod == null)
                {
                    Payload = JsonConvert.DeserializeObject<T>(RawJson);
                }
                else
                {
                    if(isList)
                    {
                        var unwrapped = JsonConvert.DeserializeObject<List<JObject>>(rawJson);
                        Payload = (T)Activator.CreateInstance(typeof(List<>).MakeGenericType(parseType));
                        var list = Payload as IList;
                        foreach(var value in unwrapped)
                        {
                            var obj = staticConstructionMethod.Invoke(null, new object[] { value });
                            list.Add(obj);
                        }
                    }
                    else
                    {
                        Payload = (T)staticConstructionMethod.Invoke(null, new object[] { JObject.Parse(rawJson) });
                    }
                }
            }
        }
    }
}
