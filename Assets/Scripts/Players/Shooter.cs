using Unity.Netcode.Components;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;


#if UNITY_EDITOR
using Unity.Netcode.Editor;
using UnityEditor;
/// <summary>
/// The custom editor for the <see cref="Shooter"/> component.
/// </summary>
[CustomEditor(typeof(Shooter), true)]
public class ShooterEditor : NetworkTransformEditor
{
    private SerializedProperty m_Speed;
    private SerializedProperty m_MouseSensivity;
    private SerializedProperty m_weapon;
    private SerializedProperty m_camera;
    private SerializedProperty m_fireRate;
    private SerializedProperty m_UI;
    private SerializedProperty m_upgradeCard;

    public override void OnEnable()
    {
        m_Speed = serializedObject.FindProperty(nameof(Shooter.speed));
        m_MouseSensivity = serializedObject.FindProperty(nameof(Shooter.mouseSensitivity));
        m_weapon = serializedObject.FindProperty(nameof(Shooter.weapon));
        m_camera = serializedObject.FindProperty(nameof(Shooter.camera));
        m_fireRate = serializedObject.FindProperty(nameof(Shooter.fireRate));
        m_UI = serializedObject.FindProperty(nameof(Shooter.UI));
        m_upgradeCard = serializedObject.FindProperty(nameof(Shooter.upgradeCard));
        base.OnEnable();
    }

    private void DisplayShooterProperties()
    {
        EditorGUILayout.PropertyField(m_Speed);
        EditorGUILayout.PropertyField(m_MouseSensivity);
        EditorGUILayout.PropertyField(m_weapon);
        EditorGUILayout.PropertyField(m_camera);
        EditorGUILayout.PropertyField(m_fireRate);
        EditorGUILayout.PropertyField(m_UI);
        EditorGUILayout.PropertyField(m_upgradeCard);
    }

    public override void OnInspectorGUI()
    {
        var Shooter = target as Shooter;
        void SetExpanded(bool expanded) { Shooter.ShooterPropertiesVisible = expanded; };
        DrawFoldOutGroup<Shooter>(Shooter.GetType(), DisplayShooterProperties, Shooter.ShooterPropertiesVisible, SetExpanded);
        base.OnInspectorGUI();
    }
}
#endif


public class Shooter : NetworkTransform
{
#if UNITY_EDITOR
    // These bool properties ensure that any expanded or collapsed property views
    // within the inspector view will be saved and restored the next time the
    // asset/prefab is viewed.
    public bool ShooterPropertiesVisible;
#endif
    public float speed;
    public float mouseSensitivity;
    public float fireRate;
    public GameObject weapon;
    public new GameObject camera;
    public ulong networkId;

    public float speedMultiplier = 1f;
    public int penetration = 0;

    public List<ShooterUpgrade> upgrades = new List<ShooterUpgrade>(); 

    private new Rigidbody rigidbody;
    private Vector3 motion;
    private float pitch = 0;
    private float yaw = 0;
    private float maxLookAngle = 90;

    private float cooldown = 0;

    private void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        Debug.Log("Camera shooter - " + NetworkObject.IsOwner);
        if (NetworkObject.IsOwner)
        {
            camera.SetActive(true);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            upgrades.Add(new SniperUpgrade(0.1f));
            upgrades.Add(new MassUpgrade(0.01f));
            
            UpgradeUI();
        }

    }

    private void Update()
    {
        if (!IsSpawned || !HasAuthority)
        {
            return;
        }

        cooldown -= Time.deltaTime;
        if (Input.GetMouseButton(0) && cooldown <= 0f)
        {
            Attack();
            cooldown = 1f / fireRate;
        }
    }

    private void FixedUpdate()
    {
        Rotate();
        Move();
    }

    void Rotate()
    {
        rigidbody.angularVelocity = rigidbody.angularVelocity * 0.2f;

        yaw = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= mouseSensitivity * Input.GetAxis("Mouse Y");
        pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

        rigidbody.rotation = Quaternion.Euler(0, yaw, 0);
        camera.transform.localEulerAngles = new Vector3(pitch, 0, 0);
    }

    void Move()
    {
        motion = Vector3.zero;
        motion.x = Input.GetAxis("Horizontal");
        motion.z = Input.GetAxis("Vertical");
        motion = rigidbody.rotation * motion * speed * speedMultiplier;
        motion = motion - rigidbody.linearVelocity;

        rigidbody.AddForce(motion, ForceMode.VelocityChange);
    }

    void Attack()
    {
        var bullet = Instantiate(weapon, camera.transform.position, Quaternion.identity);
        Projectile projectile = bullet.GetComponent<Projectile>();
        projectile.shooter = this;
        projectile.penetration = penetration;

        HitParam hitParam = new HitParam
        {
            enemyHitted = 0,
            shooterLocation = transform.position
        };

        projectile.hitParam = hitParam;

        projectile.Spawn(camera.transform.rotation);
    }

    public void Hit(float cost, HitParam hitParam)
    {
        float multiplier = 0f;
        foreach (var upgrade in upgrades)
        {
            multiplier += upgrade.CheckConditionOnHit(hitParam);
        }
        Strategist.instance.AddMoney(cost * multiplier);
    }

    public void Kill(float cost, HitParam hitParam)
    {
        float multiplier = 1f;
        foreach (var upgrade in upgrades)
        {
            multiplier += upgrade.CheckConditionOnKill(hitParam);
        }
        Strategist.instance.AddMoney(cost * multiplier);
    }



    public RectTransform UI;
    public UpgradeCard upgradeCard;

    public void UpgradeUI()
    {
        UpgradeCard upgradeCardIns = Instantiate(upgradeCard, UI);
        upgradeCardIns.upgradeType = UpgradeType.Sniper;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}

