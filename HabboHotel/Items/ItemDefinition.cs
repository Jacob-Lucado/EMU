using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Items.Wired.Configuration;
using Plus.HabboHotel.Users.Inventory.Furniture;

namespace Plus.HabboHotel.Items;

public class ItemDefinition
{
    public uint Id { get; set; }
    public int SpriteId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string PublicName { get; set; } = string.Empty;

    /// <summary>The name Habbo's furnidata gives the furni; empty when the row has none.</summary>
    public string FurniName { get; set; } = string.Empty;

    /// <summary>The Habbo furnidata furniline, as in "pet_food".</summary>
    public string FurniLine { get; set; } = string.Empty;

    /// <summary>The name a player sees: the furnidata name, or the public name when there is none.</summary>
    public string DisplayName => FurniName.Length > 0 ? FurniName : PublicName;
    public ItemType Type { get; set; }

    /// <summary>The furniture row's type letter: s, i, p, e, r or b. Purchase reads this; <see cref="Type"/> only distinguishes floor and wall.</summary>
    public string ProductType { get; set; } = "s";
    public FurniCategory Category { get; set; } = FurniCategory.Default;
    public int Width { get; set; }
    public int Length { get; set; }
    public double Height { get; set; }
    public bool Stackable { get; set; }
    public bool Walkable { get; set; }
    public bool IsSeat { get; set; }
    public bool AllowEcotronRecycle { get; set; }
    public bool AllowTrade { get; set; }
    public bool AllowMarketplaceSell { get; set; }
    public bool AllowGift { get; set; }
    public bool AllowInventoryStack { get; set; }

    /// TODO @80O: Convert to string so plugins can add new interactions.
    public InteractionType InteractionType { get; set; }
    public string InteractionName { get; set; } = string.Empty;
    public WiredBoxDescriptor? WiredDescriptor => WiredBoxRegistry.TryGet(InteractionName, out var descriptor)
        || WiredBoxRegistry.TryGet(ItemName, out descriptor) ? descriptor : null;
    public int BehaviourData { get; set; }

    /// <summary>Credit furni the owner can exchange for its credit value. Habbo names every CFC_ credit furni "Non-redeemable item".</summary>
    public bool RedeemableForCredits => InteractionType == InteractionType.Exchange && !ItemName.StartsWith("CFC_", StringComparison.OrdinalIgnoreCase);

    /// <summary>The figure set ids a purchasable clothing item unlocks: its Habbo furnidata customparams.</summary>
    public IReadOnlyList<int> FigureSetIds { get; set; } = [];

    /// <summary>The Habbo furnidata customparams, as in "15 2,3 50,50" for a horse hair dye.</summary>
    public string CustomParams { get; set; } = string.Empty;

    /// <summary>The pet a pet package holds and the furni it leaves behind, from its Habbo customparams; null for other furni.</summary>
    public PetPackage? PetPackage { get; set; }

    /// <summary>The furnidata revision of the furni's asset. Newer furni have higher revisions.</summary>
    public int Revision { get; set; }

    public int Modes { get; set; }
    public List<int> VendingIds { get; set; } = new();
    public List<double> AdjustableHeights { get; set; } = new();
    public int EffectId { get; set; }

    /// TODO @80O: Should be removed, use unique interaction name instead.
    public WiredBoxType WiredType { get; set; }

    /// TODO @80O: This is dumb, remove it.
    public bool IsRare { get; set; }

    /// <summary>Lets a furni without asset directions face odd (diagonal) rotations.</summary>
    public bool ExtraRot { get; set; }

    /// <summary>The directions the furni's asset has, in wired units (0-7), sorted; empty when unknown. See <see cref="FurnitureDirections"/>.</summary>
    public IReadOnlyList<int> Directions { get; set; } = [];

    /// <summary>The furnidata defaultdir (furniture.default_dir of the classname's furnidata row), in wired units.</summary>
    public int DefaultDirection { get; set; }
}
