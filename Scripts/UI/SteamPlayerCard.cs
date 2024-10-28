using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using UnityEngine.UI;
using TMPro;

public class SteamPlayerCard : MonoBehaviour
{
    public RawImage hostImage;
    public TMP_Text hostName;

    public void Initialise(CSteamID steamId)
    {
        if (SteamManager.Initialized)
        {
            var name = SteamFriends.GetFriendPersonaName(steamId);
            hostName.text = name;
            hostImage.texture = GetAvatarImageTexture(steamId);
            hostImage.uvRect = new Rect(0, 1, 1, 1);
        }
    }

    private Texture2D GetAvatarImageTexture(CSteamID steamId)
    {
        Texture2D retVal = null;

        // Get image id
        int imgId = SteamFriends.GetMediumFriendAvatar(steamId);
        uint width, height;
        if (SteamUtils.GetImageSize(imgId, out width, out height))
        {
            // Image buffer
            int bufferSize = (int)width * (int)height * 4 * sizeof(char);
            var imageBytes = new byte[bufferSize];

            // Get image data
            if (SteamUtils.GetImageRGBA(imgId, imageBytes, bufferSize))
            {
                retVal = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false, true);
                retVal.LoadRawTextureData(imageBytes);
                retVal.wrapModeV = TextureWrapMode.Mirror;
                retVal.Apply();
            }
        }

        return retVal;
    }
}
