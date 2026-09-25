using Godot;
using System;

public partial class MainHudController : Node
{
	[ExportGroup("Player Ref")]
	[Export] public NodePath PlayerStatsPath = "../../LifeContainer/Player/PlayerStats";

	[ExportGroup("UI Nodes")]
	[Export] public Label LevelLabel;
	[Export] public Label NameLabel;
	[Export] public Label JobLabel;

	[Export] public TextureProgressBar HpBar;
	[Export] public Label HpText;

	[Export] public TextureProgressBar MpBar;
	[Export] public Label MpText;

	[Export] public TextureProgressBar ExpBar;
	[Export] public Label ExpText;

	private PlayerStats _playerStats;

	private const float BarTweenDuration = 1.0f;
	private Tween _hpTween;
	private Tween _mpTween;
	private Tween _expTween;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_playerStats = GetNode<PlayerStats>(PlayerStatsPath);

		// 3. 이벤트 구독 및 초기 UI 갱신
		if (_playerStats != null)
		{
			_playerStats.OnStatsChanged += RefreshUI;
			_playerStats.OnLevelUp += RefreshUI;
			_playerStats.OnHpChanged += RefreshHpBar;
			_playerStats.OnMpChanged += RefreshMpBar;
			_playerStats.OnExpChanged += RefreshExpBar;
			RefreshUI();
		}
		else
		{
			GD.PrintErr("[HUDController] PlayerStats를 찾을 수 없습니다.");
		}
	}

	public override void _ExitTree()
	{
		// 메모리 릭 방지를 위한 이벤트 해제
		if (_playerStats != null)
		{
			_playerStats.OnStatsChanged -= RefreshUI;
			_playerStats.OnLevelUp -= RefreshUI;
			_playerStats.OnHpChanged -= RefreshHpBar;
			_playerStats.OnMpChanged -= RefreshMpBar;
			_playerStats.OnExpChanged -= RefreshExpBar;
		}
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void RefreshHpBar()
	{
		// HP 바 & 텍스트
		if (HpBar != null)
		{
			HpBar.MaxValue = _playerStats.MaxHp;
			_hpTween = AnimateBarValue(HpBar, _hpTween, _playerStats.CurrentHp);
		}
		if (HpText != null)
		{
			HpText.Text = $"{_playerStats.CurrentHp} / {_playerStats.MaxHp}";
		}
	}

	private void RefreshMpBar()
	{
		// MP 바 & 텍스트
		if (MpBar != null)
		{
			MpBar.MaxValue = _playerStats.MaxMp;
			_mpTween = AnimateBarValue(MpBar, _mpTween, _playerStats.CurrentMp);
		}
		if (MpText != null)
		{
			MpText.Text = $"{_playerStats.CurrentMp} / {_playerStats.MaxMp}";
		}
	}

	private void RefreshExpBar()
	{
		// EXP 바 & 텍스트
		if (ExpBar != null)
		{
			ExpBar.MaxValue = _playerStats.MaxExp;
			_expTween = AnimateBarValue(ExpBar, _expTween, _playerStats.CurrentExp);
		}

		if (ExpText != null)
		{
			float expPercent = _playerStats.MaxExp > 0
				? (float)_playerStats.CurrentExp / _playerStats.MaxExp * 100.0f
				: 0.0f;
			ExpText.Text = $"{_playerStats.CurrentExp} / {_playerStats.MaxExp} ({expPercent:F2}%)";
		}
	}

	// 진행 중이던 트윈을 정리하고 EaseOut으로 바 값을 목표치까지 부드럽게 움직인다.
	private Tween AnimateBarValue(TextureProgressBar bar, Tween previousTween, float targetValue)
	{
		previousTween?.Kill();

		Tween tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(bar, "value", targetValue, BarTweenDuration);
		return tween;
	}

	private void RefreshUI()
	{
		if (_playerStats == null) return;

		// 레벨 및 이름
		if (LevelLabel != null) LevelLabel.Text = $"Lv. {_playerStats.Level}";
		if (JobLabel != null) JobLabel.Text = $"{_playerStats.CurrentJob}";
		if (NameLabel != null) NameLabel.Text = $"{_playerStats.LifeName}";

		RefreshHpBar();
		RefreshMpBar();
		RefreshExpBar();
	}
}
