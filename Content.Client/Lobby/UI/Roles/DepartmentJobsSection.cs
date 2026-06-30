using Content.Client.Stylesheets;
using System.Numerics;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI.Roles;

/// <summary>
/// Collapsible department block for the job priority editor.
/// </summary>
public sealed class DepartmentJobsSection : Collapsible
{
    public BoxContainer JobsContent { get; }

    private readonly DepartmentPrototype _department;
    private readonly IPrototypeManager _prototype;
    private readonly CollapsibleHeading _heading;
    private readonly Color _mutedAccent;
    private readonly List<ProtoId<JobPrototype>> _jobIds = [];

    public Color MutedAccent { get; }

    public DepartmentJobsSection(DepartmentPrototype department, IPrototypeManager prototype, Texture? iconTexture)
    {
        _department = department;
        _prototype = prototype;
        MutedAccent = LobbyUiStyles.MuteDepartmentColor(department.Color);
        _mutedAccent = MutedAccent;

        HorizontalExpand = true;
        Margin = new Thickness(0, 0, 0, 6);
        BodyVisible = false;

        var departmentName = Loc.GetString(department.Name);

        _heading = new DepartmentCollapsibleHeading(departmentName, iconTexture)
        {
            HorizontalExpand = true,
            MinSize = new Vector2(0, 46),
            ToolTip = Loc.GetString("humanoid-profile-editor-jobs-amount-in-department-tooltip",
                ("departmentName", departmentName)),
        };

        ApplyHeaderStyle(_heading, _mutedAccent);

        var body = new CollapsibleBody
        {
            Margin = new Thickness(0, 1, 0, 0),
        };

        var bodyPanel = new PanelContainer
        {
            HorizontalExpand = true,
            PanelOverride = LobbyUiStyles.DepartmentBody(_mutedAccent),
        };

        JobsContent = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 0,
            Margin = new Thickness(4, 2, 4, 3),
        };

        bodyPanel.AddChild(JobsContent);
        body.AddChild(bodyPanel);

        AddChild(_heading);
        AddChild(body);
    }

    public void TrackJob(ProtoId<JobPrototype> jobId) => _jobIds.Add(jobId);

    public void UpdateSummary(HumanoidCharacterProfile? profile)
    {
        var departmentName = Loc.GetString(_department.Name);

        if (profile == null)
        {
            _heading.Title = departmentName;
            _heading.Label.FontColorOverride = LobbyUiStyles.HeaderText;
            return;
        }

        ProtoId<JobPrototype>? highJob = null;
        var activeCount = 0;

        foreach (var jobId in _jobIds)
        {
            var priority = profile.JobPriorities.GetValueOrDefault(jobId, JobPriority.Never);
            if (priority == JobPriority.Never)
                continue;

            activeCount++;
            if (priority == JobPriority.High)
                highJob = jobId;
        }

        _heading.Title = highJob != null
            ? $"{departmentName}  ·  {_prototype.Index(highJob.Value).LocalizedName}"
            : activeCount > 0
                ? $"{departmentName}  ({activeCount})"
                : departmentName;

        _heading.Label.FontColorOverride = highJob != null
            ? LobbyUiStyles.PriorityColor(JobPriority.High)
            : LobbyUiStyles.HeaderText;
    }

    private static void ApplyHeaderStyle(CollapsibleHeading heading, Color accent)
    {
        heading.StyleBoxOverride = LobbyUiStyles.DepartmentHeader(accent);
        heading.Label.FontColorOverride = LobbyUiStyles.HeaderText;
        heading.Label.StyleClasses.Add(StyleNano.StyleClassLabelBig);
    }
}
