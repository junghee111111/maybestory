public partial class MobMiddle : Mob
{
    // 중형 몬스터, 적당한 넉백 저항
    public MobMiddle()
    {
        KnockbackThresholdPercentage = 7.0f;
        KnockbackHorizontalForce = 10.0f;
        KnockbackVerticalForce = 7.0f;
    }

    protected override float GetPartDamageMultiplier(string partName) => partName switch
    {
        "Head" => 1.4f, // 헤드샷 약점
        "Leg" => 0.85f, // 다리 방어
        _ => 1.0f,      // 몸통
    };
}