using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class ImageResizer : MonoBehaviour
{
    // The path to the folder containing the images
    public string inputFolder = "InputImages";

    // The path to the folder where the resized images will be saved
    public string outputFolder = "OutputImages";

    // The size of the square to resize the images to
    public int squareSize = 1024;

    void Start()
    {
        // Get the full paths of the input and output folders
        string inputFolderPath = Application.dataPath + "/" + inputFolder;
        string outputFolderPath = Application.dataPath + "/" + outputFolder;

        // Create the output folder if it doesn't exist
        if (!Directory.Exists(outputFolderPath))
        {
            Directory.CreateDirectory(outputFolderPath);
        }

        // Get the paths of all the files in the input folder
        string[] filePaths = Directory.GetFiles(inputFolderPath);

        // Loop through each file
        foreach (string filePath in filePaths)
        {
            // Load the image as a texture
            Texture2D texture = LoadTextureFromFile(filePath);

            // Resize the texture to a square size
            Texture2D resizedTexture = ResizeTexture(texture, squareSize);

            // Save the resized texture as a PNG file in the output folder
            SaveTextureToFile(resizedTexture, outputFolderPath + "/" + Path.GetFileNameWithoutExtension(filePath) + ".png");
        }
    }

    // Load a texture from a file
    Texture2D LoadTextureFromFile(string filePath)
    {
        byte[] fileData = File.ReadAllBytes(filePath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(fileData);
        return texture;
    }

    // Resize a texture to a square size with centered crop
    Texture2D ResizeTexture(Texture2D texture, int size)
    {
        int width = texture.width;
        int height = texture.height;
        int cropSize = Mathf.Min(width, height);
        int offsetX = (width - cropSize) / 2;
        int offsetY = (height - cropSize) / 2;

        Color[] pixels = texture.GetPixels(offsetX, offsetY, cropSize, cropSize);
        Texture2D resizedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        resizedTexture.SetPixels(pixels);
        resizedTexture.Apply();
        return resizedTexture;
    }

    // Save a texture to a file as a PNG
    void SaveTextureToFile(Texture2D texture, string filePath)
    {
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(filePath, bytes);
    }
}
