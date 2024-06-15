using System.Text.Json.Serialization;
using Gw2Gizmos.Gw2Api.Contract.Account;
using Gw2Gizmos.Gw2Api.Contract.Achievements;
using Gw2Gizmos.Gw2Api.Contract.Characters;
using Gw2Gizmos.Gw2Api.Contract.Colors;
using Gw2Gizmos.Gw2Api.Contract.Commerce;
using Gw2Gizmos.Gw2Api.Contract.Items;

namespace Gw2Gizmos.Gw2Api.Client.Json;

// Account
[JsonSerializable(typeof(Account))]
[JsonSerializable(typeof(AccountAchievement[]))]
[JsonSerializable(typeof(AccountItem?[]))]
// Achievements
[JsonSerializable(typeof(Achievement[]))]
[JsonSerializable(typeof(AchievementCategory[]))]
[JsonSerializable(typeof(AchievementGroup[]))]
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
public partial class Gw2ApiV2JsonContext : JsonSerializerContext;
