using UnityEngine;

public class PenetrateAuraTower : AuraTile
{
    public int addPenetration = 1;

    public override void OnEnter(Collider other)
    {
        base.OnEnter(other);
        Shooter shooter = other.GetComponent<Shooter>();
        shooter.penetration += addPenetration;
    }

    public override void OnExit(Collider other)
    {
        base.OnExit(other);
        Shooter shooter = other.GetComponent<Shooter>();
        shooter.penetration -= addPenetration;
    }
}
