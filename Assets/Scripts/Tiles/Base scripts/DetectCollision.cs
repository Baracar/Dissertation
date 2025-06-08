using Unity.Mathematics;
using UnityEngine;

public class DetectCollision : MonoBehaviour
{
    public LayerMask layer;
    private AuraTile tile;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tile = GetComponentInParent<AuraTile>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (tile == null || ((1 << other.gameObject.layer) & layer) == 0)
        {
            return;
        }
        tile.OnEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (tile == null || ((1 << other.gameObject.layer) & layer) == 0)
        {
            return;
        }
        tile.OnExit(other);
    }
}
