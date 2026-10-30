using UnityEditor;
using UnityEngine;

/// <summary>
/// Sets the import settings for every image in Resources/Art automatically:
/// sprite, 100 pixels per unit, no blurry mipmaps, no compression.
/// Buttons and panels also get a 9-slice border, so they stretch nicely.
/// If you add images before this script existed, right-click Resources/Art
/// and choose Reimport.
/// </summary>
public class ArtImportSettings : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        string path = assetPath.Replace('\\', '/');
        if (!path.Contains("/Resources/Art/")) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        string file = System.IO.Path.GetFileNameWithoutExtension(path);
        if (file.StartsWith("panel") || file.StartsWith("button"))
        {
            int width, height;
            importer.GetSourceTextureWidthAndHeight(out width, out height);
            float border = Mathf.Clamp(Mathf.Min(width, height) / 4, 2, 32);
            importer.spriteBorder = new Vector4(border, border, border, border);
        }
    }
}
