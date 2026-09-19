using System.ComponentModel;
using Godot;

public enum SkillType
{
    Passive,           // 패시브 (MP 이터, MP 증가량 향상 등)
    ActiveBuff,        // 액티브 자가 버프 (인빈서블, 매직가드 등)
    ActiveProjectile,  // 액티브 투사체 공격 (홀리 에로우, 에너지 볼트 등)
    ActiveAutoTarget,  // 액티브 범위 자동 타게팅 공격 (힐, 홀리 포커스 등)
    ActiveSummon       // 액티브 소환수 (바하뮤트, 드래곤 등)
}

[GlobalClass]
public partial class SkillData : Resource
{
    [ExportGroup("1. 기본 메타데이터")]
    [Export] public string SkillId = "holy_arrow";
    [Export] public string SkillName = "홀리 에로우";
    [Export(PropertyHint.MultilineText)] public string Description = "성스러운 화살을 만들어 전방의 적에게 발사한다.";
    [Export] public Texture2D Icon;
    [Export] public SkillType Type = SkillType.ActiveProjectile;
    [Export] public int RequiredLevel = 1;
    [Export] public int MaxSkillLevel = 30;

    [ExportGroup("2. 소모값 및 쿨타임")]

    [Export] public int BaseHpCost = 12;
    [Export] public int HpCostPerLevel = 1;
    [Export] public int BaseMpCost = 12;
    [Export] public int MpCostPerLevel = 1;
    [Export] public float Cooldown = 0.0f;
    [Export] public float PreDelay = 0.0f;
    [Export] public string[] CastAnimationName = ["Swing1", "Swing2", "Swing3", "Stab1"]; // 발동 시 재생할 캐릭터 애니메이션

    [ExportGroup("3. 공격 스킬 공통 계수")]
    [Export] public float BaseDamageMultiplier = 1.2f;    // 1레벨 대미지 계수 (마법 공격력 대비)
    [Export] public float DamageMultiplierPerLevel = 0.04f; // 레벨당 계수 증가폭
    [Export] public int TargetCount = 1;                   // 타격 가능한 몬스터 수
    [Export] public bool UseAttackRangeBasedOnWeaponMesh = true;
    [Export] public float AttackRangeW = 15.0f;
    [Export] public float AttackRangeH = 10.0f;
    [Export] public float AttackRangeD = 5.0f;
    [Export] public Vector3 AttackRangeOffset = new(0, 0, 0);

    [ExportGroup("4. 투사체 설정 (ActiveProjectile)")]
    [Export] public PackedScene ProjectileScene;          // 발사될 투사체 프리팹 (.tscn)
    [Export] public float ProjectileSpeed = 20.0f;

    [ExportGroup("5. VFX 설정")]
    [Export] public PackedScene PreEffectScene;
    [Export] public PackedScene HitVfxScene;
    [Export] public Vector3 HitVfxTargetOffset = new Vector3(0, 0, 0); // 타게팅 중심점 보정치

    [ExportGroup("6. 버프/소환수 설정")]
    [Export] public float BaseDuration = 60.0f;           // 지속시간 (초)
    [Export] public float DurationPerLevel = 5.0f;
    [Export] public int BuffStatValue = 20;               // 버프 수치 (예: 방어력/마력 증가량)
    [Export] public PackedScene SummonPrefab;             // 소환수 프리팹 (.tscn)

    // 기본 생성자 (Godot 리소스 인스턴스화 필수 요구사항)
    public SkillData() { }

    // =========================================================================
    // 런타임 계산 헬퍼 함수
    // =========================================================================
    public int GetMpCost(int level)
        => BaseMpCost + (MpCostPerLevel * Mathf.Max(0, level - 1));

    public int GetHpCost(int level)
        => BaseHpCost + (HpCostPerLevel * Mathf.Max(0, level - 1));

    public float GetDamageMultiplier(int level)
        => BaseDamageMultiplier + (DamageMultiplierPerLevel * Mathf.Max(0, level - 1));

    public float GetDuration(int level)
        => BaseDuration + (DurationPerLevel * Mathf.Max(0, level - 1));
}