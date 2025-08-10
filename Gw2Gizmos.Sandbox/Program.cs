// See https://aka.ms/new-console-template for more information

using Gw2Gizmos.Gw2Api.Client;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Characters;
using Gw2Gizmos.Gw2Api.Contract.Characters;

var gw2ApiClient = new Gw2ApiClient(Environment.GetEnvironmentVariable("GW2_API_KEY"));
var characters = await gw2ApiClient.V2.Characters.GetByIds(["Gosferano"]);
var tokenInfo = await gw2ApiClient.V2.TokenInfo.GetBlob();
var account = await gw2ApiClient.V2.Account.GetBlob();

foreach (var guildId in account.GuildLeader)
{
    var guild = await gw2ApiClient.V2.Guild[guildId].GetBlob();
    Console.WriteLine($"Guild Leader: {guildId}");
}
