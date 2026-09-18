using Godot;

public partial class MobSpawner : Node3D
{
    [Export] public MobStatsData TargetMonsterData;
    [Export] public int MaxMonsters = 3;         // 해당 스포너가 유지할 최대 마릿수
    [Export] public float RespawnInterval = 5.0f; // 리스폰 주기 (초)
    [Export] public float SpawnRadiusX = 3.0f;    // 스폰 지점 좌우 분산 범위

    private int _currentAliveCount = 0;
    private double _timer = 0.0;

    public override async void _Ready()
    {
        // delay 0.5s
        await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
        for (int i = 0; i < MaxMonsters; i++)
        {
            SpawnMonster();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_currentAliveCount >= MaxMonsters) return;

        _timer += delta;
        if (_timer >= RespawnInterval)
        {
            _timer = 0.0;
            SpawnMonster();
        }
    }

    private void SpawnMonster()
    {
        if (TargetMonsterData?.MobScene == null) return;

        var mobInstance = TargetMonsterData.MobScene.Instantiate<Mob>();
        mobInstance.Initialize(TargetMonsterData);

        // 사망 이벤트 구독
        _currentAliveCount++;
        mobInstance.OnDeathToSpawner += () =>
        {
            _currentAliveCount--;
            _timer = 0.0; // 몬스터가 죽으면 리스폰 타이머 카운트 시작
        };

        // GlobalPosition requires the node to already be inside the scene tree.
        GetParent().GetParent().GetNode<Node3D>("Life").AddChild(mobInstance);

        // 스포너 위치 기준으로 X축만 약간 분산시켜 스폰
        float offsetX = (float)GD.RandRange(-SpawnRadiusX, SpawnRadiusX);
        Vector3 spawnPos = GlobalPosition + new Vector3(offsetX, 0, 0);
        spawnPos.Z = 0.0f; // Z축 고정

        mobInstance.GlobalPosition = spawnPos;
    }
}