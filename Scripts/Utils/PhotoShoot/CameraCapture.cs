using System.IO;
using UnityEngine;

public class CameraCapture : MonoBehaviour
{
    [SerializeField] private Camera _camera;

    [ContextMenu("Capture")]
    public void Capture(string outputPath)
    {
        RenderTexture activeRenderTexture = RenderTexture.active;
        RenderTexture.active = _camera.targetTexture;

        _camera.Render();

        Texture2D image = new Texture2D(_camera.targetTexture.width, _camera.targetTexture.height);
        image.ReadPixels(new Rect(0, 0, _camera.targetTexture.width, _camera.targetTexture.height), 0, 0);
        image.Apply();
        RenderTexture.active = activeRenderTexture;

        byte[] bytes = image.EncodeToPNG();
        Destroy(image);

        File.WriteAllBytes(outputPath, bytes);
    }
}