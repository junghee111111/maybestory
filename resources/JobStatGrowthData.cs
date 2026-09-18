using Godot;
using System;

[GlobalClass]
public partial class JobStatGrowthData : Resource
{
	[Export] public JobType Job = JobType.Beginner;

	[ExportGroup("HP 증가량")]
	[Export] public int MinHpGain = 10;
	[Export] public int MaxHpGain = 10;

	[ExportGroup("MP 증가량")]
	[Export] public int MinMpGain = 10;
	[Export] public int MaxMpGain = 10;

	[ExportGroup("MP Int당 보너스")][Export] public float MpBonusPerInt = 0.1f;
}
