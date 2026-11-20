using System;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PartyLobbyUI))]
public class PartyLobbyController : MonoBehaviour
{
    const string k_GameplaySceneName = "Main";

    PartyLobbyUI partyLobbyUI;

    readonly PartySessionService m_SessionService = new();
    PartyRoleCoordinator m_RoleCoordinator;
    bool m_IsBusy;
    bool m_StartRequested;

    void Awake()
    {
        partyLobbyUI = GetComponent<PartyLobbyUI>();
    }

    void OnEnable()
    {
        partyLobbyUI.HostRequested += HostParty;
        partyLobbyUI.JoinRequested += JoinParty;
        partyLobbyUI.ParentRequested += ClaimParent;
        partyLobbyUI.BabyRequested += ClaimBaby;
        partyLobbyUI.StartRequested += StartGame;
        PartyRoleCoordinator.Changed += OnRoleCoordinatorChanged;

        if (PartyRoleCoordinator.Instance != null)
        {
            OnRoleCoordinatorChanged(PartyRoleCoordinator.Instance);
        }
    }

    void OnDisable()
    {
        if (partyLobbyUI != null)
        {
            partyLobbyUI.HostRequested -= HostParty;
            partyLobbyUI.JoinRequested -= JoinParty;
            partyLobbyUI.ParentRequested -= ClaimParent;
            partyLobbyUI.BabyRequested -= ClaimBaby;
            partyLobbyUI.StartRequested -= StartGame;
        }

        PartyRoleCoordinator.Changed -= OnRoleCoordinatorChanged;
    }

    async void HostParty()
    {
        if (m_IsBusy)
        {
            return;
        }

        m_IsBusy = true;
        partyLobbyUI.SetLoading(true);
        try
        {
            await Task.Yield();
            var code = await m_SessionService.CreatePartyAsync();
            partyLobbyUI.ShowParty(code);
            RefreshPartyUI();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            partyLobbyUI.SetStatus($"Could not host party: {exception.Message}");
        }
        finally
        {
            m_IsBusy = false;
            partyLobbyUI.SetLoading(false);
        }
    }

    async void JoinParty(string code)
    {
        if (m_IsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            partyLobbyUI.SetStatus("Enter a session code.");
            return;
        }

        m_IsBusy = true;
        partyLobbyUI.SetLoading(true);
        try
        {
            await Task.Yield();
            var joinedCode = await m_SessionService.JoinPartyAsync(code);
            partyLobbyUI.ShowParty(joinedCode);
            RefreshPartyUI();
        }
        catch (SessionException exception)
        {
            Debug.LogWarning($"Could not join party ({exception.Error}): {exception.Message}");
            partyLobbyUI.SetStatus($"Could not join party: {exception.Message}");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            partyLobbyUI.SetStatus($"Could not join party: {exception.Message}");
        }
        finally
        {
            m_IsBusy = false;
            partyLobbyUI.SetLoading(false);
        }
    }

    void ClaimParent() => m_RoleCoordinator?.ClaimRole(PartyRole.Parent);
    void ClaimBaby() => m_RoleCoordinator?.ClaimRole(PartyRole.Baby);

    void StartGame()
    {
        var networkManager = NetworkManager.Singleton;
        if (!CanStartGame(networkManager))
        {
            partyLobbyUI.SetStatus("Start requires two connected players, both roles, and the host.");
            return;
        }

        if (networkManager.SceneManager == null)
        {
            Debug.LogError("NGO scene management is required to start the game.");
            partyLobbyUI.SetStatus("Cannot start: NGO scene management is disabled.");
            return;
        }

        m_StartRequested = true;
        m_RoleCoordinator.SetRolesLocked(true);
        RefreshPartyUI();

        try
        {
            var status = networkManager.SceneManager.LoadScene(k_GameplaySceneName, LoadSceneMode.Single);
            if (status == SceneEventProgressStatus.Started)
            {
                return;
            }

            Debug.LogError($"Could not load {k_GameplaySceneName}: {status}.");
            partyLobbyUI.SetStatus($"Could not load the game scene: {status}.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            partyLobbyUI.SetStatus($"Could not start the game: {exception.Message}");
        }

        m_StartRequested = false;
        m_RoleCoordinator.SetRolesLocked(false);
        RefreshPartyUI();
    }

    bool CanStartGame(NetworkManager networkManager)
    {
        if (m_StartRequested ||
            networkManager == null ||
            !networkManager.IsServer ||
            !networkManager.IsListening ||
            networkManager.ConnectedClientsIds.Count != 2 ||
            m_RoleCoordinator == null ||
            !m_RoleCoordinator.IsSpawned ||
            m_RoleCoordinator.RolesLocked ||
            !m_RoleCoordinator.TryGetRoleOwners(out var parentOwner, out var babyOwner))
        {
            return false;
        }

        return networkManager.ConnectedClients.ContainsKey(parentOwner) &&
               networkManager.ConnectedClients.ContainsKey(babyOwner);
    }

    void OnRoleCoordinatorChanged(PartyRoleCoordinator coordinator)
    {
        m_RoleCoordinator = coordinator;
        RefreshPartyUI();
    }

    void RefreshPartyUI()
    {
        if (partyLobbyUI == null)
        {
            return;
        }

        if (m_RoleCoordinator == null || !m_RoleCoordinator.IsSpawned)
        {
            partyLobbyUI.ShowPartyState("Waiting for the host...", false, false);
            return;
        }

        var networkManager = NetworkManager.Singleton;
        partyLobbyUI.ShowPartyState(
            BuildRosterText(networkManager),
            m_RoleCoordinator.CanClaimRole,
            CanStartGame(networkManager));
    }

    string BuildRosterText(NetworkManager networkManager)
    {
        var text = new StringBuilder();
        text.AppendLine("Players:");

        if (networkManager == null)
        {
            return text.Append("Waiting for network connection.").ToString();
        }

        foreach (var clientId in networkManager.ConnectedClientsIds)
        {
            var role = clientId == m_RoleCoordinator.ParentOwnerClientId
                ? "Parent"
                : clientId == m_RoleCoordinator.BabyOwnerClientId
                    ? "Baby"
                    : "choosing a role";
            text.Append("User ").Append(clientId + 1).Append(" (").Append(role).AppendLine(")");
        }

        return text.ToString();
    }
}
