using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BetweenMatchesCardUI : MonoBehaviour
{
    [SerializeField] Transform _crownsLayout;
    [SerializeField] Color _crownActiveColor;
    [SerializeField] Color _crownInactiveColor;
    [SerializeField] Image _playerSticker;
    [SerializeField] Image _background;

    GameObject currentWinCrown;

    // Length of the animation in seconds
    [SerializeField] float _animationLength;

    public void Initialize(PlayerAsset player, Sprite narrowBanner, bool isWinner = false)
    {
        // Customize player sticker and background
        _playerSticker.sprite = player.PlayerStickerHead;
        _background.sprite = narrowBanner;

        // Show the appropriate number of crowns, and highlight player wins
        var playerWins = GameSettings.Current.tournamentPlayerWins[player];
        var crownsToActivate = GameSettings.Current.tournamentWinsTarget;
        foreach (Transform child in _crownsLayout)
        {
            var crown = child.GetComponent<Image>();

            // Highlight as many stars as player wins
            if (playerWins > 0)
            {
                crown.color = _crownActiveColor;
                playerWins--;
                
                // For the winner of the round, the last crown will be animated, save a reference to it
                if (isWinner && playerWins == 0)
                {
                    currentWinCrown = crown.gameObject;
                    currentWinCrown.GetComponent<Image>().color = _crownInactiveColor;
                }
            }
            else
            {
                crown.color = _crownInactiveColor;
            }

            // Only show the number of crowns winnable in this tournament
            crown.gameObject.SetActive(crownsToActivate > 0);
            crownsToActivate--;
        }
    }

    private IEnumerator AppearAnimationCoroutine(Vector3 finalScale)
    {
        // Define the initial scale of the card (scale 0 to make it appear as if it "pops")
        Vector3 initialScale = transform.localScale;

        // Start the card animation
        float elapsedTime = 0;
        while (elapsedTime < _animationLength)
        {
            // Calculate the progress of the animation
            float t = elapsedTime / _animationLength;

            // Interpolate the scale of the card from its initial scale to the final scale
            transform.localScale = Vector3.Lerp(initialScale, finalScale, t);

            // Increase the elapsed time
            elapsedTime += Time.deltaTime;

            // Wait one frame before continuing with the next iteration of the loop
            yield return null;
        }

        // Set the final scale of the card as the final scale
        transform.localScale = finalScale;
    }

    public void StartAppearAnimation(Vector3 finalScale)
    {
        StartCoroutine(AppearAnimationCoroutine(finalScale));
    }

    public void AnimateWinnerCrown()
    {
        if (currentWinCrown == null) return;

        // Change crown color to active crown color
        currentWinCrown.GetComponent<Image>().color = _crownActiveColor;

        currentWinCrown.GetComponent<Animation>().Play();
    }
}
