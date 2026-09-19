using Godot;
using System;

public partial class BaseMap : Node3D
{
	[Export] public string MapId = "Map01";
	[Export] public string BGMPath = "res://assets/audio/bgm/Map01.ogg";
	[Export] public float CameraDistance = 12.0f;
	[Export] public float Gravity = 25.0f;

	private Marker3D _minBound;
	private Marker3D _maxBound;



	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_minBound = GetNodeOrNull<Marker3D>("CamBounds/MinBound");
		_maxBound = GetNodeOrNull<Marker3D>("CamBounds/MaxBound");
		if (_minBound == null || _maxBound == null)
		{
			GD.PrintErr($"Camera bounds not properly set in map '{MapId}'. Ensure 'MinBound' and 'MaxBound' markers exist under 'CamBounds'.");
		}
	}

	public Vector3 GetSpawnPosition(string spawnPointName = "Default")
	{
		Marker3D spawnPoint = GetNodeOrNull<Marker3D>($"Spawns/{spawnPointName}");
		if (spawnPoint != null)
		{
			return spawnPoint.GlobalPosition;
		}
		GD.PrintErr($"Spawn point '{spawnPointName}' not found in map '{MapId}'. Returning Vector3.Zero.");
		return Vector3.Zero;
	}

	public (Vector3 min, Vector3 max) GetCameraBounds()
	{
		if (_minBound != null && _maxBound != null)
		{
			return (_minBound.GlobalPosition, _maxBound.GlobalPosition);
		}
		GD.PrintErr($"Camera bounds not properly set in map '{MapId}'. Returning (Vector3.Zero, Vector3.Zero).");
		return (Vector3.Zero, Vector3.Zero);
	}

	// 다음 맵으로 넘어올 때 같은 PortalId를 가진 포탈의 위치에 스폰시키기 위한 조회.
	public Vector3 GetPortalPosition(string portalId)
	{
		Node portalsContainer = GetNodeOrNull("Portals");
		if (portalsContainer != null)
		{
			foreach (Node child in portalsContainer.GetChildren())
			{
				if (child is Portal portal && portal.PortalId == portalId)
				{
					return portal.GlobalPosition;
				}
			}
		}
		GD.PrintErr($"Portal '{portalId}' not found in map '{MapId}'. Returning Vector3.Zero.");
		return Vector3.Zero;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
