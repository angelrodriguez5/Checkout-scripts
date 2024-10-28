using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerItemList
{
    /// <summary>
    /// Personalise the list with the player's color and his items
    /// </summary>
    /// <param name="player"></param>
    /// <param name="items"></param>
    public void Initialise(PlayerAsset player, List<ItemAsset> items);

    /// <summary>
    /// Manually update the UI to ensure the list is correctly displaying the current game state
    /// </summary>
    public void UpdateUI();
}
