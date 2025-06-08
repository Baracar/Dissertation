using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;
//#if UNITY_EDITOR
//using Unity.Netcode.Editor;
//using UnityEditor;
///// <summary>
///// The custom editor for the <see cref="BaseEnemy"/> component.
///// </summary>
//[CustomEditor(typeof(Map), true)]
//public class BaseMapEditor : NetworkTransformEditor
//{
//    private SerializedProperty m_mapX;
//    private SerializedProperty m_mapY;
//    private SerializedProperty m_tileSize;
//    private SerializedProperty m_tileSize;

//    public override void OnEnable()
//    {
//        m_mapX = serializedObject.FindProperty(nameof(Map.mapX));
//        m_mapY = serializedObject.FindProperty(nameof(Map.mapY));
//        m_tileSize = serializedObject.FindProperty(nameof(Map.tileSize));
//        m_tileSize = serializedObject.FindProperty(nameof(Map.tileSize));
//        base.OnEnable();
//    }

//    private void DisplayProperties()
//    {
//        EditorGUILayout.PropertyField(m_mapX);
//        EditorGUILayout.PropertyField(m_mapY);
//        EditorGUILayout.PropertyField(m_tileSize);
//        EditorGUILayout.PropertyField(m_tileSize);
//    }

//    public override void OnInspectorGUI()
//    {
//        var obj = target as Map;
//        void SetExpanded(bool expanded) { obj.BaseEnemyPropertiesVisible = expanded; }
//        ;
//        DrawFoldOutGroup<PlayerCubeController>(obj.GetType(), DisplayProperties, obj.BaseEnemyPropertiesVisible, SetExpanded);
//        base.OnInspectorGUI();
//    }
//}
//#endif

public class Map : NetworkBehaviour
{
//#if UNITY_EDITOR
//    // These bool properties ensure that any expanded or collapsed property views
//    // within the inspector view will be saved and restored the next time the
//    // asset/prefab is viewed.
//    public bool BaseEnemyPropertiesVisible;
//#endif
    public int mapX = 9, mapY = 9;
    public float tileSize = 3;
    public Tile[,] tileMap;
    public Vector2Int start, finish;
    public Transform tileSet;
    public NetworkObject networkObject;
    public EnemyManager enemyManager;

    public ulong strategistId = 1;

    public static Map instance;

    public GameObject startTile;
    public GameObject finishTile;
    public GameObject pathTile;
    public GameObject subborderTile;
    public GameObject borderTile;

    public void Spawn(ulong strategistId)
    {
        //this.strategistId = strategistId; 
        networkObject = GetComponent<NetworkObject>();
        networkObject.SpawnWithOwnership(strategistId);
    }

    public override void OnNetworkSpawn()
    {
        if(networkObject == null)
        {
            networkObject = GetComponent<NetworkObject>();
        }

        instance = this;
        tileMap = new Tile[mapX, mapY];
        for (int i = 0; i < mapX; i++)
        {
            for (int j = 0; j < mapY; j++)
            {
                tileMap[i, j] = null;
            }
        }

        if(!networkObject.IsOwner)
        {
            return;
        }

        int tileCount = tileSet.childCount;
        for (int i = 0; i < tileCount; i++)
        {
            GameObject clone = tileSet.GetChild(i).gameObject;
            TileClone tileClone = clone.GetComponent<TileClone>();
            Transform tileTransform = Instantiate(tileClone.Tile, clone.transform.position, Quaternion.identity).transform;
            Tile tile = tileTransform.GetComponent<Tile>();
            int x = Mathf.RoundToInt(tileTransform.localPosition.x / tileSize);
            int y = Mathf.RoundToInt(tileTransform.localPosition.z / tileSize);
            tile.Spawn(strategistId);
        }
    }

    public Vector3 Vector2to3(Vector2Int vector2)
    {
        return new Vector3(vector2.x * tileSize, 0, vector2.y * tileSize);
    }

    public int[,] StartRoute()
    {
        bool ret;
        int[,] route = FoundRoute(tileMap, out ret);
        return route;
    }

    public bool TryReroute(Tile tile)
    {
        Tile[,] newTileMap = new Tile[mapX, mapY];
        for (int i = 0; i < mapX; i++)
        {
            for (int j = 0; j < mapY; j++)
            {
                newTileMap[i, j] = tileMap[i, j];
            }
        }
        newTileMap[tile.coord.x, tile.coord.y] = tile;

        bool ret;
        FoundRoute(newTileMap, out ret);
        return ret;
    }

    public bool Reroute(Tile tile)
    {
        Tile[,] newTileMap = new Tile[mapX, mapY];
        for (int i = 0; i < mapX; i++)
        {
            for (int j = 0; j < mapY; j++)
            {
                newTileMap[i, j] = tileMap[i, j];
            }
        }
        newTileMap[tile.coord.x, tile.coord.y] = tile;
        bool ret;
        int[,] tileMapRoute = FoundRoute(newTileMap, out ret);
        if (ret)
        {
            tileMap[tile.coord.x, tile.coord.y] = tile;
            if (enemyManager != null)
            {
                enemyManager.tileMapRoute = tileMapRoute;
            }
        }
        return ret;
    }

    public bool RerouteForce(Tile tile)
    {
        tileMap[tile.coord.x, tile.coord.y] = tile;
        bool ret;
        int[,] tileMapRoute = FoundRoute(tileMap, out ret);
        if (enemyManager != null)
        {
            enemyManager.tileMapRoute = tileMapRoute;
        }
        return ret;
    }

    int[,] FoundRoute(Tile[,] newTileMap, out bool valid)
    {
        valid = false;
        int[,] newTileMapRoute = new int[mapX, mapY];
        for (int i = 0; i < mapX; i++)
        {
            for (int j = 0; j < mapY; j++)
            {
                newTileMapRoute[i, j] = -1;
            }
        }
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        newTileMapRoute[finish.x, finish.y] = 0;
        q.Enqueue(finish);

        while (q.Count > 0)
        {
            Vector2Int tileCoord = q.Dequeue();
            int x = tileCoord.x, y = tileCoord.y;
            if (x == start.x && y == start.y)
            {
                valid = true;
            }
            if (IsTilePassable(newTileMap, newTileMapRoute, x, y, 1, 0))
            {
                q.Enqueue(new Vector2Int(x + 1, y));
                newTileMapRoute[x + 1, y] = newTileMapRoute[x, y] + 1;
            }
            if (IsTilePassable(newTileMap, newTileMapRoute, x, y, 0, 1))
            {
                q.Enqueue(new Vector2Int(x, y + 1));
                newTileMapRoute[x, y + 1] = newTileMapRoute[x, y] + 1;
            }
            if (IsTilePassable(newTileMap, newTileMapRoute, x, y, -1, 0))
            {
                q.Enqueue(new Vector2Int(x - 1, y));
                newTileMapRoute[x - 1, y] = newTileMapRoute[x, y] + 1;
            }
            if (IsTilePassable(newTileMap, newTileMapRoute, x, y, 0, -1))
            {
                q.Enqueue(new Vector2Int(x, y - 1));
                newTileMapRoute[x, y - 1] = newTileMapRoute[x, y] + 1;
            }
        }
        return newTileMapRoute;
    }

    [Rpc(SendTo.Owner)]
    public void GetTileMapRpc()
    {
        Debug.Log("to owner");
        //tileMap = SendTileMapRpc();
    }

    [Rpc(SendTo.NotOwner)]
    public void SendTileMapRpc()
    {
        Debug.Log("from owner");
        //return tileMap;
    }

    bool IsTilePassable(Tile[,] newTileMap, int[,] newTileMapRoute, int x, int y, int dX, int dY)
    {
        if (x + dX < 0)
            return false;
        if (y + dY < 0)
            return false;
        if (x + dX >= mapX)
            return false;
        if (y + dY >= mapY)
            return false;
        if (tileMap[x + dX, y + dY] == null)
            return false;
        if (newTileMapRoute[x + dX, y + dY] > -1)
            return false;
        if (!Tile.IsPassableType(newTileMap[x + dX, y + dY].tileType))
            return false;
        return true;
    }
}