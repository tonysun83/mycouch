using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using FluentAssertions;
using MyCouch;
using MyCouch.Net;
using MyCouch.Testing.TestData;


namespace IntegrationTests.CoreTests
{
    public class ClientTests : IntegrationTestsOf<IMyCouchClient>
    {
        public ClientTests()
        {
            SUT = DbClient;
        }

        [MyFact(TestScenarios.Client)]
        public async void Should_invoke_BeforeSend_if_hooked_in()
        {
            var wasCalled = false;

            SUT.Connection.BeforeSend = _ => wasCalled = true;

            await SUT.Database.HeadAsync();

            wasCalled.Should().BeTrue();
        }

        [MyFact(TestScenarios.Client)]
        public async void Should_invoke_AfterSend_if_hooked_in()
        {
            var wasCalled = false;

            SUT.Connection.AfterSend = _ =>
            {
                wasCalled = true;
            };

            await SUT.Database.HeadAsync();

            wasCalled.Should().BeTrue();
        }

        [MyFact(TestScenarios.Client)]
        public void Should_Get_Cookie_Back()
        {
            CookieContainer cookies = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = cookies;

            HttpClient authClient = new HttpClient(handler);

            var uri = new System.Uri("http://localhost:15984/_session");

            authClient.BaseAddress = uri;
            authClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpContent content = new JsonContent("{\"name\" : \"adm\", \"password\" : \"pass\"}");

            HttpResponseMessage authenticationResponse = authClient.PostAsync(uri, content).Result;

            var responseCookies = cookies.GetCookies(uri).Cast<Cookie>();

            var cookieValue = responseCookies.First().Value;

            CookieContainer requestContainer = new CookieContainer();
            requestContainer.Add(responseCookies.First());

            var DbCookieClient = IntegrationTestsRuntime.CreateDbClient(requestContainer);

            DbCookieClient.Database.PutAsync().Wait();
            var a1 = DbCookieClient.Documents.PostAsync(ClientTestData.Artists.Artist1Json).Result;
            var a1Updated = DbCookieClient.Documents.PutAsync(a1.Id, a1.Rev, ClientTestData.Artists.Artist1Json).Result;

            var a2 = DbCookieClient.Documents.PostAsync(ClientTestData.Artists.Artist2Json).Result;
            var a2Deleted = DbCookieClient.Documents.DeleteAsync(a2.Id, a2.Rev).Result;
            a2Deleted.StatusCode.Should().Be(200);
        }
    }
}