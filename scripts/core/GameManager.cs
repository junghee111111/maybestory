using Godot;

// 레벨별 요구 경험치(ExpTable)와 레벨업 연출을 관리하는 전역 매니저. GameWorld에 부착.
public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    public const int MaxLevel = 120;

    [Export] public Godot.Collections.Array<int> ExpTable = BuildDefaultExpTable();
    [Export] public PackedScene VfxLevelUp;

    public override void _Ready()
    {
        Instance = this;
    }

    // 레벨 120은 요구 경험치 0으로 취급해 더 이상 성장하지 않는다.
    public int GetMaxExp(int level)
    {
        int index = Mathf.Clamp(level, 1, MaxLevel) - 1;
        return ExpTable[index];
    }

    public void PlayLevelUpVfx(Vector3 worldPosition)
    {
        if (VfxLevelUp == null) return;

        Node vfxNode = VfxLevelUp.Instantiate();
        AddChild(vfxNode);
        if (vfxNode is Node3D vfx3D)
        {
            vfx3D.GlobalPosition = worldPosition;
        }
    }

    private static Godot.Collections.Array<int> BuildDefaultExpTable()
    {
        var table = new Godot.Collections.Array<int>();
        float exp = 15f;
        for (int level = 1; level <= MaxLevel; level++)
        {
            if (level == MaxLevel)
            {
                table.Add(0);
                break;
            }
            table.Add(Mathf.RoundToInt(exp));
            exp *= 1.13f;
        }
        return table;
    }
}
