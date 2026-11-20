using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LocalCharacterPresentation : NetworkBehaviour
{
    [SerializeField] List<GameObject> activeForNonOwnerOnly = new();
    [SerializeField] List<GameObject> activeForOwnerOnly = new();

    void Awake()
    {
        ApplyPresentation(false);
    }

    public override void OnNetworkSpawn()
    {
        ApplyPresentation(IsOwner);
    }

    public override void OnGainedOwnership()
    {
        ApplyPresentation(IsOwner);
    }

    public override void OnLostOwnership()
    {
        ApplyPresentation(false);
    }

    void ApplyPresentation(bool isOwner)
    {
        foreach (var gameObjectToHide in activeForNonOwnerOnly)
        {
            if (gameObjectToHide != null)
            {
                gameObjectToHide.SetActive(!isOwner);
            }
        }

        foreach (var gameObjectToShow in activeForOwnerOnly)
        {
            if (gameObjectToShow != null)
            {
                gameObjectToShow.SetActive(isOwner);
            }
        }
    }
}
