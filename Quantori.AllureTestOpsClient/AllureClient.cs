using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Refit;

namespace Quantori.AllureTestOpsClient
{
    public static class AllureClient
    {
        public static IAllureClient Get(string baseUrl, string accessToken)
        {
            var allureClient = RestService.For<IAllureClient>(baseUrl, new RefitSettings
            {
                AuthorizationHeaderValueGetter = () => Task.FromResult(accessToken),
                ContentSerializer = new NewtonsoftJsonContentSerializer(
                    new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver(),
                        NullValueHandling = NullValueHandling.Ignore
                    }
                )
            });
            return allureClient;
        }
    }
}