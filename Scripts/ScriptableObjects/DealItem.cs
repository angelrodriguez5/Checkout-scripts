using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EDealType
{
    OneItem,
    TwoItems
}

[CreateAssetMenu(menuName = "PartyGame/Deal Item")]
public class DealItem : ItemAsset
{
    [SerializeField] EDealType _dealType;

    public EDealType DealType => _dealType;

    private void OnValidate()
    {
        this._itemCategory = EItemCategory.DealItems;
    }
}
