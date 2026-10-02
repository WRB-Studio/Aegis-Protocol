using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class UpgradeUiSetup
{
    const string RequestPath = "Temp/UpgradeUiSetup.request";
    const string ResultPath = "Temp/UpgradeUiSetup.result";
    const string IconFolder = "Assets/GameContent/Images/UI/UpgradeS";
    const string PrefabFolder = "Assets/GameContent/Prefabs/UI/Upgrades";
    const string BasePrefab = PrefabFolder + "/UpgradeButton.prefab";

    [InitializeOnLoadMethod]
    static void CheckRequest()
    {
        if (File.Exists(RequestPath)) EditorApplication.delayCall += ProcessRequest;
    }

    static void ProcessRequest()
    {
        if (!File.Exists(RequestPath)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            File.WriteAllText(ResultPath, "Waiting for Edit Mode.");
            EditorApplication.delayCall += ProcessRequest;
            return;
        }
        File.Delete(RequestPath);
        try
        {
            Prepare();
            File.WriteAllText(ResultPath, "OK: 16 prefab buttons and individual sprites prepared and validated.");
        }
        catch (Exception exception)
        {
            File.WriteAllText(ResultPath, exception.ToString());
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/Aegis/Rebuild prepared upgrade UI")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Prepare the upgrade UI in Edit Mode.");

        EnsureFolder(IconFolder);
        EnsureFolder(PrefabFolder);
        EnsureFolder("Assets/GameContent/Images/UI/UpgradeSources");
        MoveIfPresent("Assets/Resources/Images/CommandUpgrades/RotationSpeed.png", IconFolder + "/RotationSpeed.png");
        MoveIfPresent("Assets/Resources/Images/CommandUpgrades/TargetPriority.png", IconFolder + "/TargetPriority.png");
        MoveIfPresent("Assets/Resources/Images/CommandUpgrades/RotationSpeed.svg",
            "Assets/GameContent/Images/UI/UpgradeSources/RotationSpeed.svg");
        ExportAtlasSymbols();
        AssetDatabase.Refresh();

        var scene = SceneManager.GetSceneByPath("Assets/Scenes/MainScene.unity");
        bool openedForSetup = !scene.isLoaded;
        if (openedForSetup) scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Additive);
        try
        {
            var objects = scene.GetRootGameObjects();
            var ui = objects.SelectMany(root => root.GetComponentsInChildren<UpgradeUI>(true)).Single();
            var serialized = new SerializedObject(ui);
            var panel = ((GameObject)serialized.FindProperty("panel").objectReferenceValue).GetComponent<RectTransform>();
            var canvas = panel.GetComponentInParent<Canvas>().rootCanvas;
            panel.SetParent(canvas.transform, false);
            panel.name = "UpgradePanel";
            panel.anchorMin = panel.anchorMax = new Vector2(0f, 0.5f);
            panel.pivot = new Vector2(0f, 0.5f);
            panel.anchoredPosition = new Vector2(8f, 0f);
            panel.sizeDelta = new Vector2(116f, 190f);
            foreach (var layout in panel.GetComponents<HorizontalLayoutGroup>()) Object.DestroyImmediate(layout);
            var vertical = panel.GetComponent<VerticalLayoutGroup>() ?? panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.childAlignment = TextAnchor.MiddleCenter;
            vertical.spacing = 10f;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = false;
            vertical.childForceExpandHeight = false;
            var fitter = panel.GetComponent<ContentSizeFitter>() ?? panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            while (panel.childCount > 0) Object.DestroyImmediate(panel.GetChild(0).gameObject);

            CreateBasePrefab();
            var sets = AssetDatabase.FindAssets("t:UpgradeSet", new[] { "Assets/GameContent/Prefabs/UpgradeSets" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<UpgradeSet>(AssetDatabase.GUIDToAssetPath(guid)))
                .OrderBy(set => set.moduleType).ToArray();
            var core = objects.SelectMany(root => root.GetComponentsInChildren<StationModule>(true))
                .Single(module => module.moduleType == StationModule.eModuleType.Core);
            foreach (var set in sets)
            foreach (var upgrade in set.upgradeAttributes)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefab));
                instance.name = set.moduleType + "_" + upgrade.upgradeName;
                var view = instance.GetComponent<UpgradeButtonView>();
                view.moduleType = set.moduleType;
                view.upgradeName = upgrade.upgradeName;
                view.symbol.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconFolder + "/" + upgrade.upgradeName + ".png");
                view.priceText.text = Utils.FormatNumber(Mathf.RoundToInt(upgrade.baseCost)) + " M";
                view.valueText.text = PreviewValue(upgrade, core.maxHP);
                if (upgrade.upgradeName == UpgradeAttribute.eUpgradeName.TargetPriority)
                    view.valueText.fontSizeMax = view.valueText.fontSize = 22f;
                if (set.moduleType == StationModule.eModuleType.CommandUnit)
                    view.button.image.color = new Color32(57, 64, 67, 255);
                PrefabUtility.RecordPrefabInstancePropertyModifications(view);
                PrefabUtility.RecordPrefabInstancePropertyModifications(view.symbol);
                PrefabUtility.RecordPrefabInstancePropertyModifications(view.priceText);
                PrefabUtility.RecordPrefabInstancePropertyModifications(view.valueText);
                PrefabUtility.RecordPrefabInstancePropertyModifications(view.button.image);
                string path = PrefabFolder + "/" + instance.name + ".prefab";
                var variant = PrefabUtility.SaveAsPrefabAsset(instance, path);
                Object.DestroyImmediate(instance);
                var placed = (GameObject)PrefabUtility.InstantiatePrefab(variant, panel);
                placed.name = variant.name;
            }
            foreach (var module in objects.SelectMany(root => root.GetComponentsInChildren<StationModule>(true)))
                if (module.moduleType == StationModule.eModuleType.CommandUnit)
                {
                    module.description = "Rotate faster. Choose targets.";
                    EditorUtility.SetDirty(module);
                }
            panel.gameObject.SetActive(true);
            serialized.FindProperty("contentContainer").objectReferenceValue = panel;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            Validate(panel);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Prepared all 16 upgrade buttons as scene prefab instances.");
        }
        finally
        {
            if (openedForSetup) EditorSceneManager.CloseScene(scene, true);
        }
    }

    static void CreateBasePrefab()
    {
        var wrapper = new GameObject("UpgradeButton", typeof(RectTransform), typeof(LayoutElement), typeof(UpgradeButtonView));
        wrapper.layer = 5;
        ((RectTransform)wrapper.transform).sizeDelta = new Vector2(116f, 190f);
        var slot = wrapper.GetComponent<LayoutElement>();
        slot.minWidth = slot.preferredWidth = 116f;
        slot.minHeight = slot.preferredHeight = 190f;
        var button = (GameObject)PrefabUtility.InstantiatePrefab(
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameContent/Prefabs/UI/btnUpgrade.prefab"), wrapper.transform);
        button.name = "Button";
        var rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 8f);
        var view = wrapper.GetComponent<UpgradeButtonView>();
        view.button = button.GetComponent<UpgradeButton>();
        view.symbol = button.transform.Find("ImgSymbol").GetComponent<Image>();
        view.marker = button.transform.Find("SelectionMarker").GetComponent<Image>();
        view.valueText = button.transform.Find("Info/txtInfo").GetComponent<TextMeshProUGUI>();
        view.priceText = button.transform.Find("Cost/txtCost").GetComponent<TextMeshProUGUI>();
        view.symbol.preserveAspect = true;
        view.symbol.raycastTarget = false;
        view.marker.raycastTarget = false;
        view.marker.color = Color.white;
        view.marker.enabled = true;
        var values = (RectTransform)view.valueText.transform.parent;
        values.anchoredPosition = new Vector2(0f, -76f);
        values.sizeDelta = new Vector2(108f, 46f);
        view.valueText.rectTransform.sizeDelta = values.sizeDelta;
        view.valueText.enableAutoSizing = true;
        view.valueText.fontSizeMin = 18f;
        view.valueText.fontSizeMax = view.valueText.fontSize = 28f;
        view.valueText.fontStyle = FontStyles.Bold;
        view.valueText.color = Color.white;
        view.valueText.raycastTarget = false;
        view.priceText.enableAutoSizing = true;
        view.priceText.fontSizeMin = 18f;
        view.priceText.fontSizeMax = view.priceText.fontSize = 22f;
        view.priceText.fontStyle = FontStyles.Normal;
        view.priceText.color = new Color32(230, 239, 240, 255);
        view.priceText.raycastTarget = false;
        view.selectionGlow = view.marker.gameObject.AddComponent<Outline>();
        view.selectionGlow.effectDistance = new Vector2(2f, 2f);
        view.selectionGlow.effectColor = new Color(148f / 255f, 236f / 255f, 244f / 255f, 0.16f);
        view.selectionGlow.enabled = false;
        foreach (var component in button.GetComponentsInChildren<Component>(true))
            if (component is RectTransform || component is Graphic || component is Behaviour)
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        PrefabUtility.SaveAsPrefabAsset(wrapper, BasePrefab);
        Object.DestroyImmediate(wrapper);
    }

    static string PreviewValue(UpgradeAttribute upgrade, int hp)
    {
        string value = upgrade.baseValue.ToString("0.##");
        switch (upgrade.upgradeName)
        {
            case UpgradeAttribute.eUpgradeName.RotationSpeed: return (upgrade.baseValue * 100f).ToString("0.#") + "\n<size=60%>deg/s</size>";
            case UpgradeAttribute.eUpgradeName.TargetPriority: return "Locked";
            case UpgradeAttribute.eUpgradeName.StructuralIntegrity: return hp + " HP";
            case UpgradeAttribute.eUpgradeName.AutoCollecting: return upgrade.baseValue > 0 ? "On" : "Off";
            case UpgradeAttribute.eUpgradeName.CollectingEfficiency: return "x" + value;
            case UpgradeAttribute.eUpgradeName.FireRate: return value + " rps";
            case UpgradeAttribute.eUpgradeName.FireRange: return value + " m";
            case UpgradeAttribute.eUpgradeName.Damage:
            case UpgradeAttribute.eUpgradeName.DroneDamage: return value + " dmg";
            case UpgradeAttribute.eUpgradeName.ShieldCapacity:
            case UpgradeAttribute.eUpgradeName.DroneHP: return value + " HP";
            case UpgradeAttribute.eUpgradeName.RechargeTime:
            case UpgradeAttribute.eUpgradeName.DroneBuildTime: return value + " s";
            case UpgradeAttribute.eUpgradeName.DeflectionChance: return value + " %";
            case UpgradeAttribute.eUpgradeName.DroneCount: return value + " pcs";
            default: return value;
        }
    }

    static void ExportAtlasSymbols()
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/Images/UpgradeSymbols.png").OfType<Sprite>();
        foreach (var sprite in sprites)
        {
            if (!Enum.TryParse(sprite.name, true, out UpgradeAttribute.eUpgradeName name) ||
                name == UpgradeAttribute.eUpgradeName.None) continue;
            string path = IconFolder + "/" + name + ".png";
            if (File.Exists(path)) continue;
            var previous = RenderTexture.active;
            var buffer = RenderTexture.GetTemporary(sprite.texture.width, sprite.texture.height, 0, RenderTextureFormat.ARGB32);
            var image = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height, TextureFormat.RGBA32, false);
            try
            {
                Graphics.Blit(sprite.texture, buffer);
                RenderTexture.active = buffer;
                image.ReadPixels(sprite.rect, 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(buffer);
                Object.DestroyImmediate(image);
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }
        foreach (var path in Directory.GetFiles(IconFolder, "*.png"))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }

    static void Validate(RectTransform panel)
    {
        var views = panel.GetComponentsInChildren<UpgradeButtonView>(true);
        if (views.Length != 16 || views.Select(view => view.upgradeName).Distinct().Count() != 16)
            throw new InvalidOperationException("Expected one prepared button for each of the 16 upgrades.");
        foreach (var view in views)
        {
            if (!view.symbol.sprite || !view.button || !view.priceText || !view.valueText || !view.selectionGlow)
                throw new InvalidOperationException("Incomplete prepared button: " + view.name);
            if (!(view.button is UpgradeButton) || !PrefabUtility.IsPartOfPrefabInstance(view))
                throw new InvalidOperationException("Button must retain its prefab and press handling: " + view.name);
            if (((RectTransform)view.transform).rect.height < 190f)
                throw new InvalidOperationException("Vertical layout collapsed the button: " + view.name);
        }
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static void MoveIfPresent(string from, string to)
    {
        if (!File.Exists(from)) return;
        string error = AssetDatabase.MoveAsset(from, to);
        if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
    }
}
