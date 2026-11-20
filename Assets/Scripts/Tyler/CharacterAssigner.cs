using Unity.Netcode;
using UnityEngine;

public class CharacterAssigner : NetworkBehaviour
{
    [SerializeField] NetworkObject parent;
    [SerializeField] NetworkObject baby;

    bool m_OwnershipAssigned;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            return;
        }

        if (NetworkManager.SceneManager == null)
        {
            Debug.LogError("Enable NGO scene management to assign the party characters.");
            return;
        }

        NetworkManager.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager != null && NetworkManager.SceneManager != null)
        {
            NetworkManager.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
        }
    }

    void OnLoadEventCompleted(
        string sceneName,
        UnityEngine.SceneManagement.LoadSceneMode loadSceneMode,
        System.Collections.Generic.List<ulong> clientsCompleted,
        System.Collections.Generic.List<ulong> clientsTimedOut)
    {
        if (sceneName == "Main")
        {
            AssignOwnershipIfReady();
        }
    }

    void AssignOwnershipIfReady()
    {
        if (m_OwnershipAssigned || !IsServer)
        {
            return;
        }

        if (parent == null || baby == null)
        {
            Debug.LogError("Assign the Parent and Baby NetworkObjects in CharacterAssigner.");
            return;
        }

        if (!parent.IsSpawned || !baby.IsSpawned)
        {
            return;
        }

        if (PartyRoleCoordinator.Instance == null ||
            !PartyRoleCoordinator.Instance.TryGetRoleOwners(out var parentOwner, out var babyOwner))
        {
            Debug.LogError("The party has no complete Parent/Baby role assignment.");
            return;
        }

        parent.ChangeOwnership(parentOwner);
        baby.ChangeOwnership(babyOwner);
        m_OwnershipAssigned = true;
    }
}