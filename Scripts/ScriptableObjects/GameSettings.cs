using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ENpcAmount
{
    None = 0,
    Few = 5,
    Normal = 9,
    Lots = 13
}

public enum EMatchDuration
{
    Unlimited = 0,
    Short = 90,
    Normal = 120,
    Long = 150
}

public enum EListItemAmount
{
    Few = 4,
    Normal = 8,
    Lots = 12,
}

public enum EGamemode
{
    Random,
    TimeAttack,
    Memory,
    Pandemic,
    Sequential
}

[CreateAssetMenu(menuName ="PartyGame/GameSettings")]
public class GameSettings : ScriptableObject
{
    [Header("Backend config")]
    public bool isTest = false;
    [Tooltip("The minimum quantity of each item that will spawn in a supermarket")]
    public int minItemAmount = 3;

    [Header("General")]
    public EGamemode gamemode;
    public EMatchDuration matchDuration = EMatchDuration.Normal;
    public ENpcAmount npcAmount = ENpcAmount.Normal;
    public EListItemAmount listItemAmount = EListItemAmount.Normal;
    public bool allPlayersSameList = false;

    [Header("Supermarket")]
    public bool shuffleSectionItems = false;
    public SupermarketTheme supermarketTheme;

    // In case of gamemode = Random, store the value chosen for the current match
    [HideInInspector] public EGamemode matchGamemode;

    [Header("Supermarket")]
    public bool isTournament = false;
    public int tournamentWinsTarget = 3;
    public Dictionary<PlayerAsset, int> tournamentPlayerWins = new Dictionary<PlayerAsset, int>();
    public int tournamentMatchIndex;
    public List<EGamemode> tournamentGamemodes;
    public List<string> tournamentSceneNames;

    // Constants
    public const int MEMORY_LIST_TIME_BEFORE_MATCH = 5;
    public const int MEMORY_LIST_TIME_LAST_CHANCE = 5;
    public const int MEMORY_TIME_BEFORE_CHEAT_APPEARS = 150;

    // Game settings that will hold the current match's configuration
    public static GameSettings _current;
    public static GameSettings Current
    {
        get
        {
            if (!_current)
                _current = Resources.Load<GameSettings>(@"GameSettings/current");
            return _current;
        }
    }

    private void Awake()
    {
        this.hideFlags = HideFlags.DontUnloadUnusedAsset;
    }

    public void CopySettings(GameSettings other)
    {
        this.gamemode = other.gamemode;

        this.matchDuration = other.matchDuration;
        this.npcAmount = other.npcAmount;
        this.listItemAmount = other.listItemAmount;
        this.allPlayersSameList = other.allPlayersSameList;

        this.shuffleSectionItems = other.shuffleSectionItems;
    }
}
