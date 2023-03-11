using Gw2Sharp;
using Gw2Sharp.WebApi;

namespace Gw2Gizmos.BuildMachine
{
    public class BuildMachine
    {
        private readonly IGw2Client _gw2Client;

        public BuildMachine()
        {
            _gw2Client = CreateGw2Client();
        }

        private static IGw2Client CreateGw2Client()
        {
            HttpClient httpClient = new();
            Gw2Sharp.WebApi.Http.HttpClient gw2SharpHttpClient = new(httpClient);
            Connection connection = new(null, Locale.English, httpClient: gw2SharpHttpClient);
            return new Gw2Client(connection);
        }
    }
}