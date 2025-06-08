using Unity.Netcode.Components;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

using System;



#if UNITY_EDITOR
using Unity.Netcode.Editor;
using UnityEditor;
/// <summary>
/// The custom editor for the <see cref="Strategist"/> component.
/// </summary>
[CustomEditor(typeof(Strategist), true)]
public class StrategistEditor : NetworkTransformEditor
{
    private SerializedProperty speed;
    private SerializedProperty mouseSensivity;
    private SerializedProperty scrollSensivity;
    private SerializedProperty camera;
    private SerializedProperty towers;
    private SerializedProperty moneyField;

    public override void OnEnable()
    {
        speed = serializedObject.FindProperty(nameof(Strategist.speed));
        mouseSensivity = serializedObject.FindProperty(nameof(Strategist.mouseSensivity));
        scrollSensivity = serializedObject.FindProperty(nameof(Strategist.scrollSensivity));
        camera = serializedObject.FindProperty(nameof(Strategist.camera));
        towers = serializedObject.FindProperty(nameof(Strategist.towers));
        moneyField = serializedObject.FindProperty(nameof(Strategist.moneyField));
        base.OnEnable();
    }

    private void DisplayStrategistProperties()
    {
        EditorGUILayout.PropertyField(speed);
        EditorGUILayout.PropertyField(mouseSensivity);
        EditorGUILayout.PropertyField(scrollSensivity);
        EditorGUILayout.PropertyField(camera);
        EditorGUILayout.PropertyField(towers);
        EditorGUILayout.PropertyField(moneyField);
    }

    public override void OnInspectorGUI()
    {
        var Strategist = target as Strategist;
        void SetExpanded(bool expanded) { Strategist.StrategistPropertiesVisible = expanded; };
        DrawFoldOutGroup<Strategist>(Strategist.GetType(), DisplayStrategistProperties, Strategist.StrategistPropertiesVisible, SetExpanded);
        base.OnInspectorGUI();
    }
}
#endif


public class Strategist : NetworkTransform
{
#if UNITY_EDITOR
    // These bool properties ensure that any expanded or collapsed property views
    // within the inspector view will be saved and restored the next time the
    // asset/prefab is viewed.
    public bool StrategistPropertiesVisible;
#endif
    public float speed;
    public float mouseSensivity;
    public float scrollSensivity;
    public new GameObject camera;
    public GameObject[] towers;

    public float money = 0.4f;
    public TMP_InputField moneyField;

    public static Strategist instance;
    public ulong networkId = 999999;

    private Vector3 motion;
    private float pitch = 60;
    private float yaw = 0;
    private float maxLookAngle = 90;

    //private SyncVar<float> moneySync = new SyncVar<float>();

    private void Start()
    {
        instance = this;

        Debug.Log("Camera strategist - " + NetworkObject.IsOwner);
        //moneySync.value.Add(0f);

        if (NetworkObject.IsOwner)
        {
            camera.SetActive(true);
        }
    }

    private void Update()
    {
        if (!NetworkObject.IsOwner)
        {
            SendMoney();
            return;
        } 

        if (!IsSpawned || !HasAuthority)
        {
            return;
        }

        UpdateMoney();

        if (Physics.queriesHitTriggers)
            Physics.queriesHitTriggers = false;

        motion = Vector3.zero;
        motion.x = Input.GetAxis("Horizontal");
        motion.z = Input.GetAxis("Vertical");
        motion = transform.localRotation * motion;

        if (motion.magnitude > 0)
        {
            transform.position += speed * Time.deltaTime * motion;
        }
        moveCamera();

        if (Input.GetKey(KeyCode.Mouse1))
        {
            selectedTower = null;
        }
    }

    void moveCamera()
    {
        if (Input.GetKey(KeyCode.Mouse2))
        {
            yaw = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * mouseSensivity;
            pitch -= mouseSensivity * Input.GetAxis("Mouse Y");
            pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

            transform.localEulerAngles = new Vector3(0, yaw, 0);
            camera.transform.localEulerAngles = new Vector3(pitch, 0, 0);
        }
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        transform.position += new Vector3(0, scroll * scrollSensivity, 0);
    }

    public GameObject selectedTower = null;
    public void ClickButton(int number)
    {
        if (number >= towers.Length)
        {
            Debug.Log("Button - not setted");
            selectedTower = null;
            return;
        }
        Debug.Log("Button - " + towers[number]);
        Tile tile = towers[number].GetComponent<Tile>();
        if(tile.cost > money)
        {
            Debug.Log("Not enought money. Need - " + tile.cost + ", have - " + money);
            selectedTower = null;
            return;
        }
        selectedTower = towers[number];
    }

    public void ReplaceTower(Tile oldTile)
    {
        if (selectedTower == null)
        {
            Debug.LogWarning("Tower doesn't selected!");
            return;
        }
        if(oldTile.tileType == TileType.start || oldTile.tileType == TileType.finish || oldTile.tileType == TileType.border)
        {
            Debug.LogWarning("Cant replace this tile!");
            return;
        }

        Tile newTile = Instantiate(selectedTower).GetComponent<Tile>();
        oldTile.SetUp(newTile);
        if (!Map.instance.Reroute(newTile))
        {
            Debug.LogWarning("Cant find new route!");
            Destroy(newTile.gameObject);
            selectedTower = null;
            return;
        }
        //newTile.transform.position = oldTile.transform.position;

        Debug.Log("Despawn - " + oldTile.gameObject + " and spawn - " + newTile.gameObject);
        //Destroy(oldTile.gameObject);
        newTile.Spawn(networkId);
        oldTile.Despawn();

        AddMoney(-newTile.cost);
        //newTile.networkObject.SpawnWithOwnership(networkId, true);

        selectedTower = null;
    }

    public void HoverOnTower(Tile tile)
    {

    }

    private void UpdateMoney()
    {
        moneyField.text = money.ToString();
    }

    public void AddMoney(float amount)
    {
        money += amount;
        UpdateMoney();
    }



    private void SendMoney()
    {
        while (money >= 100f)
        {
            SendHundredMoneyRpc();
            money -= 100f;
        }
        while (money >= 10f)
        {
            SendTenMoneyRpc();
            money -= 10f;
        }
        while (money >= 1f)
        {
            SendOneMoneyRpc();
            money -= 1f;
        }
        return;
    }

    [Rpc(SendTo.Owner)]
    public void SendOneMoneyRpc()
    {
        float amount = 1f;
        money += amount;
        Debug.Log("Get " + amount + ", total - " + money);
    }

    [Rpc(SendTo.Owner)]
    public void SendTenMoneyRpc()
    {
        float amount = 10f;
        money += amount;
        Debug.Log("Get " + amount + ", total - " + money);
    }

    [Rpc(SendTo.Owner)]
    public void SendHundredMoneyRpc()
    {
        float amount = 100f;
        money += amount;
        Debug.Log("Get " + amount + ", total - " + money);
    }
}











//[Serializable]
//[GenerateSerializationForGenericParameter(0)]
//public class SyncVar<T> : NetworkVariableBase
//{
//    public List<T> value = new List<T>(1);

//    public override void WriteField(FastBufferWriter writer)
//    {
//        // Serialize the data we need to synchronize
//        writer.WriteValueSafe(value.Count);
//        for (var i = 0; i < value.Count; ++i)
//        {
//            var dataEntry = value[i];
//            // NetworkVariableSerialization<T> is used for serializing generic types
//            NetworkVariableSerialization<T>.Write(writer, ref dataEntry);
//        }
//    }

//    public override void ReadField(FastBufferReader reader)
//    {
//        // De-Serialize the data being synchronized
//        var itemsToUpdate = (int)0;
//        reader.ReadValueSafe(out itemsToUpdate);
//        value.Clear();
//        for (int i = 0; i < itemsToUpdate; i++)
//        {
//            T newEntry = default;
//            // NetworkVariableSerialization<T> is used for serializing generic types
//            NetworkVariableSerialization<T>.Read(reader, ref newEntry);
//            value.Add(newEntry);
//        }
//    }

//    //public T value = default;

//    //public override void WriteField(FastBufferWriter writer)
//    //{
//    //    // Serialize the data we need to synchronize
//    //    writer.WriteValueSafe(1);
//    //    var dataEntry = value;
//    //    NetworkVariableSerialization<T>.Write(writer, ref dataEntry);
//    //}

//    //public override void ReadField(FastBufferReader reader)
//    //{
//    //    T newEntry = default;
//    //    // NetworkVariableSerialization<T> is used for serializing generic types
//    //    NetworkVariableSerialization<T>.Read(reader, ref newEntry);
//    //    value = newEntry;
//    //}

//    public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
//    {
//        // Do nothing for this example
//    }

//    public override void WriteDelta(FastBufferWriter writer)
//    {
//        // Do nothing for this example
//    }
//}