using Godot;

public partial class Projectile : Area3D
{
    private Vector3 _direction;
    private float _speed;
    private int _damage;
    private bool _isCritical;

    public void Initialize(Vector3 direction, float speed, int damage, bool isCritical = false)
    {
        _direction = direction.Normalized();
        _speed = speed;
        _damage = damage;
        _isCritical = isCritical;
    }

    public override void _PhysicsProcess(double delta)
    {
        GlobalPosition += _direction * _speed * (float)delta;
    }

    private void _on_body_entered(Node3D body)
    {
        if (body.IsInGroup("Monsters") && body.HasMethod("TakeDamage"))
        {
            DamageIndicator.Spawn(GetTree().CurrentScene, body.GlobalPosition + new Vector3(0, 2.2f, 0), _damage, _isCritical);
            body.Call("TakeDamage", _damage);
            QueueFree(); // 타격 후 소멸
        }
    }
}