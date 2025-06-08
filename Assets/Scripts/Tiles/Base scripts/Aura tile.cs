using UnityEngine;
using System.Collections.Generic;

public class AuraTile : Tile
{
    private List<Collider> entities;
    protected override void Start()
    {
        base.Start();
        entities = new List<Collider>();
    }

    public virtual void OnEnter(Collider other)
    {
        entities.Add(other);
    }

    public virtual void OnExit(Collider other)
    {
        entities.Remove(other);
    }

    public override void Despawn()
    {
        while (entities.Count > 0)
        {
            OnExit(entities[0]);
        }
        base.Despawn();
    }
}
