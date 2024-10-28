using UnityEngine;
using System.Collections;
using System.IO;

public class IconGenerator : MonoBehaviour
{
    public Camera screenshotCamera;
    public GameObject[] objectsToCapture;
    public string outputFolderPath;

    // Start is called before the first frame update
    void Start()
    {
        // Set the output folder path to the Application data path with a folder called "Icons"
        outputFolderPath = Path.Combine(Application.dataPath, "Icons");

        // Create the output folder if it doesn't exist
        if (!Directory.Exists(outputFolderPath))
        {
            Directory.CreateDirectory(outputFolderPath);
        }
        // Start the coroutine to capture the objects
        StartCoroutine(CaptureObjectsCoroutine());
    }

    private IEnumerator CaptureObjectsCoroutine()
    {
        // Loop through each object in the array
        foreach (GameObject obj in objectsToCapture)
        {
            // Activate the current object
            obj.SetActive(true);

            // Wait for a frame to ensure the object is fully activated
            yield return null;

            // Set the output file path to the object name + ".png" in the output folder
            string outputFilePath = Path.Combine(outputFolderPath, obj.name + ".png");

            // Capture a screenshot of the object with the camera
            ScreenCapture.CaptureScreenshot(outputFilePath, 1);

            // Wait for a frame to ensure the screenshot is fully captured
            yield return null;

            // Deactivate the current object
            obj.SetActive(false);
        }

        Debug.Log("Finished capturing objects.");
    }
}
