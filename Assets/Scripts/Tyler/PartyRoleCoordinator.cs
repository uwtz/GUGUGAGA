using System;
using Unity.Netcode;
using UnityEngine;

public enum PartyRole : byte
{
    Parent,
    Baby
}

[RequireComponent(typeof(NetworkObject))]
public class PartyRoleCoordinator : NetworkBehaviour
{
    public const ulong NoClient = ulong.MaxValue;

    public static PartyRoleCoordinator Instance { get; private set; }
    public static event Action<PartyRoleCoordinator> Changed;

    readonly NetworkVariable<ulong> m_ParentOwner = new(
        NoClient,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    readonly NetworkVariable<ulong> m_BabyOwner = new(
        NoClient,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    readonly NetworkVariable<bool> m_RolesLocked = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public ulong ParentOwnerClientId => m_ParentOwner.Value;
    public ulong BabyOwnerClientId => m_BabyOwner.Value;
    public bool RolesLocked => m_RolesLocked.Value;
    public bool HasBothRoles =>
        m_ParentOwner.Value != NoClient &&
        m_BabyOwner.Value != NoClient &&
        m_ParentOwner.Value != m_BabyOwner.Value;

    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public bool CanClaimRole =>
        IsSpawned &&
        !m_RolesLocked.Value &&
        Unity.Netcode.NetworkManager.Singleton != null &&
        Unity.Netcode.NetworkManager.Singleton.IsListening;

    public override void OnNetworkSpawn()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("More than one PartyRoleCoordinator is spawned.");
            return;
        }

        Instance = this;
        m_ParentOwner.OnValueChanged += OnRoleChanged;
        m_BabyOwner.OnValueChanged += OnRoleChanged;
        m_RolesLocked.OnValueChanged += OnLockChanged;
        NetworkManager.OnClientConnectedCallback += OnClientListChanged;
        NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        Changed?.Invoke(this);
    }

    public override void OnNetworkDespawn()
    {
        m_ParentOwner.OnValueChanged -= OnRoleChanged;
        m_BabyOwner.OnValueChanged -= OnRoleChanged;
        m_RolesLocked.OnValueChanged -= OnLockChanged;

        if (NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientListChanged;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        if (Instance == this)
        {
            Instance = null;
            Changed?.Invoke(null);
        }
    }

    public void ClaimRole(PartyRole role)
    {
        if (CanClaimRole)
        {
            ClaimRoleServerRpc(role);
        }
    }

    public bool TryGetRoleOwners(out ulong parentOwner, out ulong babyOwner)
    {
        parentOwner = m_ParentOwner.Value;
        babyOwner = m_BabyOwner.Value;
        return HasBothRoles;
    }

    public void SetRolesLocked(bool isLocked)
    {
        if (IsServer)
        {
            m_RolesLocked.Value = isLocked;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void ClaimRoleServerRpc(PartyRole role, RpcParams rpcParams = default)
    {
        if (m_RolesLocked.Value || !Enum.IsDefined(typeof(PartyRole), role))
        {
            return;
        }

        var clientId = rpcParams.Receive.SenderClientId;
        if (!IsClientConnected(clientId) || NetworkManager.ConnectedClientsIds.Count > 2)
        {
            return;
        }

        if (role == PartyRole.Parent)
        {
            if (m_ParentOwner.Value == clientId)
            {
                return;
            }

            var previousParent = m_ParentOwner.Value;
            m_ParentOwner.Value = clientId;
            if (previousParent != NoClient)
            {
                m_BabyOwner.Value = previousParent;
            }
            else if (m_BabyOwner.Value == clientId)
            {
                m_BabyOwner.Value = NoClient;
            }
        }
        else
        {
            if (m_BabyOwner.Value == clientId)
            {
                return;
            }

            var previousBaby = m_BabyOwner.Value;
            m_BabyOwner.Value = clientId;
            if (previousBaby != NoClient)
            {
                m_ParentOwner.Value = previousBaby;
            }
            else if (m_ParentOwner.Value == clientId)
            {
                m_ParentOwner.Value = NoClient;
            }
        }
    }

    bool IsClientConnected(ulong clientId) =>
        NetworkManager != null && NetworkManager.ConnectedClients.ContainsKey(clientId);

    void OnClientDisconnected(ulong clientId)
    {
        if (IsServer)
        {
            if (m_ParentOwner.Value == clientId)
            {
                m_ParentOwner.Value = NoClient;
            }

            if (m_BabyOwner.Value == clientId)
            {
                m_BabyOwner.Value = NoClient;
            }
        }

        OnClientListChanged(clientId);
    }

    void OnClientListChanged(ulong _) => Changed?.Invoke(this);
    void OnRoleChanged(ulong _, ulong __) => Changed?.Invoke(this);
    void OnLockChanged(bool _, bool __) => Changed?.Invoke(this);
}
