using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;


[CreateAssetMenu(menuName = "PartyGame/Player Skin")]
public class PlayerSkin : ScriptableObject
{
    [System.Serializable]
    private struct StickerData
    {
        public PlayerColor color;
        public Sprite stickerHead;
        public Sprite stickerFull;
    }

    [SerializeField] private int _id;
    [SerializeField] private LocalizedString _skinName;
    [SerializeField] private StickerData[] _stickers;

    [Header("Skin parts")]
    [SerializeField] GameObject _body;
    [SerializeField] GameObject _head;
    [SerializeField] GameObject _leftArm;
    [SerializeField] GameObject _rightArm;

    [Header("Materials")]
    [Tooltip("The materials in the skin prefabs that will be colored with the player's color")]
    [SerializeField] Material[] _teamColorMaterials;

    private static List<PlayerSkin> _allSkins;

    public int Id => _id;
    public LocalizedString SkinName => _skinName;
    private StickerData[] Stickers => _stickers;
    public GameObject Body => _body;
    public GameObject Head => _head;
    public GameObject LeftArm => _leftArm;
    public GameObject RightArm => _rightArm;
    public Material[] TeamColorMaterials => _teamColorMaterials;

    public static List<PlayerSkin> GetAll()
    {
        if (_allSkins == null)
        {
            _allSkins = Resources.LoadAll<PlayerSkin>("PlayerSkins").OrderBy(skin => skin.Id).ToList();
        }

        return _allSkins;
    }

    /// <summary>
    /// Returns a player skin based on the current skin and an offset
    /// </summary>
    /// <param name="skin">The reference skin</param>
    /// <param name="offset">whether we want the next or previous skin</param>
    /// <returns></returns>
    public static PlayerSkin GetAdjacentSkin(PlayerSkin skin, int offset = 1)
    {
        GetAll();

        int idx = _allSkins.IndexOf(skin);

        // Avoid negative offset in case idx=0 to avoid negative indexes due to modulus returning negative values
        offset = offset >= 0 ? offset : (offset % _allSkins.Count) + _allSkins.Count;
        idx = (idx + offset) % _allSkins.Count;

        return _allSkins[idx];
    }

    public Sprite GetStickerHead(PlayerColor color)
    {
        return Stickers.Where(data => data.color == color).Select(data => data.stickerHead).First();
    }

    public Sprite GetStickerFull(PlayerColor color)
    {
        return Stickers.Where(data => data.color == color).Select(data => data.stickerFull).First();
    }
}
