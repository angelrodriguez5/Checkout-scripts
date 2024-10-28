using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "PartyGame/Player Color")]
public class PlayerColor : ScriptableObject
{
    [SerializeField] Color _color;

    public Color Color => _color;

    private static List<PlayerColor> _allColors;
    public static List<PlayerColor> GetAll()
    {
        if (_allColors == null)
        {
            _allColors = Resources.LoadAll<PlayerColor>("PlayerColors").OrderBy(color => color.Color.grayscale).ToList();
        }

        return _allColors;
    }
}
