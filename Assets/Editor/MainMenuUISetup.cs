using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

// One-time wiring of the UI Toolkit main menu into the MainMenu scene.
public static class MainMenuUISetup
{
    private const string Folder = "Assets/UI/MainMenu";
    private const string PanelSettingsPath = Folder + "/MainMenuPanelSettings.asset";

    [MenuItem("Michi-Mechanika/Setup Main Menu UI")]
    private static void Setup()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "MainMenu")
        {
            EditorUtility.DisplayDialog("Setup Main Menu UI", "Open the MainMenu scene first.", "OK");
            return;
        }

        var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
        if (panelSettings == null)
        {
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(Folder + "/MainMenuTheme.tss");
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.screenMatchMode = PanelScreenMatchMode.Shrink;
            AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
        }

        // Draw under the scene's uGUI Canvas (sort order 0) so FadeImage and LoadingScreen cover the menu.
        panelSettings.sortingOrder = -1;
        EditorUtility.SetDirty(panelSettings);

        var menu = GameObject.Find("MainMenuUI");

        // The old uGUI menu carried a MainMenuController; on load Unity also gave it a UIDocument
        // (RequireComponent). Drop both from anything that isn't the new menu object.
        foreach (var legacy in Object.FindObjectsByType<MainMenuController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (menu != null && legacy.gameObject == menu) continue;
            var legacyDocument = legacy.GetComponent<UIDocument>();
            Undo.DestroyObjectImmediate(legacy);
            if (legacyDocument != null)
                Undo.DestroyObjectImmediate(legacyDocument);
        }

        if (menu == null)
        {
            menu = new GameObject("MainMenuUI");
            Undo.RegisterCreatedObjectUndo(menu, "Create Main Menu UI");
        }

        // No `??` here: a missing component is a fake-null Unity object in the Editor.
        var document = menu.GetComponent<UIDocument>();
        if (document == null)
            document = Undo.AddComponent<UIDocument>(menu);
        Undo.RecordObject(document, "Configure Main Menu UI");
        document.panelSettings = panelSettings;
        document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Folder + "/MainMenu.uxml");
        if (menu.GetComponent<MainMenuController>() == null)
            Undo.AddComponent<MainMenuController>(menu);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = menu;
        Debug.Log("Main menu UI set up in the MainMenu scene.");
    }
}
