using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ItemPhotoShootController : MonoBehaviour
{
    [SerializeField] private CameraCapture _cameraCapture;
    [Tooltip("This folder will be created on %appdata%/LocalLow/OctopusGalaecus")]
    [SerializeField] private string _outputFolderName = "ItemIcons";
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private ItemSet _itemSet;

    private void Start() => StartCoroutine(Photoshoot());

    private IEnumerator Photoshoot()
    {
        var wait = new WaitForSeconds(0.25f);
        Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, _outputFolderName));

        foreach(var itemAsset in _itemSet.Items)
        {
            var item = itemAsset.SpawnNewGameObject();
            item.transform.position = _spawnPoint.position;
            item.transform.rotation = _spawnPoint.rotation;

            var path = Path.Combine(Application.persistentDataPath, _outputFolderName, $"{itemAsset.ItemName}.png");
            _cameraCapture.Capture(path);
            yield return wait;
            Destroy(item);
            yield return wait;
        }
    }
}
