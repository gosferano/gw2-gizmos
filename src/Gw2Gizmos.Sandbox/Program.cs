// See https://aka.ms/new-console-template for more information

using Gw2Gizmos.Gw2Api.Client;
using Gw2Gizmos.Gw2Api.Contract.V2.Account;

// No DI required: Create(...) reuses a cached, managed, resilient HttpClient under the hood.
var gw2ApiClient = Gw2ApiClient.Create(Environment.GetEnvironmentVariable("GW2_API_KEY"));
var characters = await gw2ApiClient.V2.Characters.GetByIds(["Gosferano"]);
var tokenInfo = await gw2ApiClient.V2.TokenInfo.GetBlob();
Account? account = await gw2ApiClient.V2.Account.GetBlob();
if (account is null)
{
    return;
}

foreach (var guildId in account.GuildLeader)
{
    var guild = await gw2ApiClient.V2.Guild[guildId].GetBlob();
    Console.WriteLine($"Guild Leader: {guildId}");
}
