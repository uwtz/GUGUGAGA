using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyLobbyUI : MonoBehaviour
{
    [Header("Connection UI")]
    [SerializeField] GameObject connectionPanel;
    [SerializeField] Button hostButton;
    [SerializeField] Button joinButton;
    [SerializeField] TMP_InputField sessionCodeInput;
    [SerializeField] TMP_Text statusText;

    [Header("Party UI")]
    [SerializeField] GameObject partyPanel;
    [SerializeField] TMP_Text sessionCodeText;
    [SerializeField] TMP_Text playersText;
    [SerializeField] Button parentButton;
    [SerializeField] Button babyButton;
    [SerializeField] Button startButton;

    public event Action HostRequested;
    public event Action<string> JoinRequested;
    public event Action ParentRequested;
    public event Action BabyRequested;
    public event Action StartRequested;

    void OnEnable()
    {
        if (!HasRequiredReferences())
        {
            Debug.LogError("Assign all PartyLobbyUI references.", this);
            enabled = false;
            return;
        }

        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
        parentButton.onClick.AddListener(OnParentClicked);
        babyButton.onClick.AddListener(OnBabyClicked);
        startButton.onClick.AddListener(OnStartClicked);
    }

    void Start()
    {
        connectionPanel.SetActive(true);
        partyPanel.SetActive(false);
        startButton.interactable = false;
        statusText.text = "Host a party or join with a code.";
    }

    void OnDisable()
    {
        hostButton?.onClick.RemoveListener(OnHostClicked);
        joinButton?.onClick.RemoveListener(OnJoinClicked);
        parentButton?.onClick.RemoveListener(OnParentClicked);
        babyButton?.onClick.RemoveListener(OnBabyClicked);
        startButton?.onClick.RemoveListener(OnStartClicked);
    }

    bool HasRequiredReferences() =>
        connectionPanel != null &&
        hostButton != null &&
        joinButton != null &&
        sessionCodeInput != null &&
        statusText != null &&
        partyPanel != null &&
        sessionCodeText != null &&
        playersText != null &&
        parentButton != null &&
        babyButton != null &&
        startButton != null;

    public void SetLoading(bool isLoading)
    {
        hostButton.interactable = !isLoading;
        joinButton.interactable = !isLoading;
        sessionCodeInput.interactable = !isLoading;
        if (isLoading)
        {
            statusText.text = "Loading...";
        }
    }

    public void SetStatus(string message) => statusText.text = message;

    public void ShowParty(string code)
    {
        connectionPanel.SetActive(false);
        partyPanel.SetActive(true);
        sessionCodeText.text = $"Session code: {code}";
        statusText.text = "Connected. Choose a role.";
    }

    public void ShowPartyState(string roster, bool canClaimRole, bool canStart)
    {
        playersText.text = roster;
        parentButton.interactable = canClaimRole;
        babyButton.interactable = canClaimRole;
        startButton.interactable = canStart;
    }

    void OnHostClicked() => HostRequested?.Invoke();
    void OnJoinClicked() => JoinRequested?.Invoke(sessionCodeInput.text);
    void OnParentClicked() => ParentRequested?.Invoke();
    void OnBabyClicked() => BabyRequested?.Invoke();
    void OnStartClicked() => StartRequested?.Invoke();
}
