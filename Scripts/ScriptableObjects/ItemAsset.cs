using UnityEngine;

public enum EItemCategory
{
    Tools,
    Weapons,
    Vegetables,
    FoodAndDrink,
    Electronics,
    Toys,
    Misc,
    Alchemy,
    BooksAndRunes,
    Containers,
    HealingItems,
    DealItems
}

[CreateAssetMenu(menuName = "PartyGame/Item")]
public class ItemAsset : ScriptableObject
{
    [SerializeField] protected string _itemName;
    [SerializeField] protected Sprite _itemIcon;
    [SerializeField] protected GameObject _prefab;
    [SerializeField] protected Vector3 _inHandRotation;
    [SerializeField] protected EItemCategory _itemCategory;

    public string ItemName => _itemName;
    public Sprite ItemIcon => _itemIcon;
    public GameObject ItemPrefab => _prefab;
    public Vector3 InHandRotation => _inHandRotation;
    public EItemCategory ItemCategory => _itemCategory;

    public GameObject SpawnNewGameObject()
    {
        var instance = Instantiate(_prefab);

        ItemBehaviour behaviour;
        if (instance.TryGetComponent<ItemBehaviour>(out behaviour))
        {
            behaviour.ItemAsset = this;
        }
        else
        {
            behaviour = instance.AddComponent<ItemBehaviour>();
            behaviour.ItemAsset = this;
        }

        return instance;
    }
}


//[CustomEditor(typeof(ItemAsset))]
//public class ItemAssetEditor : Editor
//{
//    public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
//    {
//        ItemAsset item = (ItemAsset)target;

//        if (item == null || item.Prefab == null || AssetPreview.IsLoadingAssetPreview(item.Prefab.GetInstanceID()))
//            return null;

//        Texture2D tex = new Texture2D(width, height);
//        EditorUtility.CopySerialized(AssetPreview.GetAssetPreview(item.Prefab), tex);
//        return tex;
//    }
//}