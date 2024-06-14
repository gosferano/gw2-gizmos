using Gw2Gizmos.Gw2Api.Client;
using Gw2Gizmos.Gw2Api.Contract.Account;

var client = new Gw2ApiClient(Locale.English);

Account account = await client.Account.GetBlob();
var bank = await client.Account.Bank.GetBlob();
var achievements = await client.Account.Achievements.GetBlob();

foreach (AccountItem item in bank)
{
    if (item.Count == 250)
    {
        Console.WriteLine(item.Id);
    }
}

int[] itemIds = await client.Items.GetIds();
var items = await client.Items.GetByIds(itemIds.Take(50));
var itemsPage = await client.Items.GetPage(0, 50);

Console.WriteLine(account.Name);
