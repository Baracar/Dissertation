using Unity.Netcode.Components;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class EnemyManager : NetworkBehaviour
{
    public Map map;
    public int[,] tileMapRoute;
    public float spawnRate = 1;
    public List<BaseEnemy> enemyList = new List<BaseEnemy>();
    public ulong shooterId = 2;
    public bool spawnEnable = false;

    public EnemyWave[] waves;
    private int curWave = 0;

    float timer = 0f;
    Unity.Mathematics.Random random;
    void Start()
    {
        if (!NetworkObject.IsOwner)
        {
            return;
        }
        if(map == null)
        {
            map = Map.instance;
        }
        map.enemyManager = this;
        random = new Unity.Mathematics.Random();
        random.InitState((uint)Mathf.RoundToInt(Time.time));

        timer = 1f / spawnRate;

        NextWave();
    }

    void Update()
    {
        //if (!spawnEnable)
        //    return;
        //timer += Time.deltaTime;
        //if(timer > 1f / spawnRate){
        //    timer = 0f;
        //    SpawnEnemy();
        //}
    }

    public void NextWave()
    {
        if (curWave == waves.Length)
            return;
        EnemyWave enemyWave = Instantiate(waves[curWave]);
        enemyWave.enemyManager = this;
        curWave++;
    }

    public void SpawnEnemy(GameObject enemyObj)
    {
        Vector3 shift = new Vector3(random.NextFloat(-1f, 1f), 0, random.NextFloat(-1f, 1f));
        BaseEnemy enemy = Instantiate(enemyObj, map.Vector2to3(map.start) + shift, new Quaternion()).GetComponent<BaseEnemy>();
        enemy.map = map;
        enemy.enemyManager = this;
        enemy.shift = shift;
        var enemyNetworkObject = enemy.GetComponent<NetworkObject>();
        enemyNetworkObject.SpawnWithOwnership(shooterId, true);
    }

    public void Spawn(Map map, ulong shooterId)
    {
        this.map = map;
        this.shooterId = shooterId;
        map.enemyManager = this;
        tileMapRoute = map.StartRoute();

        //GetComponent<NetworkObject>().SpawnWithOwnership(NetworkObject.OwnerClientId);
        GetComponent<NetworkObject>().SpawnWithOwnership(shooterId);
    }

    void SpawnEnemy(){
        if(enemyList.Count > 0){
            BaseEnemy enemy = Instantiate(enemyList[0], map.Vector2to3(map.start), new Quaternion());
            enemy.map = map;
            enemy.enemyManager = this;
            var enemyNetworkObject = enemy.GetComponent<NetworkObject>();
            enemyNetworkObject.SpawnWithOwnership(shooterId, true);
        }
    }

}
