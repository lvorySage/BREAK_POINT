using UnityEditor;
using UnityEngine;

namespace BreachPoint.EditorTools
{
    /// <summary>
    /// Import rules for Assets/_BreachPoint/Resources/Art so dropped-in assets need no manual setup:
    ///   *_nor.*        → normal map
    ///   Art/Sky/*.hdr  → cubemap (used for reflections)
    ///   Art/Brand/*    → uncompressed, transparent logos
    ///   Art/Models/*   → no imported cameras/lights, materials kept inside the model
    /// </summary>
    public sealed class ArtImport : AssetPostprocessor
    {
        const string Root = "Assets/_BreachPoint/Resources/Art/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            var ti = (TextureImporter)assetImporter;
            string file = System.IO.Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
            if (file.EndsWith("_nor"))
            {
                ti.textureType = TextureImporterType.NormalMap;
            }
            else if (assetPath.StartsWith(Root + "Brand/"))
            {
                // Logos: keep them crisp and transparent.
                ti.alphaIsTransparency = true;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.mipmapEnabled = true;
                ti.anisoLevel = 4;
            }
            else if (assetPath.StartsWith(Root + "Sky/"))
            {
                ti.textureShape = TextureImporterShape.TextureCube;
                ti.generateCubemap = TextureImporterGenerateCubemap.AutoCubemap;
                ti.sRGBTexture = false;
                ti.mipmapEnabled = true;
            }
            ti.maxTextureSize = 2048;
        }

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Root)) return;
            var mi = (ModelImporter)assetImporter;
            mi.importCameras = false;
            mi.importLights = false;
            mi.addCollider = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        }
    }
}
