using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NPCSkinGenerator : MonoBehaviour
{
    [Serializable]
    private struct MaterialCustomization
    {
        public string name;
        public Material material;
        public Color[] colorChoices;
        [HideInInspector] public Color chosenColor;

        public void ChooseColor()
        {
            chosenColor = colorChoices.GetRandomElement();
        }
    }

    [Serializable]
    private struct Body
    {
        public GameObject body, rightArm, leftArm;
        public void SetActive(bool value)
        {
            body?.SetActive(value);
            rightArm?.SetActive(value);
            leftArm?.SetActive(value);
        }

        public List<Renderer> GetRenderers()
        {
            var renderers = new List<Renderer>();
            renderers.AddRange(body.GetComponentsInChildren<Renderer>());
            renderers.AddRange(leftArm.GetComponentsInChildren<Renderer>());
            renderers.AddRange(rightArm.GetComponentsInChildren<Renderer>());
            return renderers;
        }

        public override bool Equals(object obj)
        {
            return obj is Body body &&
                   EqualityComparer<GameObject>.Default.Equals(this.body, body.body) &&
                   EqualityComparer<GameObject>.Default.Equals(rightArm, body.rightArm) &&
                   EqualityComparer<GameObject>.Default.Equals(leftArm, body.leftArm);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(body, rightArm, leftArm);
        }
    }

    [Header("Skin parts")]
    [SerializeField] private GameObject[] headChoices;
    [SerializeField] private Body[] bodyChoices;

    [Header("Part randomization")]
    [SerializeField] private MaterialCustomization[] customizedMaterials;

    List<Renderer> renderers = new List<Renderer>();
    GameObject chosenHead;
    Body chosenBody;

    private void Start()
    {
        RandomizeSkin();
    }

    private void OnDestroy()
    {
        chosenHead?.SetActive(false);
        chosenBody.SetActive(false);
    }

    public void RandomizeSkin()
    {
        // Randomize colors, saving the modified structs
        for (int i = 0; i < customizedMaterials.Length; i++)
        {
            customizedMaterials[i].ChooseColor();
        }

        // Choose the parts of the model and show them
        chosenHead = headChoices.GetRandomElement();
        foreach (var head in headChoices)
        {
            head.SetActive(head == chosenHead);
        }

        chosenBody = bodyChoices.GetRandomElement();
        foreach (var body in bodyChoices)
        {
            body.SetActive(body.Equals(chosenBody));
        }

        // Apply the colors to the renderers of the chosen skin parts
        renderers = new List<Renderer>();
        renderers.AddRange(chosenBody.GetRenderers());
        renderers.AddRange(chosenHead.GetComponentsInChildren<Renderer>());

        var materialAssignedColorsDict = new Dictionary<int, Color>();
        foreach (var renderer in renderers)
        {
            // Search for all materials before changing anything. Instanting a material seems to change the sharedMaterials[]
            foreach (var customization in customizedMaterials)
            {
                var idx = Array.IndexOf(renderer.sharedMaterials, customization.material);
                if (idx != -1)
                {
                    materialAssignedColorsDict.Add(idx, customization.chosenColor);
                }
            }

            // Change all materials present in this renderer
            foreach (var item in materialAssignedColorsDict)
            {
                renderer.materials[item.Key].color = item.Value;
            }

            materialAssignedColorsDict.Clear();
        }
    }
}
