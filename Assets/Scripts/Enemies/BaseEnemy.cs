using Unity.Netcode.Components;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using Unity.Netcode.Editor;
using UnityEditor;
/// <summary>
/// The custom editor for the <see cref="BaseEnemy"/> component.
/// </summary>
[CustomEditor(typeof(BaseEnemy), true)]
public class BaseEnemyEditor : NetworkTransformEditor
{
    private SerializedProperty m_speed;
    private SerializedProperty m_maxHp;
    private SerializedProperty m_killCost;

    public override void OnEnable()
    {
        m_speed = serializedObject.FindProperty(nameof(BaseEnemy.speed));
        m_maxHp = serializedObject.FindProperty(nameof(BaseEnemy.maxHp));
        m_killCost = serializedObject.FindProperty(nameof(BaseEnemy.killCost));
        base.OnEnable();
    }

    private void DisplayPlayerCubeControllerProperties()
    {
        EditorGUILayout.PropertyField(m_speed);
        EditorGUILayout.PropertyField(m_maxHp);
        EditorGUILayout.PropertyField(m_killCost);
    }

    public override void OnInspectorGUI()
    {
        var baseEnemy = target as BaseEnemy;
        void SetExpanded(bool expanded) { baseEnemy.BaseEnemyPropertiesVisible = expanded; };
        DrawFoldOutGroup<PlayerCubeController>(baseEnemy.GetType(), DisplayPlayerCubeControllerProperties, baseEnemy.BaseEnemyPropertiesVisible, SetExpanded);
        base.OnInspectorGUI();
    }
}
#endif

public class BaseEnemy : NetworkTransform
{
#if UNITY_EDITOR
    public bool BaseEnemyPropertiesVisible;
#endif
    public EnemyManager enemyManager;
    public Map map;
    public float speed;
    public float maxHp;
    public Vector3 shift;
    public float killCost;

    private new Rigidbody rigidbody;
    private float timer = 0f;
    private float speedMultiplier = 1f;
    private float hp;

    int curDistance;
    Vector2Int targetCoord, lastTargetCoord;
    Vector3 targetCoordReal;
    Unity.Mathematics.Random random = new Unity.Mathematics.Random();
    NetworkObject networkObject;

    void Start()
    {
        networkObject = GetComponent<NetworkObject>();
        if (!networkObject.IsOwner)
        {
            return;
        }
        hp = maxHp;
        rigidbody = GetComponent<Rigidbody>();
        random.InitState((uint)Mathf.RoundToInt(Time.time));
        if (!networkObject.IsOwner)
        {
            return;
        }
        updateTargetCoord();
        lastTargetCoord = getCurCoordTile();
    }

    void Update()
    {
        if (!networkObject.IsOwner)
        {
            return;
        }
        //if (!IsSpawned || !HasAuthority)
        //{
        //    Debug.Log("spawn - " + IsSpawned + " authority - " + HasAuthority);
        //    return;
        //}

        if ((transform.position - targetCoordReal).magnitude < 0.3f)
        {
            updateTargetCoord();
        }
        timer += Time.deltaTime;
        if(timer > 1f)
        {
            checkActualPath();
        }

    }

    private void FixedUpdate()
    {
        if (!networkObject.IsOwner)
        {
            return;
        }
        if(map == null)
        {
            map = FindAnyObjectByType<Map>();
        }
        rigidbody.rotation = Quaternion.LookRotation(targetCoordReal - transform.position);
        Vector3 dir = rigidbody.rotation * Vector3.forward * speed * speedMultiplier;
        
        dir = dir - rigidbody.linearVelocity;
        
        rigidbody.AddForce(dir, ForceMode.VelocityChange);
        
        //rigidbody.linearVelocity = dir;
    }

    void updateTargetCoord()
    {
        getCurDistance();
        if (networkObject.IsOwner && curDistance == 0)
        {
            networkObject.Despawn();
        }
        lastTargetCoord = getCurCoordTile();
        setRandomNextTarget();
        //timeLeft = 0f;

        targetCoordReal = new Vector3(targetCoord.x * map.tileSize, 0, targetCoord.y * map.tileSize) + shift;
    }

    void checkActualPath()
    {
        if (enemyManager.tileMapRoute[lastTargetCoord.x, lastTargetCoord.y] < enemyManager.tileMapRoute[targetCoord.x, targetCoord.y] ||
            enemyManager.tileMapRoute[targetCoord.x, targetCoord.y] == -1)
        {
            updateTargetCoord();
        }
    }

    void getCurDistance()
    {
        int x = Mathf.RoundToInt(transform.position.x / map.tileSize);
        int y = Mathf.RoundToInt(transform.position.z / map.tileSize);
        if (enemyManager.tileMapRoute == null)
        {
            Debug.Log("No map");
            return;
        }
        curDistance = enemyManager.tileMapRoute[x, y];
    }

    Vector2Int getCurCoordTile()
    {
        int x = Mathf.RoundToInt(transform.position.x / map.tileSize);
        int y = Mathf.RoundToInt(transform.position.z / map.tileSize);
        return new Vector2Int(x, y);
    }

    void setRandomNextTarget()
    {
        Vector2Int curCoordTile = getCurCoordTile();

        List<Vector2Int> curNearTiles = new List<Vector2Int>();
        foreach (Vector2Int tile in nearTiles)
        {
            curNearTiles.Add(new Vector2Int(curCoordTile.x + tile.x, curCoordTile.y + tile.y));
        }

        while (curNearTiles.Count > 0)
        {
            random.InitState(random.NextUInt() + 1);
            int i = random.NextInt(curNearTiles.Count);
            if (enemyManager.tileMapRoute[curNearTiles[i].x, curNearTiles[i].y] == curDistance - 1)
            {
                targetCoord = curNearTiles[i];
                break;
            }
            curNearTiles.RemoveAt(i);
        }
    }

    public void takeDamage(float damage, Shooter shooter, HitParam hitParam)
    {
        hp -= damage;
        if (hp <= 0)
        {
            shooter.Kill(killCost, hitParam);
            networkObject.Despawn();
        }
    }

    public void ChangeSpeed(float multiplier)
    {
        if(multiplier > 0)
        {
            speedMultiplier *= multiplier;
        }
    }

    protected override void OnOwnershipChanged(ulong previous, ulong current)
    {
        //Debug.Log(this + " change owner from " + previous + " to " + current);
        //if (map.strategistId != 0 && map.strategistId != current)
        if (current != 2)
        {
            networkObject.ChangeOwnership(2);
        }
    }

    List<Vector2Int> nearTiles = new List<Vector2Int>(){
        new Vector2Int(1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(-1, 0),
        new Vector2Int(0, -1)
    };
}