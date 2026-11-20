using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;

public class PartySessionService
{
    public async Task<string> CreatePartyAsync()
    {
        await EnsureServicesReadyAsync();

        var options = new SessionOptions
        {
            MaxPlayers = 2,
            Name = "Party",
            Type = "party"
        };
        options.WithPlayerName();
        options.WithRelayNetwork();

        if (string.IsNullOrEmpty(AuthenticationService.Instance.PlayerName))
        {
            await AuthenticationService.Instance.GetPlayerNameAsync();
        }
        var session = await MultiplayerService.Instance.CreateSessionAsync(options);
        return session.Code;
    }

    public async Task<string> JoinPartyAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Enter a session code.", nameof(code));
        }

        await EnsureServicesReadyAsync();

        var options = new JoinSessionOptions
        {
            Type = "party"
        };
        options.WithPlayerName();

        var session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code.Trim(), options);
        return session.Code;
    }

    static async Task EnsureServicesReadyAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }
}
