using Godot;
using System;

public partial class MapManager : Node
{
	public static MapManager Instance { get; private set; }

	[Export] public NodePath MapContainerPath = "../MapContainer";
	[Export] public NodePath PlayerPath = "../LifeContainer/Player";
	[Export] public NodePath CameraControllerPath = "../CameraController";
	[Export] public PackedScene StartingMap;

	private Node3D _mapContainer;
	private Player _player;
	private CameraController _cameraController;
	private BaseMap _currentMapInstance;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;

		_mapContainer = GetNode<Node3D>(MapContainerPath);
		_player = GetNode<Player>(PlayerPath);
		_cameraController = GetNode<CameraController>(CameraControllerPath);

		PackedScene startingMap = StartingMap ?? GD.Load<PackedScene>("res://map/Map0.tscn");
		ChangeMap(startingMap);
	}

	// spawnAtPortal이 true면 spawnPointName을 새 맵의 Portals 컨테이너에서 PortalId로 찾아 스폰한다.
	public void ChangeMap(PackedScene mapScene, string spawnPointName = "Default", bool spawnAtPortal = false)
	{
		if (mapScene == null)
		{
			GD.PrintErr("[MapManager] mapScene is null.");
			return;
		}

		// Remove the current map instance if it exists
		if (_currentMapInstance != null)
		{
			_currentMapInstance.QueueFree();
			_currentMapInstance = null;
		}

		_currentMapInstance = mapScene.Instantiate<BaseMap>();
		_mapContainer.AddChild(_currentMapInstance);

		Vector3 spawnPos = spawnAtPortal
			? _currentMapInstance.GetPortalPosition(spawnPointName)
			: _currentMapInstance.GetSpawnPosition(spawnPointName);

		_player.GlobalPosition = spawnPos;
		_player.Velocity = Vector3.Zero;

		var (minBound, maxBound) = _currentMapInstance.GetCameraBounds();
		_cameraController.SetBounds(minBound, maxBound, _currentMapInstance.CameraDistance);
		_cameraController.SetTarget(_player);
		_cameraController.SnapToTarget();

		GD.Print($"Changed to map: {_currentMapInstance.MapId}, Spawn Point: {spawnPointName}, Player Position: {spawnPos}");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
