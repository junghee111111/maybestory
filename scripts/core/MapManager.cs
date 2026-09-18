using Godot;
using System;

public partial class MapManager : Node
{
	[Export] public NodePath MapContainerPath = "../MapContainer";
	[Export] public NodePath PlayerPath = "../LifeContainer/Player";
	[Export] public NodePath CameraControllerPath = "../CameraController";

	private Node3D _mapContainer;
	private Player _player;
	private CameraController _cameraController;
	private BaseMap _currentMapInstance;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_mapContainer = GetNode<Node3D>(MapContainerPath);
		_player = GetNode<Player>(PlayerPath);
		_cameraController = GetNode<CameraController>(CameraControllerPath);

		ChangeMap("res://map/Map0.tscn");
	}

	public void ChangeMap(string mapScenePath, string spawnPointName = "Default")
	{
		// Remove the current map instance if it exists
		if (_currentMapInstance != null)
		{
			_currentMapInstance.QueueFree();
			_currentMapInstance = null;
		}

		var mapScene = GD.Load<PackedScene>(mapScenePath);
		if (mapScene == null)
		{
			GD.PrintErr($"Failed to load map scene: {mapScenePath}");
			return;
		}

		_currentMapInstance = mapScene.Instantiate<BaseMap>();
		_mapContainer.AddChild(_currentMapInstance);
		Vector3 spawnPos = _currentMapInstance.GetSpawnPosition(spawnPointName);
		_player.GlobalPosition = spawnPos;
		_player.Velocity = Vector3.Zero;

		var (minBound, maxBound) = _currentMapInstance.GetCameraBounds();
		_cameraController.SetBounds(minBound, maxBound, _currentMapInstance.CameraDistance);
		_cameraController.SetTarget(_player);
		_cameraController.SnapToTarget();

		GD.Print($"Changed to map: {mapScenePath}, Spawn Point: {spawnPointName}, Player Position: {spawnPos}");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
