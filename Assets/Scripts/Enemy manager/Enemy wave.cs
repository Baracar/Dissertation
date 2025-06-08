using System.Collections;
using UnityEngine;

public class EnemyWave : MonoBehaviour
{
    public EnemyManager enemyManager;
    private EnemySubwave[] enemySubwaves;
    public int endedSubwaves = 0;

    private void Start()
    {
        enemySubwaves = GetComponents<EnemySubwave>();
        StartCoroutine(WaitEnd());
    }

    IEnumerator WaitEnd()
    {
        while (enemySubwaves.Length > endedSubwaves)
        {
            yield return 0;
        }
        yield return new WaitForSeconds(10f);
        enemyManager.NextWave();
        Destroy(gameObject);
        yield return null;
    }
}
