using Godot;
public partial class MobLarge : Mob
{
    // 다중 Area3D 히트박스로부터 맞은 부위에 따른 대미지 증감 계산
    public void TakePartDamage(string partName, int baseDamage)
    {
        float multiplier = partName switch
        {
            "Head" => 1.5f,   // 헤드샷 약점
            "Leg" => 0.8f,    // 다리 방어
            _ => 1.0f         // 몸통
        };

        int finalDmg = Mathf.RoundToInt(baseDamage * multiplier);
        TakeDamage(finalDmg, GlobalPosition);
    }
}