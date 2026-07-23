using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FirstPassV2Builder
{
    private const string SourceScenePath = "Assets/Scenes/FirstPass_1.unity";
    private const string TargetScenePath = "Assets/Scenes/FirstPass_V2.unity";
    private const string PerformanceProfilePath = "Assets/Resources/Config/GameplayPerformanceProfile.asset";
    private const string PlayerControllerPath = "Assets/Animation/PlayerAni/Player.controller";
    private const string OptimizedSpriteFolder = "Assets/File/V2/OptimizedSprites";
    private const string OptimizedClipFolder = "Assets/File/V2/AnimationClips";
    private const string OptimizedControllerFolder = "Assets/File/V2/Controllers";
    private const float AttackRecoveryTime = 0.2f;
    private const float AttackEndTime = 0.4f;

    private static readonly string[] AttackClipPaths =
    {
        "Assets/Animation/PlayerAni/Attack/PlayerAttack.anim",
        "Assets/Animation/PlayerAni/Attack/PlayerAttackBack.anim",
        "Assets/Animation/PlayerAni/Attack/PlayerAttackLeft.anim",
        "Assets/Animation/PlayerAni/Attack/PlayerAttackRight.anim"
    };

    private static readonly Dictionary<Texture2D, Texture2D> ReadableTextureCache =
        new Dictionary<Texture2D, Texture2D>();

    [MenuItem("Tools/Architecture/Build FirstPass V2")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath) != null)
        {
            Debug.LogError($"V2 场景已存在，构建已中止：{TargetScenePath}");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScenePath) == null)
        {
            Debug.LogError($"未找到第一关源场景：{SourceScenePath}");
            return;
        }

        EnsurePerformanceProfile();
        RetimeAttackClips();
        ConfigureAttackTransitions();

        if (!AssetDatabase.CopyAsset(SourceScenePath, TargetScenePath))
        {
            Debug.LogError($"复制 V2 场景失败：{TargetScenePath}");
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        GameObject optimizationRoot = new GameObject("V2_Optimization");
        optimizationRoot.AddComponent<V2SceneProfile>();
        GameplayStressTestController stressController = optimizationRoot.AddComponent<GameplayStressTestController>();
        stressController.ApplyProfileDefaults();

        int optimizedSpriteCount = OptimizeSceneSingleSprites(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, TargetScenePath);
        AddV2ToBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath);
        Debug.Log($"FirstPass_V2 构建完成，已替换 {optimizedSpriteCount} 个裁边后的独立 Sprite 副本。");
    }

    [MenuItem("Tools/Architecture/Upgrade FirstPass V2 Art")]
    public static void UpgradeV2ArtAssets()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath) == null)
        {
            Debug.LogError($"未找到 V2 场景：{TargetScenePath}");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        int optimizedSpriteCount = OptimizeSceneSingleSprites(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, TargetScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"FirstPass_V2 动画美术升级完成，替换引用数：{optimizedSpriteCount}。");
    }

    private static void EnsurePerformanceProfile()
    {
        if (AssetDatabase.LoadAssetAtPath<GameplayPerformanceProfile>(PerformanceProfilePath) != null)
        {
            return;
        }

        EnsureAssetFolder("Assets/Resources/Config");
        GameplayPerformanceProfile profile = ScriptableObject.CreateInstance<GameplayPerformanceProfile>();
        AssetDatabase.CreateAsset(profile, PerformanceProfilePath);
        EditorUtility.SetDirty(profile);
    }

    private static void RetimeAttackClips()
    {
        for (int i = 0; i < AttackClipPaths.Length; i++)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AttackClipPaths[i]);
            if (clip == null)
            {
                Debug.LogWarning($"未找到攻击动画：{AttackClipPaths[i]}");
                continue;
            }

            float sourceLength = Mathf.Max(1f / Mathf.Max(1f, clip.frameRate), clip.length);
            float timeScale = AttackEndTime / sourceLength;

            EditorCurveBinding[] floatBindings = AnimationUtility.GetCurveBindings(clip);
            for (int bindingIndex = 0; bindingIndex < floatBindings.Length; bindingIndex++)
            {
                EditorCurveBinding binding = floatBindings[bindingIndex];
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null)
                {
                    continue;
                }

                Keyframe[] keys = curve.keys;
                for (int keyIndex = 0; keyIndex < keys.Length; keyIndex++)
                {
                    keys[keyIndex].time = Mathf.Min(AttackEndTime, keys[keyIndex].time * timeScale);
                }

                curve.keys = keys;
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }

            EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            for (int bindingIndex = 0; bindingIndex < objectBindings.Length; bindingIndex++)
            {
                EditorCurveBinding binding = objectBindings[bindingIndex];
                ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                for (int keyIndex = 0; keyIndex < keys.Length; keyIndex++)
                {
                    keys[keyIndex].time = Mathf.Min(AttackEndTime, keys[keyIndex].time * timeScale);
                }

                AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            }

            List<AnimationEvent> events = new List<AnimationEvent>();
            AnimationEvent[] sourceEvents = AnimationUtility.GetAnimationEvents(clip);
            for (int eventIndex = 0; eventIndex < sourceEvents.Length; eventIndex++)
            {
                AnimationEvent animationEvent = sourceEvents[eventIndex];
                if (string.Equals(animationEvent.functionName, "OnAttackEnd", StringComparison.Ordinal) ||
                    string.Equals(animationEvent.functionName, "OnAttackRecovery", StringComparison.Ordinal))
                {
                    continue;
                }

                animationEvent.time = Mathf.Min(AttackEndTime, animationEvent.time * timeScale);
                events.Add(animationEvent);
            }

            events.Add(new AnimationEvent { functionName = "OnAttackRecovery", time = AttackRecoveryTime });
            events.Add(new AnimationEvent { functionName = "OnAttackEnd", time = AttackEndTime });
            AnimationUtility.SetAnimationEvents(clip, events.ToArray());
            EditorUtility.SetDirty(clip);
        }
    }

    private static void ConfigureAttackTransitions()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerControllerPath);
        if (controller == null)
        {
            Debug.LogWarning($"未找到玩家 Animator Controller：{PlayerControllerPath}");
            return;
        }

        for (int layerIndex = 0; layerIndex < controller.layers.Length; layerIndex++)
        {
            ConfigureStateMachineTransitions(controller.layers[layerIndex].stateMachine);
        }

        EditorUtility.SetDirty(controller);
    }

    private static void ConfigureStateMachineTransitions(AnimatorStateMachine stateMachine)
    {
        ChildAnimatorState[] states = stateMachine.states;
        for (int stateIndex = 0; stateIndex < states.Length; stateIndex++)
        {
            AnimatorState state = states[stateIndex].state;
            if (state == null || !string.Equals(state.name, "Attack", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            AnimatorStateTransition[] transitions = state.transitions;
            for (int transitionIndex = 0; transitionIndex < transitions.Length; transitionIndex++)
            {
                AnimatorStateTransition transition = transitions[transitionIndex];
                if (transition.destinationState == null ||
                    !string.Equals(transition.destinationState.name, "Move", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                transition.hasExitTime = false;
                transition.duration = 0.05f;
                transition.interruptionSource = TransitionInterruptionSource.SourceThenDestination;
                transition.orderedInterruption = true;
                EditorUtility.SetDirty(transition);
            }
        }

        ChildAnimatorStateMachine[] childStateMachines = stateMachine.stateMachines;
        for (int childIndex = 0; childIndex < childStateMachines.Length; childIndex++)
        {
            ConfigureStateMachineTransitions(childStateMachines[childIndex].stateMachine);
        }
    }

    private static int OptimizeSceneSingleSprites(Scene scene)
    {
        EnsureAssetFolder(OptimizedSpriteFolder);
        EnsureAssetFolder(OptimizedClipFolder);
        EnsureAssetFolder(OptimizedControllerFolder);
        Dictionary<Sprite, Sprite> optimizedSprites = new Dictionary<Sprite, Sprite>();
        int replacementCount = 0;

        try
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                SpriteRenderer[] renderers = roots[rootIndex].GetComponentsInChildren<SpriteRenderer>(true);
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    SpriteRenderer renderer = renderers[rendererIndex];
                    Sprite optimized = GetOrCreateOptimizedSprite(renderer.sprite, optimizedSprites);
                    if (optimized == null || optimized == renderer.sprite)
                    {
                        continue;
                    }

                    renderer.sprite = optimized;
                    EditorUtility.SetDirty(renderer);
                    replacementCount++;
                }

                Image[] images = roots[rootIndex].GetComponentsInChildren<Image>(true);
                for (int imageIndex = 0; imageIndex < images.Length; imageIndex++)
                {
                    Image image = images[imageIndex];
                    if (image.type != Image.Type.Simple)
                    {
                        continue;
                    }

                    Sprite optimized = GetOrCreateOptimizedSprite(image.sprite, optimizedSprites);
                    if (optimized == null || optimized == image.sprite)
                    {
                        continue;
                    }

                    image.sprite = optimized;
                    EditorUtility.SetDirty(image);
                    replacementCount++;
                }

                replacementCount += OptimizeAnimatorSprites(roots[rootIndex], optimizedSprites);
            }
        }
        finally
        {
            ClearReadableTextureCache();
        }

        return replacementCount;
    }

    private static int OptimizeAnimatorSprites(
        GameObject root,
        Dictionary<Sprite, Sprite> optimizedSprites)
    {
        Animator[] animators = root.GetComponentsInChildren<Animator>(true);
        Dictionary<RuntimeAnimatorController, RuntimeAnimatorController> controllerCache =
            new Dictionary<RuntimeAnimatorController, RuntimeAnimatorController>();
        int replacementCount = 0;

        for (int animatorIndex = 0; animatorIndex < animators.Length; animatorIndex++)
        {
            Animator animator = animators[animatorIndex];
            RuntimeAnimatorController sourceController = animator.runtimeAnimatorController;
            if (sourceController == null)
            {
                continue;
            }

            if (!controllerCache.TryGetValue(sourceController, out RuntimeAnimatorController optimizedController))
            {
                optimizedController = CreateOptimizedController(
                    sourceController,
                    optimizedSprites,
                    out int optimizedFrameCount);
                controllerCache[sourceController] = optimizedController != null
                    ? optimizedController
                    : sourceController;
                replacementCount += optimizedFrameCount;
            }

            if (optimizedController == null || optimizedController == sourceController)
            {
                continue;
            }

            animator.runtimeAnimatorController = optimizedController;
            EditorUtility.SetDirty(animator);
        }

        return replacementCount;
    }

    private static RuntimeAnimatorController CreateOptimizedController(
        RuntimeAnimatorController sourceController,
        Dictionary<Sprite, Sprite> optimizedSprites,
        out int replacementCount)
    {
        replacementCount = 0;
        AnimatorOverrideController overrideController = new AnimatorOverrideController(sourceController);
        List<KeyValuePair<AnimationClip, AnimationClip>> overrides =
            new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);

        bool hasOverrides = false;
        for (int overrideIndex = 0; overrideIndex < overrides.Count; overrideIndex++)
        {
            AnimationClip sourceClip = overrides[overrideIndex].Key;
            AnimationClip optimizedClip = CreateOptimizedAnimationClip(
                sourceClip,
                optimizedSprites,
                out int clipReplacementCount);
            if (optimizedClip == null)
            {
                continue;
            }

            overrides[overrideIndex] = new KeyValuePair<AnimationClip, AnimationClip>(sourceClip, optimizedClip);
            replacementCount += clipReplacementCount;
            hasOverrides = true;
        }

        if (!hasOverrides)
        {
            UnityEngine.Object.DestroyImmediate(overrideController);
            return null;
        }

        string sourcePath = AssetDatabase.GetAssetPath(sourceController);
        string guid = AssetDatabase.AssetPathToGUID(sourcePath);
        string outputPath = $"{OptimizedControllerFolder}/{SanitizeFileName(sourceController.name)}_{guid.Substring(0, 8)}.overrideController";
        AnimatorOverrideController existing = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(outputPath);
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(overrideController);
            return existing;
        }

        overrideController.ApplyOverrides(overrides);
        AssetDatabase.CreateAsset(overrideController, outputPath);
        EditorUtility.SetDirty(overrideController);
        return overrideController;
    }

    private static AnimationClip CreateOptimizedAnimationClip(
        AnimationClip sourceClip,
        Dictionary<Sprite, Sprite> optimizedSprites,
        out int replacementCount)
    {
        replacementCount = 0;
        if (sourceClip == null)
        {
            return null;
        }

        string sourcePath = AssetDatabase.GetAssetPath(sourceClip);
        string guid = AssetDatabase.AssetPathToGUID(sourcePath);
        string outputPath = $"{OptimizedClipFolder}/{SanitizeFileName(sourceClip.name)}_{guid.Substring(0, 8)}.anim";
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(outputPath);
        if (existing != null)
        {
            return existing;
        }

        AnimationClip optimizedClip = new AnimationClip();
        EditorUtility.CopySerialized(sourceClip, optimizedClip);
        EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(optimizedClip);
        bool changed = false;

        for (int bindingIndex = 0; bindingIndex < bindings.Length; bindingIndex++)
        {
            EditorCurveBinding binding = bindings[bindingIndex];
            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(optimizedClip, binding);
            bool curveChanged = false;
            for (int keyIndex = 0; keyIndex < keys.Length; keyIndex++)
            {
                Sprite sourceSprite = keys[keyIndex].value as Sprite;
                Sprite optimizedSprite = GetOrCreateOptimizedSprite(sourceSprite, optimizedSprites);
                if (optimizedSprite == null || optimizedSprite == sourceSprite)
                {
                    continue;
                }

                keys[keyIndex].value = optimizedSprite;
                replacementCount++;
                curveChanged = true;
                changed = true;
            }

            if (curveChanged)
            {
                AnimationUtility.SetObjectReferenceCurve(optimizedClip, binding, keys);
            }
        }

        if (!changed)
        {
            UnityEngine.Object.DestroyImmediate(optimizedClip);
            return null;
        }

        AssetDatabase.CreateAsset(optimizedClip, outputPath);
        EditorUtility.SetDirty(optimizedClip);
        return optimizedClip;
    }

    private static Sprite GetOrCreateOptimizedSprite(Sprite source, Dictionary<Sprite, Sprite> cache)
    {
        if (source == null)
        {
            return null;
        }

        if (cache.TryGetValue(source, out Sprite cached))
        {
            return cached;
        }

        Sprite optimized = TryCreateCroppedSprite(source);
        cache[source] = optimized != null ? optimized : source;
        return cache[source];
    }

    private static Sprite TryCreateCroppedSprite(Sprite source)
    {
        string sourcePath = AssetDatabase.GetAssetPath(source);
        TextureImporter sourceImporter = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
        if (sourceImporter == null)
        {
            return null;
        }

        Texture2D sourceTexture = source.texture;
        if (sourceTexture == null || sourceTexture.width < 64 || sourceTexture.height < 64)
        {
            return null;
        }

        Texture2D readable = ReadTextureCached(sourceTexture);
        if (readable == null)
        {
            return null;
        }

        Color32[] pixels = readable.GetPixels32();
        Rect spriteRect = source.rect;
        RectInt sourceBounds = new RectInt(
            Mathf.Clamp(Mathf.FloorToInt(spriteRect.x), 0, readable.width - 1),
            Mathf.Clamp(Mathf.FloorToInt(spriteRect.y), 0, readable.height - 1),
            Mathf.Clamp(Mathf.CeilToInt(spriteRect.width), 1, readable.width),
            Mathf.Clamp(Mathf.CeilToInt(spriteRect.height), 1, readable.height));
        sourceBounds.width = Mathf.Min(sourceBounds.width, readable.width - sourceBounds.x);
        sourceBounds.height = Mathf.Min(sourceBounds.height, readable.height - sourceBounds.y);

        if (!TryFindAlphaBounds(pixels, readable.width, sourceBounds, out RectInt alphaBounds))
        {
            return null;
        }

        alphaBounds = AddPadding(alphaBounds, sourceBounds, 2);
        Texture2D cropped = new Texture2D(alphaBounds.width, alphaBounds.height, TextureFormat.RGBA32, false);
        cropped.filterMode = FilterMode.Point;
        cropped.SetPixels(readable.GetPixels(alphaBounds.x, alphaBounds.y, alphaBounds.width, alphaBounds.height));
        cropped.Apply(false, false);

        string guid = AssetDatabase.AssetPathToGUID(sourcePath);
        string outputPath =
            $"{OptimizedSpriteFolder}/{SanitizeFileName(source.name)}_{guid.Substring(0, 8)}_" +
            $"{sourceBounds.x}_{sourceBounds.y}_{sourceBounds.width}_{sourceBounds.height}.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(outputPath);
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(cropped);
            return existing;
        }

        string absoluteOutputPath = Path.GetFullPath(outputPath);
        File.WriteAllBytes(absoluteOutputPath, cropped.EncodeToPNG());

        UnityEngine.Object.DestroyImmediate(cropped);

        AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter outputImporter = AssetImporter.GetAtPath(outputPath) as TextureImporter;
        if (outputImporter == null)
        {
            return null;
        }

        float sourcePivotX = source.rect.x + source.pivot.x;
        float sourcePivotY = source.rect.y + source.pivot.y;
        outputImporter.textureType = TextureImporterType.Sprite;
        outputImporter.spriteImportMode = SpriteImportMode.Single;
        outputImporter.mipmapEnabled = false;
        outputImporter.filterMode = FilterMode.Point;
        outputImporter.textureCompression = TextureImporterCompression.Uncompressed;
        outputImporter.npotScale = TextureImporterNPOTScale.None;
        outputImporter.spritePixelsPerUnit = source.pixelsPerUnit;
        TextureImporterSettings textureSettings = new TextureImporterSettings();
        outputImporter.ReadTextureSettings(textureSettings);
        textureSettings.spriteAlignment = (int)SpriteAlignment.Custom;
        textureSettings.spritePivot = new Vector2(
            Mathf.Clamp01((sourcePivotX - alphaBounds.x) / alphaBounds.width),
            Mathf.Clamp01((sourcePivotY - alphaBounds.y) / alphaBounds.height));
        outputImporter.SetTextureSettings(textureSettings);
        outputImporter.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(outputPath);
    }

    private static Texture2D ReadTextureCached(Texture2D source)
    {
        if (ReadableTextureCache.TryGetValue(source, out Texture2D cached))
        {
            return cached;
        }

        RenderTexture temporary = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.sRGB);
        RenderTexture previous = RenderTexture.active;
        Graphics.Blit(source, temporary);
        RenderTexture.active = temporary;

        Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);
        readable.Apply(false, false);

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(temporary);
        ReadableTextureCache[source] = readable;
        return readable;
    }

    private static bool TryFindAlphaBounds(
        Color32[] pixels,
        int textureWidth,
        RectInt searchBounds,
        out RectInt bounds)
    {
        int minX = searchBounds.xMax;
        int minY = searchBounds.yMax;
        int maxX = -1;
        int maxY = -1;

        for (int y = searchBounds.yMin; y < searchBounds.yMax; y++)
        {
            int rowOffset = y * textureWidth;
            for (int x = searchBounds.xMin; x < searchBounds.xMax; x++)
            {
                if (pixels[rowOffset + x].a <= 8)
                {
                    continue;
                }

                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
        }

        if (maxX < minX || maxY < minY)
        {
            bounds = default;
            return false;
        }

        bounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        return true;
    }

    private static RectInt AddPadding(RectInt bounds, RectInt containingBounds, int padding)
    {
        int minX = Mathf.Max(containingBounds.xMin, bounds.xMin - padding);
        int minY = Mathf.Max(containingBounds.yMin, bounds.yMin - padding);
        int maxX = Mathf.Min(containingBounds.xMax, bounds.xMax + padding);
        int maxY = Mathf.Min(containingBounds.yMax, bounds.yMax + padding);
        return new RectInt(minX, minY, maxX - minX, maxY - minY);
    }

    private static void ClearReadableTextureCache()
    {
        foreach (KeyValuePair<Texture2D, Texture2D> pair in ReadableTextureCache)
        {
            if (pair.Value != null)
            {
                UnityEngine.Object.DestroyImmediate(pair.Value);
            }
        }

        ReadableTextureCache.Clear();
    }

    private static string SanitizeFileName(string value)
    {
        char[] invalidCharacters = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalidCharacters.Length; i++)
        {
            value = value.Replace(invalidCharacters[i], '_');
        }

        return string.IsNullOrWhiteSpace(value) ? "Sprite" : value;
    }

    private static void AddV2ToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (int i = 0; i < scenes.Count; i++)
        {
            if (string.Equals(scenes[i].path, TargetScenePath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        int insertIndex = scenes.FindIndex(scene =>
            string.Equals(scene.path, SourceScenePath, StringComparison.OrdinalIgnoreCase));
        insertIndex = insertIndex >= 0 ? insertIndex + 1 : scenes.Count;
        scenes.Insert(insertIndex, new EditorBuildSettingsScene(TargetScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureAssetFolder(string folderPath)
    {
        string[] segments = folderPath.Split('/');
        string current = segments[0];
        for (int i = 1; i < segments.Length; i++)
        {
            string next = $"{current}/{segments[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, segments[i]);
            }

            current = next;
        }
    }
}
