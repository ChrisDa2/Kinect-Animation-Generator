using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KinectMapDisplay : MonoBehaviour
{
    public RawImage colorImage;
    public RawImage depthImage;
    public bool flipColorVertically = true;
    public bool flipDepthVertically = true;

    private KinectManager manager;
    private Texture2D flippedColorTex;
    private Texture2D flippedDepthTex;

    void Start()
    {
        manager = KinectManager.Instance;
        if (manager == null)
            Debug.LogError("KinectManager not found!");

        colorImage.color = Color.white;
        depthImage.color = Color.white;
    }

    void Update()
    {
        if (manager == null || !manager.IsInitialized())
            return;

        // Update color map texture (flipped)
        if (colorImage != null && manager.ComputeColorMap)
        {
            Texture2D original = manager.GetUsersClrTex();
            if (original != null)
            {
                if (flipColorVertically)
                    flippedColorTex = FlipTextureVertically(original);
                else
                    flippedColorTex = original;

                colorImage.texture = flippedColorTex;
            }
        }

        // Update depth/user map texture (flipped)
        if (depthImage != null && manager.ComputeUserMap)
        {
            Texture2D original = manager.GetUsersLblTex();
            if (original != null)
            {
                if (flipDepthVertically)
                    flippedDepthTex = FlipTextureVertically(original);
                else
                    flippedDepthTex = original;

                depthImage.texture = flippedDepthTex;
            }
        }
    }

    Texture2D FlipTextureVertically(Texture2D original)
    {
        // Create a new texture with same dimensions
        Texture2D flipped = new Texture2D(original.width, original.height);
        // Get pixels from original
        Color[] originalPixels = original.GetPixels();
        Color[] flippedPixels = new Color[originalPixels.Length];

        int width = original.width;
        int height = original.height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Flip vertically: new row = height - 1 - y
                int srcIndex = y * width + x;
                int dstIndex = (height - 1 - y) * width + x;
                flippedPixels[dstIndex] = originalPixels[srcIndex];
            }
        }

        flipped.SetPixels(flippedPixels);
        flipped.Apply();
        return flipped;
    }

    void OnDestroy()
    {
        // Clean up temporary textures to avoid memory leaks
        if (flippedColorTex != null && flippedColorTex != manager.GetUsersClrTex())
            Destroy(flippedColorTex);
        if (flippedDepthTex != null && flippedDepthTex != manager.GetUsersLblTex())
            Destroy(flippedDepthTex);
    }
}