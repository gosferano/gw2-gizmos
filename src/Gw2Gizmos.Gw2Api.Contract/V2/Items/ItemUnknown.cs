namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

/// <summary>
/// An item of a type this contract doesn't know yet, read with only the fields shared by all of them. Its
/// <see cref="Item.Type"/> holds the type the API sent.
/// </summary>
public sealed class ItemUnknown : Item { }
