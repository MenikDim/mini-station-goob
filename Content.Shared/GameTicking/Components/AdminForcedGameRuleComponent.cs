namespace Content.Shared.GameTicking.Components;

/// <summary>
/// Marks a game rule added via the admin <c>addgamerule</c> command so it is not cancelled by round-mode blockers.
/// </summary>
[RegisterComponent]
public sealed partial class AdminForcedGameRuleComponent : Component;
