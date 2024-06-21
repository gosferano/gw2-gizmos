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
using Gw2Gizmos.Gw2Api.Contract.DailyCrafting;
using Gw2Gizmos.Gw2Api.Contract.Dungeons;
using Gw2Gizmos.Gw2Api.Contract.Emblem;
using Gw2Gizmos.Gw2Api.Contract.Emotes;
using Gw2Gizmos.Gw2Api.Contract.Finishers;
using Gw2Gizmos.Gw2Api.Contract.Gliders;
using Gw2Gizmos.Gw2Api.Contract.Guild;
using Gw2Gizmos.Gw2Api.Contract.Home;
using Gw2Gizmos.Gw2Api.Contract.Items;
using Gw2Gizmos.Gw2Api.Contract.ItemStats;
using Gw2Gizmos.Gw2Api.Contract.JadeBots;
using Gw2Gizmos.Gw2Api.Contract.LegendaryArmory;
using Gw2Gizmos.Gw2Api.Contract.Legends;
using Gw2Gizmos.Gw2Api.Contract.MailCarriers;
using Gw2Gizmos.Gw2Api.Contract.MapChests;
using Gw2Gizmos.Gw2Api.Contract.Maps;
using Gw2Gizmos.Gw2Api.Contract.Masteries;
using Gw2Gizmos.Gw2Api.Contract.Materials;
using Gw2Gizmos.Gw2Api.Contract.Minis;
using Gw2Gizmos.Gw2Api.Contract.Mounts;
using Gw2Gizmos.Gw2Api.Contract.Novelties;
using Gw2Gizmos.Gw2Api.Contract.Outfits;
using Gw2Gizmos.Gw2Api.Contract.Pets;
using Gw2Gizmos.Gw2Api.Contract.Professions;
using Gw2Gizmos.Gw2Api.Contract.Pvp;
using Gw2Gizmos.Gw2Api.Contract.Quests;
using Gw2Gizmos.Gw2Api.Contract.Races;
using Gw2Gizmos.Gw2Api.Contract.Raids;
using Gw2Gizmos.Gw2Api.Contract.Recipes;
using Gw2Gizmos.Gw2Api.Contract.Specializations;
using File = Gw2Gizmos.Gw2Api.Contract.Files.File;

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
// Daily Crafting
[JsonSerializable(typeof(DailyCrafting[]))]
// Dungeons
[JsonSerializable(typeof(Dungeon[]))]
// Emblem
[JsonSerializable(typeof(Emblem[]))]
// Emotes
[JsonSerializable(typeof(Emote[]))]
// Files
[JsonSerializable(typeof(File[]))]
// Finishers
[JsonSerializable(typeof(Finisher[]))]
// Gliders
[JsonSerializable(typeof(Glider[]))]
// Guild
[JsonSerializable(typeof(Guild))]
[JsonSerializable(typeof(GuildMember[]))]
[JsonSerializable(typeof(GuildRank[]))]
[JsonSerializable(typeof(GuildStashSection[]))]
[JsonSerializable(typeof(GuildStorageItem[]))]
[JsonSerializable(typeof(GuildTeam[]))]
[JsonSerializable(typeof(GuildTreasuryItem[]))]
[JsonSerializable(typeof(GuildPermission[]))]
[JsonSerializable(typeof(GuildUpgrade))]
[JsonSerializable(typeof(GuildUpgrade[]))]
[JsonSerializable(typeof(GuildUpgradeBankBag))]
[JsonSerializable(typeof(GuildUpgradeSimple))]
// Home
[JsonSerializable(typeof(HomeCat[]))]
[JsonSerializable(typeof(HomeNode[]))]
// Items
[JsonSerializable(typeof(Item))]
[JsonSerializable(typeof(ItemSimple))]
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
// Item Stats
[JsonSerializable(typeof(ItemStat[]))]
// Jade Bots
[JsonSerializable(typeof(JadeBot[]))]
// Legendary Armory
[JsonSerializable(typeof(LegendaryArmoryItem[]))]
// Legends
[JsonSerializable(typeof(Legend[]))]
// Mail Carriers
[JsonSerializable(typeof(MailCarrier[]))]
// Map Chests
[JsonSerializable(typeof(MapChest[]))]
// Maps
[JsonSerializable(typeof(Map[]))]
// Masteries
[JsonSerializable(typeof(Mastery[]))]
// Materials
[JsonSerializable(typeof(MaterialCategory[]))]
// Minis
[JsonSerializable(typeof(Mini[]))]
// Mounts
[JsonSerializable(typeof(MountSkin[]))]
[JsonSerializable(typeof(MountType[]))]
// Novelties
[JsonSerializable(typeof(Novelty[]))]
// Outfits
[JsonSerializable(typeof(Outfit[]))]
// Pets
[JsonSerializable(typeof(Pet[]))]
// Professions
[JsonSerializable(typeof(Profession[]))]
[JsonSerializable(typeof(ProfessionTrainingTrackStep))]
[JsonSerializable(typeof(ProfessionTrainingTrackStepSkill))]
[JsonSerializable(typeof(ProfessionTrainingTrackStepTrait))]
// Pvp
[JsonSerializable(typeof(PvpAmulet[]))]
[JsonSerializable(typeof(PvpGame[]))]
[JsonSerializable(typeof(PvpHero[]))]
[JsonSerializable(typeof(PvpRank[]))]
[JsonSerializable(typeof(PvpSeason[]))]
[JsonSerializable(typeof(PvpSeasonLeaderboardEntry[]))]
[JsonSerializable(typeof(PvpStanding[]))]
[JsonSerializable(typeof(PvpStats))]
// Quests
[JsonSerializable(typeof(Quest[]))]
// Races
[JsonSerializable(typeof(Race[]))]
// Raids
[JsonSerializable(typeof(Raid[]))]
// Recipes
[JsonSerializable(typeof(Recipe[]))]
// Specializations
[JsonSerializable(typeof(Specialization[]))]
public partial class Gw2ApiV2JsonContext : JsonSerializerContext;
