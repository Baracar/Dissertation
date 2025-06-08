using UnityEngine;

public class FrostTower : AuraTile
{
    public float slowdown = 20f;

    public override void OnEnter(Collider other)
    {
        base.OnEnter(other);
        BaseEnemy enemy = other.GetComponent<BaseEnemy>();
        enemy.ChangeSpeed((100f -  slowdown) / 100f);
    }

    public override void OnExit(Collider other)
    {
        base.OnExit(other);
        BaseEnemy enemy = other.GetComponent<BaseEnemy>();
        enemy.ChangeSpeed(100f / (100f - slowdown));
    }
}
