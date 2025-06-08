using System.Collections;
using UnityEngine;

public class EnemySubwave : MonoBehaviour
{
    public GameObject enemy;
    public float delayStartSpawn;
    public float delayBetweenSpawn;
    public int count;

    private EnemyWave enemyWave;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemyWave = GetComponent<EnemyWave>();
        StartCoroutine(Spawn());
    }

    IEnumerator Spawn()
    {
        yield return new WaitForSeconds(delayStartSpawn);

        for (int spawned = 0; spawned < count; spawned++)
        {
            enemyWave.enemyManager.SpawnEnemy(enemy);
            yield return new WaitForSeconds(delayBetweenSpawn);
        }

        enemyWave.endedSubwaves++;
        yield return null;
    }
}
