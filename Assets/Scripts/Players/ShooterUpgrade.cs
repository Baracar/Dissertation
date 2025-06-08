using UnityEngine;

public struct HitParam
{
    public bool shooterWithBuff;
    public bool enemyWithDebuff;
    public Vector3 shooterLocation;
    public Vector3 hitLocation;
    public float rangeToFinish;
    public int enemyHitted;
}

public class ShooterUpgrade
{
    public float value;

    public virtual float CheckConditionOnKill(HitParam hitParam)
    {
        return 0;
    }
    public virtual float CheckConditionOnHit(HitParam hitParam)
    {
        return 0;
    }

    public ShooterUpgrade(float value)
    {
        this.value = value;
    }
}

public enum UpgradeType
{
    Sniper,
    Mass
}

public class SniperUpgrade : ShooterUpgrade
{
    public float range = 12f;

    public override float CheckConditionOnKill(HitParam hitParam)
    {
        if ((hitParam.shooterLocation - hitParam.hitLocation).magnitude >= range)
            return value;
        return 0;
    }

    public SniperUpgrade(float value) : base(value)
    {
    }
}

public class MassUpgrade : ShooterUpgrade
{
    public int minHitted = 2;
    public override float CheckConditionOnHit(HitParam hitParam)
    {
        if (hitParam.enemyHitted > 3)
            return value * (hitParam.enemyHitted - minHitted);
        return 0;
    }

    public MassUpgrade(float value) : base(value)
    {
    }
}

