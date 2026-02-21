using Content.Goobstation.Shared.Disease;
using Content.Goobstation.Shared.Disease.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.Disease.Effects;

/// <summary>
/// Уменьшает прогресс болезней выбранного типа на цели.
/// </summary>
public sealed partial class DiseaseProgressChange : EntityEffect
{
    /// <summary>
    /// Типы болезней, на которые действует эффект.
    /// </summary>
    [DataField]
    public ProtoId<DiseaseTypePrototype> AffectedType;

    /// <summary>
    /// Величина изменения прогресса болезни.
    /// </summary>
    [DataField]
    public float ProgressModifier = -0.02f;

    [DataField]
    public bool Scaled = true;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-disease-progress-change",
            ("chance", Probability),
            ("type", prototype.Index<DiseaseTypePrototype>(AffectedType).LocalizedName),
            ("amount", ProgressModifier));
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!args.EntityManager.TryGetComponent<DiseaseCarrierComponent>(args.TargetEntity, out var carrier))
            return;

        foreach (var diseaseUid in carrier.Diseases.ContainedEntities)
        {
            if (!args.EntityManager.TryGetComponent<DiseaseComponent>(diseaseUid, out var disease)
                || disease.DiseaseType != AffectedType)
                continue;

            var sys = args.EntityManager.System<DiseaseSystem>();
            var amt = ProgressModifier;
            if (args is EntityEffectReagentArgs reagentArgs)
            {
                if (Scaled)
                    amt *= reagentArgs.Quantity.Float();
                amt *= reagentArgs.Scale.Float();
            }

            sys.ChangeInfectionProgress((diseaseUid, disease), amt);
        }
    }
}
