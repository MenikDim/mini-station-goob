using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._Amour.Stickers;

[Prototype("sticker")]
public sealed partial class StickerPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("texture")]
    public ResPath TexturePath { get; private set; } = default!;
}
