using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gw2Gizmos.Data.Provider.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountContainerSlots",
                columns: table => new
                {
                    AccountId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Store = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    SlotIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    Charges = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountContainerSlots", x => new { x.AccountId, x.Store, x.SlotIndex });
                });

            migrationBuilder.CreateTable(
                name: "AccountItemObservations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Container = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    ObservedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountItemObservations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    AccountId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    World = table.Column<int>(type: "INTEGER", nullable: false),
                    LastSyncedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.AccountId);
                });

            migrationBuilder.CreateTable(
                name: "AccountWalletObservations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CurrencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<long>(type: "INTEGER", nullable: false),
                    ObservedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountWalletObservations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterItemSlots",
                columns: table => new
                {
                    CharacterName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SlotIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    AccountId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    Charges = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterItemSlots", x => new { x.CharacterName, x.SlotIndex });
                });

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AccountId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Race = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Gender = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Profession = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Guild = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Age = table.Column<int>(type: "INTEGER", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Deaths = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<int>(type: "INTEGER", nullable: true),
                    BuildTabsUnlocked = table.Column<int>(type: "INTEGER", nullable: false),
                    ActiveBuildTab = table.Column<int>(type: "INTEGER", nullable: false),
                    EquipmentTabsUnlocked = table.Column<int>(type: "INTEGER", nullable: false),
                    ActiveEquipmentTab = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Icon = table.Column<string>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InfixUpgrade",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfixUpgrade", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemCraftCosts",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    CraftingCost = table.Column<double>(type: "REAL", nullable: false),
                    ComputedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemCraftCosts", x => x.ItemId);
                });

            migrationBuilder.CreateTable(
                name: "ItemInfusionSlot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemInfusionSlot", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChatLink = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Icon = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Rarity = table.Column<string>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    VendorValue = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultSkin = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaterialCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaterialCategoryItems",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialCategoryItems", x => new { x.CategoryId, x.ItemId });
                });

            migrationBuilder.CreateTable(
                name: "PriceSnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Buy = table.Column<int>(type: "INTEGER", nullable: true),
                    Sell = table.Column<int>(type: "INTEGER", nullable: true),
                    Demand = table.Column<int>(type: "INTEGER", nullable: false),
                    Supply = table.Column<int>(type: "INTEGER", nullable: false),
                    Sold = table.Column<int>(type: "INTEGER", nullable: false),
                    Bought = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    OutputItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    OutputItemCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeToCraftMs = table.Column<int>(type: "INTEGER", nullable: false),
                    MinRating = table.Column<int>(type: "INTEGER", nullable: false),
                    OutputUpgradeId = table.Column<int>(type: "INTEGER", nullable: true),
                    ChatLink = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InfixUpgradeAttributes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Attribute = table.Column<string>(type: "TEXT", nullable: false),
                    Modifier = table.Column<decimal>(type: "TEXT", nullable: false),
                    InfixUpgradeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfixUpgradeAttributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InfixUpgradeAttributes_InfixUpgrade_InfixUpgradeId",
                        column: x => x.InfixUpgradeId,
                        principalTable: "InfixUpgrade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InfixUpgradeBuffs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SkillId = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    InfixUpgradeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfixUpgradeBuffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InfixUpgradeBuffs_InfixUpgrade_InfixUpgradeId",
                        column: x => x.InfixUpgradeId,
                        principalTable: "InfixUpgrade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemInfusionSlotFlags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Flag = table.Column<string>(type: "TEXT", nullable: false),
                    InfusionSlotId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemInfusionSlotFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemInfusionSlotFlags_ItemInfusionSlot_InfusionSlotId",
                        column: x => x.InfusionSlotId,
                        principalTable: "ItemInfusionSlot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Armors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Armors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Armors_Items_Id",
                        column: x => x.Id,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BackItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackItems_Items_Id",
                        column: x => x.Id,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bags_Items_Id",
                        column: x => x.Id,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Consumables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consumables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Consumables_Items_Id",
                        column: x => x.Id,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Containers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Containers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Containers_Items_Id",
                        column: x => x.Id,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Gatherings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gatherings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Gatherings_Items_Id",
                        column: x => x.Id,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Gizmos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gizmos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Gizmos_Items_Id",
                        column: x => x.Id,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemFlags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemFlags_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemGameTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemGameTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemGameTypes_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemRestrictions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemRestrictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemRestrictions_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MiniPets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MiniPets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MiniPets_Items_Id",
                        column: x => x.Id,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tools",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tools", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tools_Items_Id",
                        column: x => x.Id,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Trinkets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trinkets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trinkets_Items_Id",
                        column: x => x.Id,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UpgradeComponents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpgradeComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UpgradeComponents_Items_Id",
                        column: x => x.Id,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Weapons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Weapons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Weapons_Items_Id",
                        column: x => x.Id,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecipeDisciplines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    RecipeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeDisciplines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeDisciplines_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecipeFlags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    RecipeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeFlags_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecipeIngredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    RecipeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeIngredients", x => new { x.Id, x.RecipeId });
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArmorDetails",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    WeightClass = table.Column<string>(type: "TEXT", nullable: false),
                    Defense = table.Column<int>(type: "INTEGER", nullable: false),
                    AttributeAdjustment = table.Column<decimal>(type: "TEXT", nullable: false),
                    SuffixItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    SecondarySuffixItemId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArmorDetails", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_ArmorDetails_Armors_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Armors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BackItemDetails",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    AttributeAdjustment = table.Column<decimal>(type: "TEXT", nullable: false),
                    SuffixItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    SecondarySuffixItemId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackItemDetails", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_BackItemDetails_BackItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BackItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BagDetails",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Size = table.Column<int>(type: "INTEGER", nullable: false),
                    NoSellOrSort = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BagDetails", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_BagDetails_Bags_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Bags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsumableDetails",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    DurationMs = table.Column<int>(type: "INTEGER", nullable: true),
                    UnlockType = table.Column<string>(type: "TEXT", nullable: true),
                    ColorId = table.Column<int>(type: "INTEGER", nullable: true),
                    RecipeId = table.Column<int>(type: "INTEGER", nullable: true),
                    GuildUpgradeId = table.Column<int>(type: "INTEGER", nullable: true),
                    ApplyCount = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Icon = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumableDetails", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_ConsumableDetails_Consumables_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Consumables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContainerDetails",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContainerDetails", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_ContainerDetails_Containers_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Containers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GatheringDetails",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GatheringDetails", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_GatheringDetails_Gatherings_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Gatherings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MiniPetDetails",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    MinipetId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MiniPetDetails", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_MiniPetDetails_MiniPets_ItemId",
                        column: x => x.ItemId,
                        principalTable: "MiniPets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ToolDetails",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Charges = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolDetails", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_ToolDetails_Tools_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Tools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrinketDetails",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    AttributeAdjustment = table.Column<decimal>(type: "TEXT", nullable: false),
                    SuffixItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    SecondarySuffixItemId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrinketDetails", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_TrinketDetails_Trinkets_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Trinkets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UpgradeComponentDetails",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Suffix = table.Column<string>(type: "TEXT", nullable: false),
                    Flags = table.Column<string>(type: "TEXT", nullable: false),
                    InfusionUpgradeFlags = table.Column<string>(type: "TEXT", nullable: false),
                    Bonuses = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpgradeComponentDetails", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_UpgradeComponentDetails_UpgradeComponents_ItemId",
                        column: x => x.ItemId,
                        principalTable: "UpgradeComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeaponDetails",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    DamageType = table.Column<string>(type: "TEXT", nullable: false),
                    MinPower = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxPower = table.Column<int>(type: "INTEGER", nullable: false),
                    Defense = table.Column<int>(type: "INTEGER", nullable: false),
                    AttributeAdjustment = table.Column<decimal>(type: "TEXT", nullable: false),
                    SuffixItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    SecondarySuffixItemId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeaponDetails", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_WeaponDetails_Weapons_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Weapons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArmorInfixUpgrades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArmorDetailsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArmorInfixUpgrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArmorInfixUpgrades_ArmorDetails_ArmorDetailsId",
                        column: x => x.ArmorDetailsId,
                        principalTable: "ArmorDetails",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArmorInfixUpgrades_InfixUpgrade_Id",
                        column: x => x.Id,
                        principalTable: "InfixUpgrade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArmorInfusionSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArmorDetailsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArmorInfusionSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArmorInfusionSlots_ArmorDetails_ArmorDetailsId",
                        column: x => x.ArmorDetailsId,
                        principalTable: "ArmorDetails",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArmorInfusionSlots_ItemInfusionSlot_Id",
                        column: x => x.Id,
                        principalTable: "ItemInfusionSlot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArmorStatChoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArmorDetailsId = table.Column<int>(type: "INTEGER", nullable: false),
                    StatId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArmorStatChoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArmorStatChoices_ArmorDetails_ArmorDetailsId",
                        column: x => x.ArmorDetailsId,
                        principalTable: "ArmorDetails",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BackItemInfixUpgrades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BackItemDetailsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackItemInfixUpgrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackItemInfixUpgrades_BackItemDetails_BackItemDetailsId",
                        column: x => x.BackItemDetailsId,
                        principalTable: "BackItemDetails",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BackItemInfixUpgrades_InfixUpgrade_Id",
                        column: x => x.Id,
                        principalTable: "InfixUpgrade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BackItemInfusionSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BackItemDetailsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackItemInfusionSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackItemInfusionSlots_BackItemDetails_BackItemDetailsId",
                        column: x => x.BackItemDetailsId,
                        principalTable: "BackItemDetails",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BackItemInfusionSlots_ItemInfusionSlot_Id",
                        column: x => x.Id,
                        principalTable: "ItemInfusionSlot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BackItemStatChoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BackItemDetailsId = table.Column<int>(type: "INTEGER", nullable: false),
                    StatId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackItemStatChoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackItemStatChoices_BackItemDetails_BackItemDetailsId",
                        column: x => x.BackItemDetailsId,
                        principalTable: "BackItemDetails",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsumableExtraRecipes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConsumableId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecipeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumableExtraRecipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumableExtraRecipes_ConsumableDetails_ConsumableId",
                        column: x => x.ConsumableId,
                        principalTable: "ConsumableDetails",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsumableSkins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConsumableId = table.Column<int>(type: "INTEGER", nullable: false),
                    SkinId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumableSkins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumableSkins_ConsumableDetails_ConsumableId",
                        column: x => x.ConsumableId,
                        principalTable: "ConsumableDetails",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrinketInfixUpgrades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrinketDetailsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrinketInfixUpgrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrinketInfixUpgrades_InfixUpgrade_Id",
                        column: x => x.Id,
                        principalTable: "InfixUpgrade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrinketInfixUpgrades_TrinketDetails_TrinketDetailsId",
                        column: x => x.TrinketDetailsId,
                        principalTable: "TrinketDetails",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrinketInfusionSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrinketDetailsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrinketInfusionSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrinketInfusionSlots_ItemInfusionSlot_Id",
                        column: x => x.Id,
                        principalTable: "ItemInfusionSlot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrinketInfusionSlots_TrinketDetails_TrinketDetailsId",
                        column: x => x.TrinketDetailsId,
                        principalTable: "TrinketDetails",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrinketStatChoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrinketDetailsId = table.Column<int>(type: "INTEGER", nullable: false),
                    StatId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrinketStatChoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrinketStatChoices_TrinketDetails_TrinketDetailsId",
                        column: x => x.TrinketDetailsId,
                        principalTable: "TrinketDetails",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UpgradeComponentInfixUpgrades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UpgradeComponentDetailsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpgradeComponentInfixUpgrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UpgradeComponentInfixUpgrades_InfixUpgrade_Id",
                        column: x => x.Id,
                        principalTable: "InfixUpgrade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UpgradeComponentInfixUpgrades_UpgradeComponentDetails_UpgradeComponentDetailsId",
                        column: x => x.UpgradeComponentDetailsId,
                        principalTable: "UpgradeComponentDetails",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeaponInfixUpgrades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WeaponDetailsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeaponInfixUpgrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeaponInfixUpgrades_InfixUpgrade_Id",
                        column: x => x.Id,
                        principalTable: "InfixUpgrade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WeaponInfixUpgrades_WeaponDetails_WeaponDetailsId",
                        column: x => x.WeaponDetailsId,
                        principalTable: "WeaponDetails",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeaponInfusionSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WeaponDetailsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeaponInfusionSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeaponInfusionSlots_ItemInfusionSlot_Id",
                        column: x => x.Id,
                        principalTable: "ItemInfusionSlot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WeaponInfusionSlots_WeaponDetails_WeaponDetailsId",
                        column: x => x.WeaponDetailsId,
                        principalTable: "WeaponDetails",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeaponStatChoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WeaponDetailsId = table.Column<int>(type: "INTEGER", nullable: false),
                    StatId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeaponStatChoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeaponStatChoices_WeaponDetails_WeaponDetailsId",
                        column: x => x.WeaponDetailsId,
                        principalTable: "WeaponDetails",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountItemObservations_AccountId_Container_ItemId_Id",
                table: "AccountItemObservations",
                columns: new[] { "AccountId", "Container", "ItemId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountWalletObservations_AccountId_CurrencyId_Id",
                table: "AccountWalletObservations",
                columns: new[] { "AccountId", "CurrencyId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ArmorInfixUpgrades_ArmorDetailsId",
                table: "ArmorInfixUpgrades",
                column: "ArmorDetailsId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArmorInfusionSlots_ArmorDetailsId",
                table: "ArmorInfusionSlots",
                column: "ArmorDetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_ArmorStatChoices_ArmorDetailsId",
                table: "ArmorStatChoices",
                column: "ArmorDetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_BackItemInfixUpgrades_BackItemDetailsId",
                table: "BackItemInfixUpgrades",
                column: "BackItemDetailsId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackItemInfusionSlots_BackItemDetailsId",
                table: "BackItemInfusionSlots",
                column: "BackItemDetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_BackItemStatChoices_BackItemDetailsId",
                table: "BackItemStatChoices",
                column: "BackItemDetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumableExtraRecipes_ConsumableId",
                table: "ConsumableExtraRecipes",
                column: "ConsumableId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumableSkins_ConsumableId",
                table: "ConsumableSkins",
                column: "ConsumableId");

            migrationBuilder.CreateIndex(
                name: "IX_InfixUpgradeAttributes_InfixUpgradeId",
                table: "InfixUpgradeAttributes",
                column: "InfixUpgradeId");

            migrationBuilder.CreateIndex(
                name: "IX_InfixUpgradeBuffs_InfixUpgradeId",
                table: "InfixUpgradeBuffs",
                column: "InfixUpgradeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemFlags_ItemId",
                table: "ItemFlags",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemGameTypes_ItemId",
                table: "ItemGameTypes",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemInfusionSlotFlags_InfusionSlotId",
                table: "ItemInfusionSlotFlags",
                column: "InfusionSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemRestrictions_ItemId",
                table: "ItemRestrictions",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceSnapshots_ItemId_TimestampUtc",
                table: "PriceSnapshots",
                columns: new[] { "ItemId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeDisciplines_RecipeId",
                table: "RecipeDisciplines",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeFlags_RecipeId",
                table: "RecipeFlags",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_RecipeId",
                table: "RecipeIngredients",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_TrinketInfixUpgrades_TrinketDetailsId",
                table: "TrinketInfixUpgrades",
                column: "TrinketDetailsId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrinketInfusionSlots_TrinketDetailsId",
                table: "TrinketInfusionSlots",
                column: "TrinketDetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_TrinketStatChoices_TrinketDetailsId",
                table: "TrinketStatChoices",
                column: "TrinketDetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_UpgradeComponentInfixUpgrades_UpgradeComponentDetailsId",
                table: "UpgradeComponentInfixUpgrades",
                column: "UpgradeComponentDetailsId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeaponInfixUpgrades_WeaponDetailsId",
                table: "WeaponInfixUpgrades",
                column: "WeaponDetailsId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeaponInfusionSlots_WeaponDetailsId",
                table: "WeaponInfusionSlots",
                column: "WeaponDetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_WeaponStatChoices_WeaponDetailsId",
                table: "WeaponStatChoices",
                column: "WeaponDetailsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountContainerSlots");

            migrationBuilder.DropTable(
                name: "AccountItemObservations");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "AccountWalletObservations");

            migrationBuilder.DropTable(
                name: "ArmorInfixUpgrades");

            migrationBuilder.DropTable(
                name: "ArmorInfusionSlots");

            migrationBuilder.DropTable(
                name: "ArmorStatChoices");

            migrationBuilder.DropTable(
                name: "BackItemInfixUpgrades");

            migrationBuilder.DropTable(
                name: "BackItemInfusionSlots");

            migrationBuilder.DropTable(
                name: "BackItemStatChoices");

            migrationBuilder.DropTable(
                name: "BagDetails");

            migrationBuilder.DropTable(
                name: "CharacterItemSlots");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "ConsumableExtraRecipes");

            migrationBuilder.DropTable(
                name: "ConsumableSkins");

            migrationBuilder.DropTable(
                name: "ContainerDetails");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "GatheringDetails");

            migrationBuilder.DropTable(
                name: "Gizmos");

            migrationBuilder.DropTable(
                name: "InfixUpgradeAttributes");

            migrationBuilder.DropTable(
                name: "InfixUpgradeBuffs");

            migrationBuilder.DropTable(
                name: "ItemCraftCosts");

            migrationBuilder.DropTable(
                name: "ItemFlags");

            migrationBuilder.DropTable(
                name: "ItemGameTypes");

            migrationBuilder.DropTable(
                name: "ItemInfusionSlotFlags");

            migrationBuilder.DropTable(
                name: "ItemRestrictions");

            migrationBuilder.DropTable(
                name: "MaterialCategories");

            migrationBuilder.DropTable(
                name: "MaterialCategoryItems");

            migrationBuilder.DropTable(
                name: "MiniPetDetails");

            migrationBuilder.DropTable(
                name: "PriceSnapshots");

            migrationBuilder.DropTable(
                name: "RecipeDisciplines");

            migrationBuilder.DropTable(
                name: "RecipeFlags");

            migrationBuilder.DropTable(
                name: "RecipeIngredients");

            migrationBuilder.DropTable(
                name: "ToolDetails");

            migrationBuilder.DropTable(
                name: "TrinketInfixUpgrades");

            migrationBuilder.DropTable(
                name: "TrinketInfusionSlots");

            migrationBuilder.DropTable(
                name: "TrinketStatChoices");

            migrationBuilder.DropTable(
                name: "UpgradeComponentInfixUpgrades");

            migrationBuilder.DropTable(
                name: "WeaponInfixUpgrades");

            migrationBuilder.DropTable(
                name: "WeaponInfusionSlots");

            migrationBuilder.DropTable(
                name: "WeaponStatChoices");

            migrationBuilder.DropTable(
                name: "ArmorDetails");

            migrationBuilder.DropTable(
                name: "BackItemDetails");

            migrationBuilder.DropTable(
                name: "Bags");

            migrationBuilder.DropTable(
                name: "ConsumableDetails");

            migrationBuilder.DropTable(
                name: "Containers");

            migrationBuilder.DropTable(
                name: "Gatherings");

            migrationBuilder.DropTable(
                name: "MiniPets");

            migrationBuilder.DropTable(
                name: "Recipes");

            migrationBuilder.DropTable(
                name: "Tools");

            migrationBuilder.DropTable(
                name: "TrinketDetails");

            migrationBuilder.DropTable(
                name: "UpgradeComponentDetails");

            migrationBuilder.DropTable(
                name: "InfixUpgrade");

            migrationBuilder.DropTable(
                name: "ItemInfusionSlot");

            migrationBuilder.DropTable(
                name: "WeaponDetails");

            migrationBuilder.DropTable(
                name: "Armors");

            migrationBuilder.DropTable(
                name: "BackItems");

            migrationBuilder.DropTable(
                name: "Consumables");

            migrationBuilder.DropTable(
                name: "Trinkets");

            migrationBuilder.DropTable(
                name: "UpgradeComponents");

            migrationBuilder.DropTable(
                name: "Weapons");

            migrationBuilder.DropTable(
                name: "Items");
        }
    }
}
