using Content.Shared._CorvaxGoob.Skills;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using SkillTypes = Content.Shared._CorvaxGoob.Skills.Skills;

namespace Content.Client._CorvaxGoob.Skills;

[UsedImplicitly]
public sealed class SkillTrainingBoundUserInterface : BoundUserInterface
{
    private SkillTrainingWindow? _window;
    private readonly List<SkillTypes> _skills = new();

    public SkillTrainingBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SkillTrainingWindow>();
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not SkillTrainingBoundUserInterfaceState uiState)
            return;

        _skills.Clear();
        _skills.AddRange(uiState.Skills);

        _window.SkillButtonContainer.RemoveAllChildren();

        foreach (var skill in _skills)
        {
            var button = new Button
            {
                Text = Loc.GetString($"skill-training-name-{skill}"),
                HorizontalExpand = true,
                MinHeight = 36,
            };
            button.StyleClasses.Add("OpenBoth");

            var captured = skill;
            button.OnPressed += _ =>
            {
                SendMessage(new SkillTrainingSelectSkillMessage(captured));
                Close();
            };

            _window.SkillButtonContainer.AddChild(button);
        }
    }
}
