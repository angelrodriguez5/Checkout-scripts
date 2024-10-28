using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
    public class PlayerPhotoShootController : MonoBehaviour
    {
        [SerializeField] private CameraCapture _cameraCapture;
        [Tooltip("This folder will be created on %appdata%/LocalLow/OctopusGalaecus")]
        [SerializeField] private string _outputFolderName = "ItemIcons";
        [SerializeField] private PlayerSkinSelector _skinSelector;

        private void Start() => StartCoroutine(Photoshoot());

        private IEnumerator Photoshoot()
        {
            var allSkins = PlayerSkin.GetAll();
            var allColors = PlayerColor.GetAll();
            var wait = new WaitForSeconds(0.2f);
            Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, _outputFolderName));

            foreach (var skin in allSkins)
            {
                var skinName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(skin));
                _skinSelector.ShowModel(skin);

                foreach (var color in allColors)
                {
                    var colorName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(color));
                    _skinSelector.ShowColor(color.Color);

                    var path = Path.Combine(Application.persistentDataPath, _outputFolderName, $"{skinName}_{colorName}.png");
                    yield return wait;
                    _cameraCapture.Capture(path);
                    Debug.Log($"Image captured: {path}");
                    yield return wait;
                }
            }

            Debug.Log("---- DONE ----");
        }
    }
#endif
