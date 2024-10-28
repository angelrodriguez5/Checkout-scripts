using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ItemQueueGUI : MonoBehaviour
{
    [System.Serializable]
    private struct ItemBackgroundUIData
    {
        public PlayerColor color;
        public Sprite sprite;
    }

    [SerializeField] ItemBackgroundUIData[] _itemBackgrounds;
    [SerializeField] Image[] _itemImagePool;
    [SerializeField] Sprite _closedSprite;
    [SerializeField] Sprite _successSprite;
    [SerializeField] Sprite _failSprite;

    int _frontIdx = 0;
    int _lastFreeIdx = 0;
    int _closedMarkerIndex;

    private void Awake()
    {
        foreach (var image in _itemImagePool)
        {
            image.gameObject.SetActive(false);
        }
    }

    public void AddItem(ItemBehaviour item, PlayerController player, bool isClosedMarker = false) 
    {
        // Customize image and display it
        var image = _itemImagePool[_lastFreeIdx];
        CustomizeItemImage(image, item, player, isClosedMarker);
        image.gameObject.SetActive(true);

        // Store the position of the closed marker
        if (isClosedMarker)
            _closedMarkerIndex = _lastFreeIdx;

        _lastFreeIdx = (_lastFreeIdx + 1) % _itemImagePool.Length;
    }
    
    public void ItemProcessed(bool wasSuccess)
    {
        // Cycle all images positions
        var previous = _itemImagePool.Last();

        var sequence = DOTween.Sequence();
        for (int i = 0; i < _itemImagePool.Length; i++)
        {
            var image = _itemImagePool[i];

            if (i == _frontIdx)
            {
                // If it is the closed marker dont change the sprite
                if (i == _closedMarkerIndex)
                    _closedMarkerIndex = -1;
                // For the item that was processed show either success or fail
                else if (wasSuccess)
                    image.sprite = _successSprite;
                else
                    image.sprite = _failSprite;

                // Make all this image movements at the beginning of the sequence
                // Scale it
                sequence.Insert(0f, image.rectTransform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack)
                    .OnComplete(() => image.gameObject.SetActive(false)));
                // Then return to its initial scale
                sequence.Insert(0.2f, image.rectTransform.DOScale(image.rectTransform.localScale, 0));
                // Then move it to the end of the queue and match the normal image size
                sequence.Insert(0.2f, image.rectTransform.DOMove(previous.rectTransform.position, 0));
                sequence.Insert(0.2f, image.rectTransform.DOSizeDelta(previous.rectTransform.sizeDelta, 0));
            }
            else
            {
                // All the rest of the images move at the same time, just as the first one dissappears
                sequence.Insert(0.2f, image.rectTransform.DOMove(previous.rectTransform.position, 0.2f).SetEase(Ease.OutBack));
                sequence.Insert(0.2f, image.rectTransform.DOSizeDelta(previous.rectTransform.sizeDelta, 0.2f).SetEase(Ease.OutBack));
            }

            previous = image;
        }

        // Advance front index
        _frontIdx = (_frontIdx + 1) % _itemImagePool.Length;
    }

    private void CustomizeItemImage(Image image, ItemBehaviour item, PlayerController player, bool isClosedMarker)
    {
        if (isClosedMarker)
        {
            image.sprite = _closedSprite;
            image.transform.GetChild(0).GetComponent<Image>().enabled = false;
        }
        else
        {
            var bg = _itemBackgrounds.First(x => x.color == player.PlayerAsset.ColorAsset).sprite;
            image.sprite = bg;
            image.transform.GetChild(0).GetComponent<Image>().sprite = item.ItemAsset.ItemIcon;
            image.transform.GetChild(0).GetComponent<Image>().enabled = true;
        }
    }
}
