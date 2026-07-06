using Robust.Shared.Serialization;

namespace Content.Goobstation.Common.Barks;

[Serializable, NetSerializable]
public sealed class PlayBarkEvent(NetEntity sourceUid, string message, bool whisper) : EntityEventArgs
{
    public NetEntity SourceUid { get; private set; } = sourceUid;
    public string Message { get; private set; } = message;
    public bool Whisper { get; private set; } = whisper;
}

[Serializable, NetSerializable]
public sealed class PreviewBarkEvent(string barkProtoID) : EntityEventArgs
{
    public string BarkProtoID { get; private set; } = barkProtoID;
}
