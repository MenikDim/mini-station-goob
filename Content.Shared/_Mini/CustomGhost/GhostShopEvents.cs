using Robust.Shared.Serialization;

namespace Content.Shared._Mini.CustomGhost;

[Serializable, NetSerializable]
public sealed class GhostShopOpenRequestEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class GhostShopBuyRequestEvent : EntityEventArgs
{
    public string ThemeId { get; }

    public GhostShopBuyRequestEvent(string themeId)
    {
        ThemeId = themeId;
    }
}

[Serializable, NetSerializable]
public sealed class GhostShopSelectRequestEvent : EntityEventArgs
{
    public string? ThemeId { get; }

    public GhostShopSelectRequestEvent(string? themeId)
    {
        ThemeId = themeId;
    }
}

[Serializable, NetSerializable]
public sealed class GhostShopStateEvent : EntityEventArgs
{
    public int Balance { get; }
    public List<GhostThemeEntry> Themes { get; }

    public GhostShopStateEvent(int balance, List<GhostThemeEntry> themes)
    {
        Balance = balance;
        Themes = themes;
    }
}

[Serializable, NetSerializable]
public sealed class GhostThemeEntry
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public int Price { get; }
    public bool Owned { get; }
    public bool Selected { get; }
    public string IconRsiPath { get; }
    public string IconRsiState { get; }

    public GhostThemeEntry(string id, string name, string description, int price, bool owned, bool selected, string iconRsiPath, string iconRsiState)
    {
        Id = id;
        Name = name;
        Description = description;
        Price = price;
        Owned = owned;
        Selected = selected;
        IconRsiPath = iconRsiPath;
        IconRsiState = iconRsiState;
    }
}
