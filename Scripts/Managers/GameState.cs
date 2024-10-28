using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EGameState
{
    Menu,
    MatchSettings,
    PlayerSelection,
    Game,
    Pause,
    DeviceLost,
}

public static class GameState
{
    public static EGameState CurrentState { get; private set; }

    public static event Action onStateChanged;

    public static void SetState(EGameState newState)
    {
        CurrentState = newState;
        onStateChanged?.Invoke();
    }
}
