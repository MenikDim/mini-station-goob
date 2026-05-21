using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Mobs.Components;
using Robust.Shared.Console;

namespace Content.Server._CorvaxGoob.Skills.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class GrantAllSkillsCommand : LocalizedEntityCommands
{
    [Dependency] private readonly ILocalizationManager _localization = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;

    public override string Command => "grantallskills";

    public override void Execute(IConsoleShell shell, string arg, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(_localization.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var id))
        {
            shell.WriteError(_localization.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        if (!EntityManager.TryGetEntity(id, out var entity))
        {
            shell.WriteError(_localization.GetString("shell-invalid-entity-id"));
            return;
        }

        if (!EntityManager.HasComponent<MobStateComponent>(entity.Value))
        {
            shell.WriteError(_localization.GetString("shell-invalid-entity-id"));
            return;
        }

        _skills.GrantAllSkills(entity.Value);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.Components<MobStateComponent>(args[0], EntityManager),
                _localization.GetString("shell-argument-net-entity"));
        }

        return CompletionResult.Empty;
    }
}
