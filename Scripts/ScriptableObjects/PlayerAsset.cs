using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;

[CreateAssetMenu(menuName = "PartyGame/Player")]
public class PlayerAsset : ScriptableObject
{
    [SerializeField] private int _id;
    [SerializeField] private PlayerColor _color;
    [SerializeField] private PlayerSkin _skin;

    public int Id => _id;
    public bool Active { get; set; } = false;
    public Color PlayerColor => _color.Color;
    public PlayerColor ColorAsset => _color;
    public Sprite PlayerStickerHead => _skin.GetStickerHead(_color);
    public Sprite PlayerStickerFull => _skin.GetStickerFull(_color);
    public LocalizedString PlayerName => _skin.SkinName;
    public PlayerSkin PlayerSkin { get => _skin; set => _skin = value; }

    // Input system properties
    public InputDevice Device { get; set; }
    public string ControlScheme { get; set; }

    #region Equals
    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        PlayerAsset other = obj as PlayerAsset;
        return this.Id == other.Id;
    }

    // override object.GetHashCode
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
    #endregion
}
