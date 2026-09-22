using Godot;
using System;
using System.Collections.Generic;

// 메이플스토리 스타일 스킬창: SkillData.RequiredLevel(0/8/30/70) 기준 4개 탭(초보자/1차/2차/3차)으로 나눠 보여주고 SP를 투자한다.
public partial class SkillUI : UIWindowPanel
{
    private static readonly int[] TierRequiredLevels = { 0, 8, 30, 70 };
    private static readonly string[] TierNames = { "초보자", "1차", "2차", "3차" };

    [Export] public NodePath SpLabelPath = "Panel/VBox/SpLabel";
    [Export] public NodePath TabContainerPath = "Panel/VBox/TabContainer";

    private Label _spLabel;
    private TabContainer _tabContainer;
    private readonly List<VBoxContainer> _tierContainers = new();

    public override void _Ready()
    {
        WindowContentId = "UI_SKILL";
        base._Ready();

        _spLabel = GetNode<Label>(SpLabelPath);
        _tabContainer = GetNode<TabContainer>(TabContainerPath);

        foreach (string tierName in TierNames)
        {
            var vbox = new VBoxContainer { Name = tierName };
            _tabContainer.AddChild(vbox);
            _tierContainers.Add(vbox);
        }
    }

    protected override void OnOpened() => RefreshSkillList();

    private void RefreshSkillList()
    {
        if (SkillManager.Instance == null || PlayerStats.Instance == null)
        {
            return;
        }

        _spLabel.Text = $"SP: {PlayerStats.Instance.SkillPoints}";

        foreach (VBoxContainer vbox in _tierContainers)
        {
            foreach (Node child in vbox.GetChildren())
            {
                child.QueueFree();
            }
        }

        foreach (SkillData skill in SkillManager.Instance.AllSkills)
        {
            int tierIndex = Array.IndexOf(TierRequiredLevels, skill.RequiredLevel);
            if (tierIndex < 0)
            {
                continue; // 0/8/30/70 티어에 해당하지 않는 스킬은 표시하지 않는다.
            }

            AddSkillRow(_tierContainers[tierIndex], skill);
        }
    }

    private void AddSkillRow(VBoxContainer parent, SkillData skill)
    {
        int level = SkillManager.Instance.GetSkillLevel(skill.SkillId);

        var row = new HBoxContainer();

        if (skill.Icon != null)
        {
            var icon = new TextureRect
            {
                Texture = skill.Icon,
                CustomMinimumSize = new Vector2(32, 32),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
            row.AddChild(icon);
        }

        var nameLabel = new Label { Text = skill.SkillName, CustomMinimumSize = new Vector2(140, 0) };
        var levelLabel = new Label { Text = $"Lv. {level} / {skill.MaxSkillLevel}", CustomMinimumSize = new Vector2(80, 0) };
        var investButton = new Button { Text = "+" };
        investButton.Pressed += () =>
        {
            SkillManager.Instance.InvestPoint(skill.SkillId);
            RefreshSkillList();
        };

        row.AddChild(nameLabel);
        row.AddChild(levelLabel);
        row.AddChild(investButton);
        parent.AddChild(row);
    }
}
