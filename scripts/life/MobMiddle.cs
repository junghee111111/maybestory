public partial class MobMiddle : Mob
{
    // 중형 몬스터, 적당한 넉백 저항
    public MobMiddle()
    {
        KnockbackThresholdPercentage = 7.0f;
        KnockbackHorizontalForce = 10.0f;
        KnockbackVerticalForce = 7.0f;
    }
}