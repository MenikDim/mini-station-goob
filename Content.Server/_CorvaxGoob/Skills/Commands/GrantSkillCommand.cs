using System.Linq;
using Content.Server.Administration;
using SkillTypes = Content.Shared._CorvaxGoob.Skills.Skills;
using Content.Shared._CorvaxGoob.Skills;
using Content.Shared.Administration;
using Content.Shared.Mobs.Components;
using Robust.Shared.Console;

namespace Content.Server._CorvaxGoob.Skills.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class GrantSkillCommand : LocalizedEntityCommands
{
    [Dependency] private readonly ILocalizationManager _localization = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;

    public override string Command => "grantskill";

    public override void Execute(IConsoleShell shell, string arg, string[] args)
    {
        if (args.Length < 2)
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

        var skills = new HashSet<SkillTypes>();

        for (var i = 1; i < args.Length; i++)
        {
            if (!Enum.TryParse<SkillTypes>(args[i], out var skill))
            {
                shell.WriteError(Loc.GetString("cmd-grantskill-not-a-skill-type", ("args", args[i])));
                return;
            }

            skills.Add(skill);
        }

        _skills.GrantSkill(entity.Value, skills);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.Components<MobStateComponent>(args[0], EntityManager),
                _localization.GetString("shell-argument-net-entity"));
        }

        HashSet<SkillTypes> existingSkills = new();
        if (NetEntity.TryParse(args[0], out var netEntity)
            && EntityManager.TryGetEntity(netEntity, out var entity)
            && _skills.TryGetSkills(entity!.Value, out var found))
        {
            existingSkills = found;
        }

        var alreadyEnteredSkills = new HashSet<SkillTypes>();
        for (var i = 1; i < args.Length - 1; i++)
        {
            if (Enum.TryParse<SkillTypes>(args[i], out var skill))
                alreadyEnteredSkills.Add(skill);
        }

        var allExcludedSkills = new HashSet<SkillTypes>(existingSkills);
        allExcludedSkills.UnionWith(alreadyEnteredSkills);

        return CompletionResult.FromOptions(Enum.GetValues<SkillTypes>()
            .Where(skill => !allExcludedSkills.Contains(skill))
            .Select(skill => skill.ToString())
            .Where(name => name.StartsWith(args[^1], StringComparison.OrdinalIgnoreCase))
            .Select(name => new CompletionOption(name)));
    }
}
