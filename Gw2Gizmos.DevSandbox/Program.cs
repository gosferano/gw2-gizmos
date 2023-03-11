using Gw2Gizmos.Gw2Api.Client;
using Gw2Gizmos.Gw2Api.Contract.Items;

Gw2ApiClient client = new Gw2ApiClient();
var items = await client.Get<Item[]>("/v2/items?page=1");

Console.WriteLine("GW");