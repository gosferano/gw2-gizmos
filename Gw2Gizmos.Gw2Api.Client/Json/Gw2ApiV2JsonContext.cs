using System.Text.Json.Serialization;
using Gw2Gizmos.Gw2Api.Contract.Account;
using Gw2Gizmos.Gw2Api.Contract.Achievements;
using Gw2Gizmos.Gw2Api.Contract.Backstory;
using Gw2Gizmos.Gw2Api.Contract.Build;
using Gw2Gizmos.Gw2Api.Contract.BuildStorage;
using Gw2Gizmos.Gw2Api.Contract.Characters;
using Gw2Gizmos.Gw2Api.Contract.Colors;
using Gw2Gizmos.Gw2Api.Contract.Commerce;
using Gw2Gizmos.Gw2Api.Contract.Continents;
using Gw2Gizmos.Gw2Api.Contract.Currencies;
using Gw2Gizmos.Gw2Api.Contract.Items;
using Gw2Gizmos.Gw2Api.Contract.Materials;
using Gw2Gizmos.Gw2Api.Contract.Races;
using Gw2Gizmos.Gw2Api.Contract.Specializations;

namespace Gw2Gizmos.Gw2Api.Client.Json;

// Account
[JsonSerializable(typeof(Account))]
[JsonSerializable(typeof(AccountAchievement[]))]
[JsonSerializable(typeof(AccountItem?[]))]
[JsonSerializable(typeof(AccountMaterial[]))]
[JsonSerializable(typeof(AccountLegendaryArmoryItem[]))]
[JsonSerializable(typeof(AccountLuck[]))]
[JsonSerializable(typeof(AccountMastery[]))]
[JsonSerializable(typeof(AccountProgression[]))]
[JsonSerializable(typeof(AccountWalletCurrency[]))]
[JsonSerializable(typeof(AccountWizardsVaultCategory))]
[JsonSerializable(typeof(AccountWizardsVaultListing[]))]
[JsonSerializable(typeof(AccountWizardsVaultSpecial))]
[JsonSerializable(typeof(BuildStorageBuild[]))]
// Achievements
[JsonSerializable(typeof(Achievement[]))]
[JsonSerializable(typeof(AchievementCategory[]))]
[JsonSerializable(typeof(AchievementGroup[]))]
// Backstory
[JsonSerializable(typeof(BackstoryAnswer[]))]
[JsonSerializable(typeof(BackstoryQuestion[]))]
// Build
[JsonSerializable(typeof(Build))]
// Character
[JsonSerializable(typeof(Character[]))]
[JsonSerializable(typeof(CharacterBackstory))]
[JsonSerializable(typeof(CharacterBuildTab[]))]
[JsonSerializable(typeof(CharacterCore))]
[JsonSerializable(typeof(CharacterCrafting))]
[JsonSerializable(typeof(CharacterEquipment))]
[JsonSerializable(typeof(CharacterEquipmentTab[]))]
[JsonSerializable(typeof(CharacterInventory))]
[JsonSerializable(typeof(CharacterRecipes))]
[JsonSerializable(typeof(CharacterSab))]
[JsonSerializable(typeof(CharacterTraining))]
// Colors
[JsonSerializable(typeof(Color[]))]
// Commerce
[JsonSerializable(typeof(CommerceDelivery))]
[JsonSerializable(typeof(CommerceExchange))]
[JsonSerializable(typeof(CommerceListings[]))]
[JsonSerializable(typeof(CommercePrices[]))]
[JsonSerializable(typeof(CommerceTransaction[]))]
// Continents
[JsonSerializable(typeof(Continent[]))]
[JsonSerializable(typeof(ContinentFloor[]))]
[JsonSerializable(typeof(ContinentFloorRegion[]))]
[JsonSerializable(typeof(ContinentFloorRegionMap[]))]
[JsonSerializable(typeof(ContinentFloorRegionMapPoi[]))]
[JsonSerializable(typeof(ContinentFloorRegionMapSector[]))]
[JsonSerializable(typeof(ContinentFloorRegionMapTask[]))]
// Currencies
[JsonSerializable(typeof(Currency[]))]
// Items
[JsonSerializable(typeof(Item))]
[JsonSerializable(typeof(Item[]))]
[JsonSerializable(typeof(Armor))]
[JsonSerializable(typeof(BackItem))]
[JsonSerializable(typeof(Bag))]
[JsonSerializable(typeof(Consumable))]
[JsonSerializable(typeof(Container))]
[JsonSerializable(typeof(Gathering))]
[JsonSerializable(typeof(Gizmo))]
[JsonSerializable(typeof(MiniPet))]
[JsonSerializable(typeof(Tool))]
[JsonSerializable(typeof(Trinket))]
[JsonSerializable(typeof(UpgradeComponent))]
[JsonSerializable(typeof(Weapon))]
// Materials
[JsonSerializable(typeof(MaterialCategory[]))]
// Races
[JsonSerializable(typeof(Race[]))]
// Specializations
[JsonSerializable(typeof(Specialization[]))]
public partial class Gw2ApiV2JsonContext : JsonSerializerContext;
