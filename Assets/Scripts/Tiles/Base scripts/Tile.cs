using System;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class Tile : NetworkBehaviour
{
    [HideInInspector]
    public Vector2Int coord;
    public TileType tileType;
    public float cost = 100f;
    [HideInInspector]
    public Map map;
    [HideInInspector]
    public NetworkObject networkObject;

    public void setLocation(int x, int y)
    {
        coord = new Vector2Int(x, y);
    }

    public void setLocation(Vector2Int newCoord)
    {
        coord = new Vector2Int(newCoord.x, newCoord.y);
    }

    protected virtual void Start()
    {

    }
    void Update()
    {

    }

    public void Spawn(ulong strategistId)
    {
        networkObject = GetComponent<NetworkObject>();
        networkObject.SpawnWithOwnership(1);
    }

    public override void OnNetworkSpawn()
    {
        if (networkObject == null)
        {
            networkObject = GetComponent<NetworkObject>();
        } 
        base.OnNetworkSpawn();
        if (map == null)
        {
            map = Map.instance;
        }

        coord = new Vector2Int(Mathf.RoundToInt(transform.localPosition.x / map.tileSize), Mathf.RoundToInt(transform.localPosition.z / map.tileSize));

        if (tileType == TileType.start)
        {
            map.start = coord;
        }
        else if (tileType == TileType.finish)
        {
            map.finish = coord;
        }

        map.RerouteForce(this);
    }

    protected override void OnOwnershipChanged(ulong previous, ulong current)
    {
        if (current != 1)
        {
            networkObject.ChangeOwnership(1);
        }
    }

    void OnMouseDown()
    {
        if (networkObject.IsOwner)
        {
            Strategist.instance.ReplaceTower(this);
        }
    }

    private void OnMouseEnter()
    {
        Strategist.instance?.HoverOnTower(this);
    }

    static public bool IsPassableType(TileType type)
    {
        if (type == TileType.path || type == TileType.start || type == TileType.finish)
        {
            return true;
        }
        return false;
    }

    public void SetUp(Tile newTile)
    {
        newTile.setLocation(coord);
        newTile.map = map;
        newTile.transform.position = transform.position;
    }

    public virtual void Despawn()
    {
        networkObject.Despawn();
    }
}

public enum TileType
{
    path,
    start,
    finish,
    subborder,
    border
}