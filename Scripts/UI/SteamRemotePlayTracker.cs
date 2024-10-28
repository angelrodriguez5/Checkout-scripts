using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using System;

public class SteamRemotePlayTracker : MonoBehaviour
{
    [SerializeField] Transform _playerCardLayout;
    [SerializeField] GameObject _playerCardPrefab;

    Dictionary<RemotePlaySessionID_t, GameObject> _playerCards = new Dictionary<RemotePlaySessionID_t, GameObject>();
    
    protected Callback<SteamRemotePlaySessionConnected_t> _steamRemotePlaySessionConnected;
    protected Callback<SteamRemotePlaySessionDisconnected_t> _steamRemotePlaySessionDisconnected;

    private void Start()
    {
        // Destroy placeholders
        foreach(Transform child in _playerCardLayout)
            Destroy(child.gameObject);

        if (SteamManager.Initialized)
        {
            // Subscribe to callbacks
            _steamRemotePlaySessionConnected = new Callback<SteamRemotePlaySessionConnected_t>(OnSteamRemoteSessionConnected);
            _steamRemotePlaySessionDisconnected = new Callback<SteamRemotePlaySessionDisconnected_t>(OnSteamRemoteSessionDisconnected);

            // Manually add local player card
            var playerCard = Instantiate(_playerCardPrefab, _playerCardLayout).GetComponent<SteamPlayerCard>();
            playerCard.Initialise(SteamUser.GetSteamID());
        }

        // Check if remote play sessions already exist
        for (int i = 0; i < SteamRemotePlay.GetSessionCount(); i++)
        {
            var sessionId = SteamRemotePlay.GetSessionID(i);
            AddRemoteSessionAvatar(sessionId);
        }
    }

    public void InviteFriendsRemotePlay()
    {
        if (SteamManager.Initialized)
        {
            SteamFriends.ActivateGameOverlayRemotePlayTogetherInviteDialog(SteamUser.GetSteamID());
        }
    }

    private void OnSteamRemoteSessionDisconnected(SteamRemotePlaySessionDisconnected_t param)
    {
        var obj = _playerCards[param.m_unSessionID];
        _playerCards.Remove(param.m_unSessionID);
        Destroy(obj);
    }

    private void OnSteamRemoteSessionConnected(SteamRemotePlaySessionConnected_t param)
    {
        AddRemoteSessionAvatar(param.m_unSessionID);
    }

    private void AddRemoteSessionAvatar(RemotePlaySessionID_t sessionId)
    {
        var userId = SteamRemotePlay.GetSessionSteamID(sessionId);
        var playerCard = Instantiate(_playerCardPrefab, _playerCardLayout).GetComponent<SteamPlayerCard>();
        playerCard.Initialise(userId);

        _playerCards.Add(sessionId, playerCard.gameObject);
    }
}
