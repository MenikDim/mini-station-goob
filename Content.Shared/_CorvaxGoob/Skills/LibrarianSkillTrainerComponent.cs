using Robust.Shared.GameStates;

namespace Content.Shared._CorvaxGoob.Skills;

/// <summary>
/// Marker on the librarian body at round spawn. Grants universal skill training; not tied to mind or ID.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LibrarianSkillTrainerComponent : Component;
