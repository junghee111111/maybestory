using Godot;

public partial class BossMob : Mob
{
    [Export] public int Phase = 1;
    // 슈퍼아머(넉백 무시), 페이즈별 특수 스킬 패턴 AI 구현
}