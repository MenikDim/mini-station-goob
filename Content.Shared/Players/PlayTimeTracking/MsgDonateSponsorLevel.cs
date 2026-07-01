// SPDX-FileCopyrightText: 2026 Casha
// SPDX-License-Identifier: MIT

using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Players.PlayTimeTracking;

/// <summary>
/// Discord donate sponsor level (0 = none). While &gt; 0, playtime requirements are bypassed.
/// </summary>
public sealed class MsgDonateSponsorLevel : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public int Level;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Level = buffer.ReadVariableInt32();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.WriteVariableInt32(Level);
    }
}
