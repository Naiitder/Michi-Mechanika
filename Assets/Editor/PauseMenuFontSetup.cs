using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// Pone la fuente del menú principal (Rye) en los textos del menú de pausa.
// "Rye-Regular SDF" es un font asset de UI Toolkit y TextMeshPro no puede usarlo, así que se
// genera uno de TextMeshPro a partir del mismo .ttf y se asigna a los textos de PauseMenu en Canvas.prefab.
// Se ejecuta solo la primera vez (mientras no exista el font asset); también está en el menú Michi-Mechanika.
public static class PauseMenuFontSetup
{
    private const string SourceFontPath = "Assets/Fonts/Rye/Rye-Regular.ttf";
    private const string FontAssetPath = "Assets/Fonts/Rye/Rye-Regular TMP.asset";
    private const string PrefabPath = "Assets/Prefabs/UI/Canvas.prefab";
    private const string SessionKey = "PauseMenuFontSetup.ran";
    private const string Characters =
        " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~·ÁÉÍÓÚÑÜáéíóúñü¡¿";

    [InitializeOnLoadMethod]
    private static void RunOnce()
    {
        if (SessionState.GetBool(SessionKey, false)) return;
        SessionState.SetBool(SessionKey, true);
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(FontAssetPath)) Apply();
        };
    }

    [MenuItem("Michi-Mechanika/Apply Main Menu Font To Pause Menu")]
    private static void Apply()
    {
        TMP_FontAsset fontAsset = LoadOrCreateFontAsset();
        if (fontAsset == null) return;

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform pauseMenu = root.transform.Find("PauseMenu");
            if (pauseMenu == null)
            {
                Debug.LogError($"PauseMenuFontSetup: no PauseMenu under {PrefabPath}.");
                return;
            }

            int count = 0;
            foreach (TMP_Text text in pauseMenu.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = fontAsset;
                count++;
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log($"PauseMenuFontSetup: Rye applied to {count} pause menu texts.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static TMP_FontAsset LoadOrCreateFontAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (existing != null) return existing;

        var font = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (font == null)
        {
            Debug.LogError($"PauseMenuFontSetup: font not found at {SourceFontPath}.");
            return null;
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
        if (fontAsset == null)
        {
            Debug.LogError("PauseMenuFontSetup: could not create the TextMeshPro font asset.");
            return null;
        }

        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

        // The atlas texture and the material live inside the font asset, like any TMP font.
        fontAsset.material.name = "Rye-Regular TMP Material";
        fontAsset.atlasTexture.name = "Rye-Regular TMP Atlas";
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);

        // Bake the usual characters now so the atlas is ready in builds.
        fontAsset.TryAddCharacters(Characters);

        EditorUtility.SetDirty(fontAsset.atlasTexture);
        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(FontAssetPath);
        return fontAsset;
    }
}
