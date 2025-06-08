using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
    private string _profileName;
    private string _sessionName;
    private int _maxPlayers = 2;
    private ConnectionState _state = ConnectionState.Disconnected;
    private ISession _session;
    private NetworkManager networkManager;
    public GameObject strategist;
    public GameObject level;
    public GameObject shooter;
    public Camera tempCamera;
    public EnemyManager enemyManager;
    private Map map = null;

    private enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
    }
    private bool trySpawnShooter = false;

    private async void Awake()
    {
        networkManager = GetComponent<NetworkManager>();
        networkManager.OnClientConnectedCallback += OnClientConnectedCallback;
        networkManager.OnSessionOwnerPromoted += OnSessionOwnerPromoted;
        await UnityServices.InitializeAsync();
    }

    private void OnSessionOwnerPromoted(ulong sessionOwnerPromoted)
    {
        if (networkManager.LocalClient.IsSessionOwner)
        {
            Debug.Log($"Client-{networkManager.LocalClientId} is the session owner!");
        }
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        if (networkManager.LocalClientId == clientId)
        {
            Debug.Log($"Client-{clientId} is connected and can spawn {nameof(NetworkObject)}s.");


            GameObject player;
            //if (_profileName == "strategist" || _profileName == "1")
            if (clientId == 1)
            {
                Debug.Log("Spawn level");
                GameObject levelInstance = Instantiate(level, Vector3.zero, new Quaternion());
                levelInstance.name = "Level";
                map = levelInstance.GetComponent<Map>();
                map.Spawn(clientId);
                //map.strategistId = clientId;
                //levelInstance.GetComponent<NetworkObject>().SpawnWithOwnership(clientId, true);

                Debug.Log("Spawn strategist");
                player = Instantiate(strategist, map.Vector2to3(map.finish), new Quaternion());
                Strategist strategistIns = player.GetComponent<Strategist>();
                strategistIns.networkId = clientId;
                var playerNetworkObject = player.GetComponent<NetworkObject>();
                playerNetworkObject.SpawnAsPlayerObject(clientId);

                Debug.Log("Temporal camera off");
                Destroy(tempCamera.gameObject);
            }
            else
            {
                trySpawnShooter = true;
            }

        }
    }

    private void Update()
    {
        if (trySpawnShooter)
        {
            ulong clientId = networkManager.LocalClientId;
            GameObject player;

            map = FindAnyObjectByType<Map>();
            if (map == null)
                return;

            EnemyManager enemyManagerIns = Instantiate(enemyManager, Vector3.zero, new Quaternion());
            Debug.Log(enemyManagerIns);
            enemyManagerIns.Spawn(map, clientId);

            Debug.Log("Spawn shooter");
            player = Instantiate(shooter, map.Vector2to3(map.finish), new Quaternion());

            player.GetComponent<Shooter>().networkId = clientId;
            var playerNetworkObject = player.GetComponent<NetworkObject>();
            playerNetworkObject.SpawnAsPlayerObject(clientId);

            enemyManagerIns.spawnEnable = true;

            Debug.Log("Camera off");
            Destroy(tempCamera.gameObject);

            trySpawnShooter = false;
        }
    }

    private void OnGUI()
    {
        if (_state != ConnectionState.Connected)
        {
            GUI.enabled = _state != ConnectionState.Connecting;

            using (new GUILayout.HorizontalScope(GUILayout.Width(250)))
            {
                GUILayout.Label("Profile Name", GUILayout.Width(100));
                _profileName = GUILayout.TextField(_profileName);
            }

            using (new GUILayout.HorizontalScope(GUILayout.Width(250)))
            {
                GUILayout.Label("Session Name", GUILayout.Width(100));
                _sessionName = GUILayout.TextField(_sessionName);
            }

            GUI.enabled = GUI.enabled && !string.IsNullOrEmpty(_profileName) && !string.IsNullOrEmpty(_sessionName);

            if (GUILayout.Button("Create or Join Session"))
            {
                _ = CreateOrJoinSessionAsync();
            }
        }
    }

    private void OnDestroy()
    {
        _session?.LeaveAsync();
    }

    private async Task CreateOrJoinSessionAsync()
    {
        _state = ConnectionState.Connecting;

        try
        {
            AuthenticationService.Instance.SwitchProfile(_profileName);
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            var options = new SessionOptions()
            {
                Name = _sessionName,
                MaxPlayers = _maxPlayers
            }.WithDistributedAuthorityNetwork();

            _session = await MultiplayerService.Instance.CreateOrJoinSessionAsync(_sessionName, options);

            _state = ConnectionState.Connected;
        }
        catch (Exception e)
        {
            _state = ConnectionState.Disconnected;
            Debug.LogException(e);
        }
    }
}