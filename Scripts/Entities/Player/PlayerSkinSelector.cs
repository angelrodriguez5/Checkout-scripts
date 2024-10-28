using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Helper class to manage swapping the skin of kaykit character.
/// All the parts of all the available skins need to be instanced in the 
/// corresponding part of the rig and added as a PlayerModelData
/// </summary>
public class PlayerSkinSelector : MonoBehaviour
{
    [Header("Skin parts")]
    [SerializeField] GameObject _body;
    [SerializeField] GameObject _head;
    [SerializeField] GameObject _leftArm;
    [SerializeField] GameObject _rightArm;

    [Header("Blink effect")]
    [SerializeField] bool blinkActive;
    [SerializeField] float blinkSpeed = 3.5f;
    [Tooltip("The target color for the blink. The amount of blend between the origial color an this one" +
        "is set on the alpha channel")]
    [SerializeField] Color blinkOverlayColor;

    PlayerSkin _currentSkin;
    Color _currentColor;

    List<Material> _allMaterials = new List<Material>();
    List<Material> _teamColorMaterialInstances = new List<Material>();
    List<Color> _allOriginalColors = new List<Color>();
    List<Color> _blinkDestinationColors = new List<Color>();
    bool _blinkToWhite;
    float t;

    private void Update()
    {
        if (blinkActive)
        {
            // Advance Lerp
            t += Time.deltaTime * blinkSpeed;

            // Set all material colors
            for (int i = 0; i < _allMaterials.Count; i++)
            {
                Color color;
                if (_blinkToWhite)
                {
                    color = Color.Lerp(_allOriginalColors[i], _blinkDestinationColors[i], t);
                }
                else
                {
                    color = Color.Lerp(_blinkDestinationColors[i], _allOriginalColors[i], t);
                }

                _allMaterials[i].color = color;
            }

            // When we reach Lerp destination, invert lerp direction
            if (t >= 1f)
            {
                _blinkToWhite = !_blinkToWhite;
                t = 0;
            }
        }
    }

    public void ChangeSkin(PlayerSkin skin)
    {
        ShowModel(skin);
        ShowColor(_currentColor);
    }

    public void ShowModel(PlayerSkin skin)
    {
        _currentSkin = skin;
        // Only clear this when changing skin
        _teamColorMaterialInstances.Clear();

        // Delete the previous/placeholder skin and instantiate the new one in its place
        var newHead = Instantiate(skin.Head, _head.transform.parent);
        Destroy(_head);
        _head = newHead;

        var newBody = Instantiate(skin.Body, _body.transform.parent);
        Destroy(_body);
        _body = newBody;

        var newLeftArm = Instantiate(skin.LeftArm, _leftArm.transform.parent);
        Destroy(_leftArm);
        _leftArm = newLeftArm;

        var newRightArm = Instantiate(skin.RightArm, _rightArm.transform.parent);
        Destroy(_rightArm);
        _rightArm = newRightArm;
    }

    public void ShowColor(Color color)
    {
        _currentColor = color;

        // Get all mesh renderers from the current skin
        var meshRenderers = new List<MeshRenderer> {
                    _body.GetComponent<MeshRenderer>(),
                    _head.GetComponent<MeshRenderer>(),
                    _leftArm.GetComponent<MeshRenderer>(),
                    _rightArm.GetComponent<MeshRenderer>()
                };
        meshRenderers.AddRange(_body.GetComponentsInChildren<MeshRenderer>());
        meshRenderers.AddRange(_head.GetComponentsInChildren<MeshRenderer>());
        meshRenderers.AddRange(_leftArm.GetComponentsInChildren<MeshRenderer>());
        meshRenderers.AddRange(_rightArm.GetComponentsInChildren<MeshRenderer>());

        _allMaterials.Clear();
        foreach (var renderer in meshRenderers)
        {
            // Change the instances of the team color materials in the model parts
            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                if (_currentSkin.TeamColorMaterials.Contains(renderer.sharedMaterials[i]))
                    _teamColorMaterialInstances.Add(renderer.materials[i]);
            }

            // Take this time to cache all materials
            _allMaterials.AddRange(renderer.materials);
        }

        // Done this way to allow changing color multiple times
        foreach (var matInstance in _teamColorMaterialInstances)
        {
            matInstance.color = color;
        }

        // Save original colors
        _allOriginalColors.Clear();
        _allOriginalColors.AddRange(_allMaterials.Select(mat => mat.color));

        // Cache blink color
        _blinkDestinationColors.Clear();
        for (int i = 0; i < _allOriginalColors.Count; i++)
        {
            var c = Color.Lerp(_allOriginalColors[i], blinkOverlayColor, blinkOverlayColor.a);
            c.a = 1f;
            _blinkDestinationColors.Add(c);
        }
    }

    public void SetBlinkEffect(bool value)
    {
        blinkActive = value;

        if (value)
        {
            _blinkToWhite = true;
            t = 0f;
        }
        else
        { 
            // Restore original colors
            for (int i = 0; i < _allMaterials.Count; i++)
            {
                _allMaterials[i].color = _allOriginalColors[i];
            }
        }
    }
}
