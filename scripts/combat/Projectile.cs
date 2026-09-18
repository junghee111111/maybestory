using Godot;

public partial class Projectile : Area3D
{
    private Vector3 _direction;
    private float _speed;
    private int _damage;

    public void Initialize(Vector3 direction, float speed, int damage)
    {
        _direction = direction.Normalized();
        _speed = speed;
        _damage = damage;
    }

    public override void _PhysicsProcess(double delta)
    {
        GlobalPosition += _direction * _speed * (float)delta;
    }

    private void _on_body_entered(Node3D body)
    {
        if (body.IsInGroup("Monsters") && body.HasMethod("TakeDamage"))
        {
            body.Call("TakeDamage", _damage);
            QueueFree(); // 타격 후 소멸
        }
    }
}