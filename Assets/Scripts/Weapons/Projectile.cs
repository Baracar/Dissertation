using Unity.Netcode.Components;
using UnityEngine;
using Unity.Netcode;
using System.Collections;



#if UNITY_EDITOR
using Unity.Netcode.Editor;
using UnityEditor;
/// <summary>
/// The custom editor for the <see cref="Projectile"/> component.
/// </summary>
[CustomEditor(typeof(Projectile), true)]
public class ProjectileEditor : NetworkTransformEditor
{
    private SerializedProperty m_Speed;
    private SerializedProperty m_Damage;
    private SerializedProperty m_camera;
    private SerializedProperty m_towers;

    public override void OnEnable()
    {
        m_Speed = serializedObject.FindProperty(nameof(Projectile.speed));
        m_Damage = serializedObject.FindProperty(nameof(Projectile.damage));
        base.OnEnable();
    }

    private void DisplayProjectileProperties()
    {
        EditorGUILayout.PropertyField(m_Speed);
        EditorGUILayout.PropertyField(m_Damage);
    }

    public override void OnInspectorGUI()
    {
        var Projectile = target as Projectile;
        void SetExpanded(bool expanded) { Projectile.ProjectilePropertiesVisible = expanded; };
        DrawFoldOutGroup<Projectile>(Projectile.GetType(), DisplayProjectileProperties, Projectile.ProjectilePropertiesVisible, SetExpanded);
        base.OnInspectorGUI();
    }
}
#endif


public class Projectile : NetworkTransform
{
#if UNITY_EDITOR
    public bool ProjectilePropertiesVisible;
#endif
    public float speed;
    public float damage;
    public int penetration = 0;
    public Quaternion direction;

    public Shooter shooter;
    public HitParam hitParam;

    NetworkObject networkObject;
    new Rigidbody rigidbody;

    public void Spawn(Quaternion direction)
    {
        rigidbody = GetComponent<Rigidbody>();
        networkObject = GetComponent<NetworkObject>();
        networkObject.Spawn();

        rigidbody.rotation = direction;
        this.direction = direction;
    }

    void FixedUpdate()
    {
        if (NetworkObject.IsOwner)
        {
            rigidbody.linearVelocity = direction * Vector3.forward * speed;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        GameObject collisionObject = other.gameObject;
        while (true)
        {
            Shooter shooterIns = collisionObject.GetComponent<Shooter>();
            if (shooterIns != null)
            {
                return;
            }
            BaseEnemy enemy = collisionObject.GetComponent<BaseEnemy>();
            if (enemy != null)
            {
                hitParam.enemyHitted++;
                hitParam.hitLocation = other.transform.position;
                shooter.Hit(enemy.killCost, hitParam);
                enemy.takeDamage(damage, shooter, hitParam);
                Debug.Log(penetration);
                if (penetration > 0)
                {
                    penetration--;
                }
                else
                {
                    networkObject.Despawn();
                }
                return;
            }
            Tile tile = collisionObject.GetComponent<Tile>();
            if (tile != null)
            {
                networkObject.Despawn();
                return;
            }
            collisionObject = collisionObject.transform?.parent?.gameObject;
            if(collisionObject == null)
            {
                return;
            }
        }
    }
}
