using System;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>An item the account plans to make or buy, and how many.</summary>
public sealed record Goal
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string AccountId { get; init; } = "";

    public int ItemId { get; init; }

    public string ItemName { get; init; } = "";

    public int Count { get; init; } = 1;
}
