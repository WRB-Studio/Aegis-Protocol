using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ShootingStarSetup
{
    [InitializeOnLoadMethod]
    static void CheckRequest()
    {
        if (File.Exists("Temp/ShootingStarSetup.request")) EditorApplication.delayCall += Run;
    }

    [MenuItem("Tools/Aegis/Prepare shooting star prefab")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            EditorApplication.delayCall += Run;
            return;
        }
        File.Delete("Temp/ShootingStarSetup.request");
        try
        {
            const string imagePath = "Assets/GameContent/Images/Effects/ShootingStar.png";
            AssetDatabase.ImportAsset(imagePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(imagePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 128f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            var root = new GameObject("ShootingStar");
            ShootingStar prefab;
            try
            {
                var head = root.AddComponent<SpriteRenderer>();
                head.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
                head.color = new Color(0.8f, 1f, 1f);
                head.sortingOrder = 35;
                var tail = root.AddComponent<LineRenderer>();
                tail.sharedMaterial = head.sharedMaterial;
                tail.sortingOrder = 34;
                tail.useWorldSpace = false;
                tail.positionCount = 3;
                tail.SetPositions(new[] { Vector3.zero, new Vector3(-0.3f, 0f, 0f), new Vector3(-1.25f, 0f, 0f) });
                tail.startWidth = 0.07f;
                tail.endWidth = 0f;
                tail.startColor = new Color(0.4f, 0.9f, 1f, 0.7f);
                tail.endColor = new Color(0.2f, 0.65f, 0.8f, 0f);
                var star = root.AddComponent<ShootingStar>();
                var serializedStar = new SerializedObject(star);
                serializedStar.FindProperty("head").objectReferenceValue = head;
                serializedStar.FindProperty("tail").objectReferenceValue = tail;
                serializedStar.ApplyModifiedPropertiesWithoutUndo();
                prefab = PrefabUtility.SaveAsPrefabAsset(root,
                    "Assets/GameContent/Prefabs/GameObjects/ShootingStar.prefab").GetComponent<ShootingStar>();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }

            var scene = SceneManager.GetSceneByPath("Assets/Scenes/MainScene.unity");
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Additive);
            try
            {
                var resources = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<ResourceManager>(true)).Single();
                var serialized = new SerializedObject(resources);
                serialized.FindProperty("shootingStarPrefab").objectReferenceValue = prefab;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            File.WriteAllText("Temp/ShootingStarSetup.result", "OK: shooting star prefab prepared and linked.");
        }
        catch (Exception exception)
        {
            File.WriteAllText("Temp/ShootingStarSetup.result", exception.ToString());
            Debug.LogException(exception);
        }
    }
}
