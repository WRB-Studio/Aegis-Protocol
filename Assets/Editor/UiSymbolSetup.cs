using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class UiSymbolSetup
{
    const string Request = "Temp/UiSymbolSetup.request";
    const string Result = "Temp/UiSymbolSetup.result";
    const string Root = "Assets/GameContent/Images/UI/";

    [InitializeOnLoadMethod]
    static void CheckRequest()
    {
        if (File.Exists(Request)) EditorApplication.delayCall += ProcessRequest;
    }

    static void ProcessRequest()
    {
        if (!File.Exists(Request)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            File.WriteAllText(Result, "Waiting for Edit Mode.");
            EditorApplication.delayCall += ProcessRequest;
            return;
        }
        File.Delete(Request);
        try
        {
            Apply();
            File.WriteAllText(Result, "OK: 24 symbols exported; 8 module prefabs and 16 upgrade prefabs linked.");
        }
        catch (Exception exception)
        {
            File.WriteAllText(Result, exception.ToString());
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/Aegis/Apply UI symbol sprites")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Export symbols in Edit Mode.");
        EnsureFolder(Root + "ModuleS");
        EnsureFolder("Assets/GameContent/Prefabs/UI/Modules");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (var name in Enum.GetNames(typeof(UpgradeAttribute.eUpgradeName)).Where(name => name != "None"))
            ConfigureSprite(Root + "UpgradeS/" + name + ".png");
        foreach (var name in Enum.GetNames(typeof(StationModule.eModuleType)).Where(name => name != "None"))
            ConfigureSprite(Root + "ModuleS/" + name + ".png");
        SetTemplateSymbol("Assets/GameContent/Prefabs/UI/btnUpgrade.prefab", Root + "UpgradeS/FireRate.png");
        SetTemplateSymbol("Assets/GameContent/Prefabs/UI/btnModule.prefab", Root + "ModuleS/Core.png");

        var scene = SceneManager.GetSceneByPath("Assets/Scenes/MainScene.unity");
        bool openedForSetup = !scene.isLoaded;
        if (openedForSetup) scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Additive);
        try
        {
            var modules = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ModulesUI>(true)).Single();
            var serialized = new SerializedObject(modules);
            foreach (var name in Enum.GetNames(typeof(StationModule.eModuleType)).Where(name => name != "None"))
            {
                var property = serialized.FindProperty("btn" + name);
                var button = (Button)property.objectReferenceValue;
                var image = button.transform.Find("ImgSymbol").GetComponent<Image>();
                image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "ModuleS/" + name + ".png");
                image.preserveAspect = true;
                if (!image.sprite) throw new InvalidOperationException("Missing module symbol: " + name);
                PrefabUtility.RecordPrefabInstancePropertyModifications(image);
                string prefabPath = "Assets/GameContent/Prefabs/UI/Modules/" + name + ".prefab";
                PrefabUtility.SaveAsPrefabAssetAndConnect(button.gameObject, prefabPath, InteractionMode.AutomatedAction);
                property.objectReferenceValue = button;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            foreach (var view in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<UpgradeButtonView>(true)))
            {
                string expected = Root + "UpgradeS/" + view.upgradeName + ".png";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(expected);
                if (!sprite || view.symbol.sprite != sprite)
                    throw new InvalidOperationException("Upgrade prefab does not reference its individual sprite: " + view.name);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (openedForSetup) EditorSceneManager.CloseScene(scene, true);
        }
    }

    static void ConfigureSprite(string destination)
    {
        if (!File.Exists(destination)) throw new InvalidOperationException("Export the SVG sources first: " + destination);
        AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(destination);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 512;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        var textureSettings = new TextureImporterSettings();
        importer.ReadTextureSettings(textureSettings);
        textureSettings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(textureSettings);
        importer.SaveAndReimport();
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static void SetTemplateSymbol(string path, string spritePath)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            root.transform.Find("ImgSymbol").GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
