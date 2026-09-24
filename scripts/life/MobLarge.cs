public partial class MobLarge : Mob
{
    public MobLarge()
    {
        KnockbackThresholdPercentage = 9.0f;
        KnockbackHorizontalForce = 8.0f;
        KnockbackVerticalForce = 5.0f;
    }

    protected override float GetPartDamageMultiplier(string partName) => partName switch
    {
        "Head" => 1.5f, // 헤드샷 약점
        "Leg" => 0.8f,  // 다리 방어
        _ => 1.0f,      // 몸통
    };
}