using Microsoft.Extensions.Configuration;
using System.IO;
using System.Net;
using Xunit;

namespace TestRail.Tests
{
    public class ClientTests
    {
        private readonly TestRailClient _client;

        public ClientTests()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            var section = configuration.GetSection("TestRail");

            _client = new TestRailClient(section["baseurl"], section["username"], section["password"]);
        }

        [Fact]
        public void Authentication_Is_Successful()
        {
            var response = _client.GetCase(1);

            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
