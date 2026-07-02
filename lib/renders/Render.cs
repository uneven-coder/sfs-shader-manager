#nullable enable

using System;
using System.Reflection;
using UnityEngine;

namespace shaders.Lib.Renders
{
    // Minimal AccessTools for reflection (only what is needed)
    internal static class AccessTools
    {
        public static Type TypeByName(string name)
        {   // Find a type by its full name in loaded assemblies
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(name, false);
                if (t != null) return t;
            }
            return null;
        }

        public static PropertyInfo Property(Type type, string name)
            => type?.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    }

    public enum OverlayRenderMode
    {
        BehindUI,
        Inclusive,
        OnTop,
        Exclusive,
        CustomRender
    }

    public static class OverlayDispatcher
    {
        private static Camera? _effectCamera;
        private static Shader? _currentShader;
        private static Material? _effectMaterial;
        private static OverlayImageEffect? _attachedEffect;
        private static OverlayRenderMode _currentMode;

        public static IShaderModule? SelectedModule { get; set; }
        public static object? CurrentArgs { get; set; }
        public static Material? CurrentMaterial => _effectMaterial;

        private static void ResetSharedState()
        {
            Inclusive_Render.Release();
            CurrentUiShader.Value = null;
            CurrentScreenShader.Value = null;
            Inclusive_RenderActive.Value = false;
            Exclusive_Render.UiBlurOverrideTexture = null;
        }

        public static void Render(
            Camera cam,
            RenderTexture srcRT,
            RenderTexture dstRT,
            Shader selectedShader = null,
            Action<Unity.Collections.NativeArray<Color32>, Unity.Collections.NativeArray<Color32>, int, int> cpuEffect = null,
            OverlayRenderMode renderMode = OverlayRenderMode.BehindUI)
        {   // Dispatch rendering based on mode, ensuring Exclusive generates UI background RT

            if (cam == null)
            {
                RemoveCameraImageEffect();
                ResetSharedState();
                return;
            }

            // Custom mode can be shaderless at call site and still use SelectedModule.Shader.
            if (selectedShader == null && renderMode != OverlayRenderMode.CustomRender)
            {
                RemoveCameraImageEffect();
                ResetSharedState();
                return;
            }

            var effectiveShader = selectedShader ?? SelectedModule?.Shader;
            if (_effectCamera == cam && _attachedEffect != null && _currentMode == renderMode && _currentShader == effectiveShader)
            {
                if (!_attachedEffect.enabled)
                    _attachedEffect.enabled = true;

                if (_effectMaterial != null && SelectedModule != null && CurrentArgs != null)
                    SelectedModule.ApplyArgs(_effectMaterial, CurrentArgs);

                return;
            }

            // Force reattachment if camera changed or effect was destroyed
            if (_effectCamera != cam || _attachedEffect == null || _currentMode != renderMode)
                RemoveCameraImageEffect();

            SetupCameraImageEffect(cam, selectedShader, renderMode);
        }

        private static void SetupCameraImageEffect(Camera cam, Shader? shader, OverlayRenderMode mode)
        {   // Attach or update a MonoBehaviour to perform OnRenderImage blit

            var shaderFx = cam.gameObject.GetComponent<shadersOverlayEffect>();
            if (shaderFx != null)
            {   // Keep a single render authority to avoid double-application per frame.
                foreach (var stale in cam.gameObject.GetComponents<OverlayImageEffect>())
                    if (stale != null)
                        UnityEngine.Object.Destroy(stale);

                _attachedEffect = null;
                _effectCamera = cam;
                _currentMode = mode;
                _currentShader = shader ?? SelectedModule?.Shader;

                if (_effectMaterial != null)
                {
                    UnityEngine.Object.Destroy(_effectMaterial);
                    _effectMaterial = null;
                }
                return;
            }

            var existingEffects = cam.gameObject.GetComponents<OverlayImageEffect>();
            OverlayImageEffect cameraEffect = null;
            foreach (var effect in existingEffects)
            {
                if (effect == null)
                    continue;

                if (cameraEffect == null)
                {
                    cameraEffect = effect;
                    continue;
                }

                UnityEngine.Object.Destroy(effect);
            }

            if (_attachedEffect == null || _effectCamera != cam)
            {
                _attachedEffect = cameraEffect ?? cam.gameObject.AddComponent<OverlayImageEffect>();
                _effectCamera = cam;
                _currentMode = mode;
            }
            else if (cameraEffect != null && _attachedEffect != cameraEffect)
            {
                _attachedEffect = cameraEffect;
            }

            var needsSceneMat = mode is OverlayRenderMode.BehindUI or OverlayRenderMode.OnTop;
            var effectiveShader = shader ?? SelectedModule?.Shader;

            if (needsSceneMat)
            {   // Create scene material for standard modes
                if (effectiveShader == null)
                {
                    if (_effectMaterial != null)
                    {
                        UnityEngine.Object.Destroy(_effectMaterial);
                        _effectMaterial = null;
                    }
                    _currentShader = null;
                }
                else if (_effectMaterial == null || _currentShader != effectiveShader)
                {
                    if (_effectMaterial != null)
                        UnityEngine.Object.Destroy(_effectMaterial);

                    _effectMaterial = new Material(effectiveShader);
                    _currentShader = effectiveShader;

                    if (SelectedModule != null && CurrentArgs != null)
                        SelectedModule.ApplyArgs(_effectMaterial, CurrentArgs);
                }
            }
            else
            {   // CustomRender handles its own materials
                if (_effectMaterial != null)
                {
                    UnityEngine.Object.Destroy(_effectMaterial);
                    _effectMaterial = null;
                }
                _currentShader = effectiveShader;
            }

            _attachedEffect.Configure(mode, effectiveShader, _effectMaterial);
            _attachedEffect.enabled = true;
        }

        private static void RemoveCameraImageEffect()
        {   // Remove the MonoBehaviour and material from the camera

            if (_attachedEffect != null)
            {
                if (_attachedEffect.gameObject != null)
                    UnityEngine.Object.Destroy(_attachedEffect);
                _attachedEffect = null;
            }

            if (_effectMaterial != null)
            {
                UnityEngine.Object.Destroy(_effectMaterial);
                _effectMaterial = null;
            }

            _effectCamera = null;
            _currentShader = null;
        }

        public static void ForceRefresh(Camera cam)
        {   // Force reattachment to current camera with current settings

            if (cam == null || (_currentShader == null && _currentMode != OverlayRenderMode.CustomRender && SelectedModule == null))
                return;

            var fx = cam.GetComponent<shadersOverlayEffect>();
            var mode = fx != null ? fx.renderMode : _currentMode;
            var shader = fx != null ? fx.selectedShader : _currentShader;
            if (shader == null && mode == OverlayRenderMode.CustomRender && SelectedModule != null)
                shader = SelectedModule.Shader;

            RemoveCameraImageEffect();
            SetupCameraImageEffect(cam, shader, mode);
        }

        public static void ClearAllEffects(bool clearSharedState = true)
        {   // Clear dispatcher/effect state from all cameras and optionally reset shared module/args state.
            for (var i = 0; i < Camera.allCamerasCount; i++)
            {
                var cam = Camera.allCameras[i];
                if (cam != null && cam.gameObject != null)
                    ClearCameraEffect(cam);
            }

            RemoveCameraImageEffect();
            ResetSharedState();

            if (!clearSharedState)
                return;

            SelectedModule = null;
            CurrentArgs = null;
        }

        public static void ClearCameraEffect(Camera cam)
        {   // Clear all shader-related components/state from a single camera.
            if (cam == null || cam.gameObject == null)
                return;

            var clearingActiveEffectCamera = _effectCamera == cam;

            var shaderFx = cam.gameObject.GetComponent<shadersOverlayEffect>();
            if (shaderFx != null)
            {
                shaderFx.selectedShader = null;
                shaderFx.customRenderKey = string.Empty;
                shaderFx.renderMode = OverlayRenderMode.BehindUI;
            }

            foreach (var effect in cam.gameObject.GetComponents<OverlayImageEffect>())
                if (effect != null)
                    UnityEngine.Object.Destroy(effect);

            if (!clearingActiveEffectCamera)
                return;

            RemoveCameraImageEffect();
            ResetSharedState();
        }
    }

    // MonoBehaviour to perform OnRenderImage blit with the given material
    public sealed class OverlayImageEffect : MonoBehaviour
    {
        private OverlayRenderMode _mode;
        private Shader? _shader;
        private Material? _sceneMat;
        private Material? _customMat;
        private Shader? _customShader;

        public void Configure(OverlayRenderMode mode, Shader? shader, Material? sceneMat)
        {   // Set the mode, shader, and scene material for rendering
            _mode = mode;
            _shader = shader;
            _sceneMat = sceneMat;
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {   // Always write to dest, and update UI RT if needed
            var shader = _shader ?? OverlayDispatcher.SelectedModule?.Shader;

            switch (_mode)
            {
                case OverlayRenderMode.Exclusive:
                    if (shader == null)
                    {
                        Graphics.Blit(src, dest);
                        return;
                    }
                    // 1) build/update the RT for UI background using the active camera frame
                    Exclusive_Render.RenderUIBackground(src, shader);
                    // 2) do NOT change the scene output
                    Graphics.Blit(src, dest);
                    return;

                case OverlayRenderMode.Inclusive:
                    // Always write to dest, even if shader is null
                    Inclusive_Render.Render(src, dest, shader);
                    return;

                case OverlayRenderMode.CustomRender:
                    if (shader == null)
                    {
                        Graphics.Blit(src, dest);
                        return;
                    }

                    if (_customMat == null || _customShader != shader)
                    {
                        if (_customMat != null)
                            UnityEngine.Object.Destroy(_customMat);

                        _customMat = new Material(shader);
                        _customShader = shader;
                    }

                    if (OverlayDispatcher.SelectedModule != null && OverlayDispatcher.CurrentArgs != null)
                        OverlayDispatcher.SelectedModule.ApplyArgs(_customMat, OverlayDispatcher.CurrentArgs);

                    Graphics.Blit(src, dest, _customMat);
                    return;

                default:
                    // normal scene postprocess modes
                    Graphics.Blit(src, dest, _sceneMat);
                    return;
            }
        }

        private void OnDisable()
        {
            if (_customMat != null)
            {
                UnityEngine.Object.Destroy(_customMat);
                _customMat = null;
            }

            _customShader = null;
        }
    }
}

namespace shaders
{
    using shaders.Lib.Renders;

    public static class Inclusive_RenderActive
    {
        public static bool Value;
    }

    public static class CurrentScreenShader
    {
        public static Shader? Value;
    }

    public static class CurrentUiShader
    {
        public static Shader? Value;
    }

    public static class Exclusive_Render
    {
        public static RenderTexture? UiBackgroundTexture;
        public static readonly int GlobalUiBackgroundTexId = Shader.PropertyToID("_shaders_UIBackgroundTex");

        public static Texture? UiBlurOverrideTexture { get; set; }

        private static Material? _uiMat;
        private static Shader? _selectedShader;

        public static RenderTexture? RenderUIBackground(RenderTexture sceneTexture, Shader? selectedShader)
        {   // Render UI background using the selected shader, preserving alpha

            if (sceneTexture == null || sceneTexture.width <= 1 || sceneTexture.height <= 1)
                return null;

            EnsureUIRenderTexture(sceneTexture);

            CurrentUiShader.Value = selectedShader;
            Inclusive_RenderActive.Value = selectedShader != null;

            if (selectedShader != _selectedShader)
            {
                DestroyUiMaterial();
                _selectedShader = selectedShader;
                _uiMat = _selectedShader != null ? new(_selectedShader) : null;
            }

            if (_uiMat != null && OverlayDispatcher.SelectedModule != null && OverlayDispatcher.CurrentArgs != null)
                OverlayDispatcher.SelectedModule.ApplyArgs(_uiMat, OverlayDispatcher.CurrentArgs);

            // Clear the render texture to prevent old frames from persisting
            if (UiBackgroundTexture != null)
            {
                var prev = RenderTexture.active;
                RenderTexture.active = UiBackgroundTexture;
                GL.Clear(true, true, Color.clear);
                RenderTexture.active = prev;
            }

            if (_uiMat != null)
                Graphics.Blit(sceneTexture, UiBackgroundTexture, _uiMat);
            else
                Graphics.Blit(sceneTexture, UiBackgroundTexture);

            Shader.SetGlobalTexture(GlobalUiBackgroundTexId, UiBackgroundTexture);

            UiBlurOverrideTexture = UiBackgroundTexture;

            return UiBackgroundTexture;
        }

        public static void Release()
        {   // Release all resources and reset state
            DestroyUiMaterial();

            CurrentUiShader.Value = null;
            Inclusive_RenderActive.Value = false;

            if (UiBackgroundTexture != null)
            {
                UiBackgroundTexture.Release();
                UnityEngine.Object.Destroy(UiBackgroundTexture);
                UiBackgroundTexture = null;
            }

            Shader.SetGlobalTexture(GlobalUiBackgroundTexId, (Texture?)null);
            UiBlurOverrideTexture = null;
        }

        private static void EnsureUIRenderTexture(RenderTexture src)
        {   // Ensure the UI background RT matches the source
            if (src == null || src.width <= 1 || src.height <= 1)
                return;

            var desc = src.descriptor;
            if (desc.width <= 1 || desc.height <= 1)
                return;

            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            desc.useMipMap = false;
            desc.autoGenerateMips = false;
            desc.colorFormat = RenderTextureFormat.ARGB32;

            var needsRebuild = UiBackgroundTexture == null
                || UiBackgroundTexture.width != desc.width
                || UiBackgroundTexture.height != desc.height
                || UiBackgroundTexture.format != desc.colorFormat;

            if (!needsRebuild)
                return;

            if (UiBackgroundTexture != null)
            {
                UiBackgroundTexture.Release();
                UnityEngine.Object.Destroy(UiBackgroundTexture);
            }

            UiBackgroundTexture = new(desc)
            {
                name = "shaders_UIBackground",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            UiBackgroundTexture.Create();
        }

        private static void DestroyUiMaterial()
        {   // Destroy the UI material if it exists
            if (_uiMat == null)
                return;

            UnityEngine.Object.Destroy(_uiMat);
            _uiMat = null;
        }
    }

    public static class BehindUI_Render
    {
        private static Material? _sceneMat;
        private static Shader? _selectedShader;

        public static void RenderScene(RenderTexture src, RenderTexture dest, Shader? selectedShader)
        {   // Render the scene behind UI, applying shader if provided

            if (src == null)
                return;

            CurrentScreenShader.Value = selectedShader;

            if (selectedShader != _selectedShader || _sceneMat == null)
            {   // Shader or material changed, rebuild
                DestroySceneMaterial();
                _selectedShader = selectedShader;
                _sceneMat = selectedShader != null ? new(selectedShader) : null;
            }

            if (_sceneMat != null && OverlayDispatcher.SelectedModule != null && OverlayDispatcher.CurrentArgs != null)
                OverlayDispatcher.SelectedModule.ApplyArgs(_sceneMat, OverlayDispatcher.CurrentArgs);

            if (_sceneMat != null)
                Graphics.Blit(src, dest, _sceneMat);
            else
                Graphics.Blit(src, dest);
        }

        public static void Release() => DestroySceneMaterial();

        private static void DestroySceneMaterial()
        {   // Destroy the scene material if it exists
            if (_sceneMat == null)
                return;

            UnityEngine.Object.Destroy(_sceneMat);
            _sceneMat = null;
        }
    }

    public static class Inclusive_Render
    {
        private static Material? _inclusiveMat;
        private static Shader? _inclusiveShader;

        public static void Render(RenderTexture src, RenderTexture dest, Shader? selectedShader)
        {   // Inclusive = apply shader to both scene output and UI background in parallel

            // Always update UI background in parallel
            Exclusive_Render.RenderUIBackground(src, selectedShader);

            // Always write to dest (or screen if dest is null), using shader if provided
            if (selectedShader == null)
            {
                Graphics.Blit(src, dest);
                return;
            }

            if (_inclusiveMat == null || _inclusiveShader != selectedShader)
            {
                if (_inclusiveMat != null)
                    UnityEngine.Object.Destroy(_inclusiveMat);
                _inclusiveMat = new Material(selectedShader);
                _inclusiveShader = selectedShader;
            }

            if (OverlayDispatcher.SelectedModule != null && OverlayDispatcher.CurrentArgs != null)
                OverlayDispatcher.SelectedModule.ApplyArgs(_inclusiveMat, OverlayDispatcher.CurrentArgs);

            Graphics.Blit(src, dest, _inclusiveMat);
        }

        public static void Release()
        {   // Release all resources for inclusive render
            if (_inclusiveMat != null)
            {
                UnityEngine.Object.Destroy(_inclusiveMat);
                _inclusiveMat = null;
            }

            _inclusiveShader = null;
            BehindUI_Render.Release();
            Exclusive_Render.Release();
        }
    }

    public static class CustomRender_Render
    {
        private static string? _currentModuleKey;
        private static IShaderModule? _currentModule;
        private static Material? _screenMat;
        private static Shader? _screenShader;
        private static readonly System.Collections.Generic.Dictionary<Type, bool> _isObjectTargetCache = new();
        private static readonly System.Collections.Generic.Dictionary<Type, MethodInfo?> _runMethodCache = new();
        private static readonly System.Collections.Generic.Dictionary<Type, Type?> _argsTypeCache = new();
        private static readonly System.Collections.Generic.Dictionary<Type, FieldInfo?> _shaderFieldCache = new();

        private static bool IsObjectTargetModule(Type moduleType)
        {   // Detect ObjectTargetShaderModule<,> across inheritance to avoid fullscreen blits for mesh-target modules.
            if (_isObjectTargetCache.TryGetValue(moduleType, out var cached))
                return cached;

            for (var type = moduleType; type != null; type = type.BaseType)
            {
                if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(ObjectTargetShaderModule<,>))
                    continue;

                _isObjectTargetCache[moduleType] = true;
                return true;
            }

            _isObjectTargetCache[moduleType] = false;
            return false;
        }

        private static Type? ResolveArgsType(Type moduleType)
        {
            if (_argsTypeCache.TryGetValue(moduleType, out var cached))
                return cached;

            var t = moduleType;
            while (t != null && !(t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ShaderModule<,>)))
                t = t.BaseType;

            var argsType = t?.GetGenericArguments().Length == 2 ? t.GetGenericArguments()[0] : null;
            _argsTypeCache[moduleType] = argsType;
            return argsType;
        }

        private static MethodInfo? ResolveRunMethod(Type moduleType)
        {
            if (_runMethodCache.TryGetValue(moduleType, out var cached))
                return cached;

            var runMethod = moduleType.GetMethod("Run");
            _runMethodCache[moduleType] = runMethod;
            return runMethod;
        }

        private static FieldInfo? ResolveShaderField(Type moduleType)
        {
            if (_shaderFieldCache.TryGetValue(moduleType, out var cached))
                return cached;

            var shaderField = moduleType.GetField("_shader", BindingFlags.NonPublic | BindingFlags.Instance);
            _shaderFieldCache[moduleType] = shaderField;
            return shaderField;
        }

        public static void Render(RenderTexture src, RenderTexture dest, string moduleKey, Shader? overrideShader = null)
        {
            if (string.IsNullOrEmpty(moduleKey))
            {
                Debug.LogError("[CustomRender_Render] Module key is null or empty");
                Graphics.Blit(src, dest);
                return;
            }

            var module = ShaderRegistry.Get(moduleKey);
            if (module == null)
            {
                Debug.LogError($"[CustomRender_Render] Module '{moduleKey}' not found in registry");
                Graphics.Blit(src, dest);
                return;
            }

            _currentModuleKey = moduleKey;
            _currentModule = module;
            OverlayDispatcher.SelectedModule = module;

            var moduleType = module.GetType();

            if (overrideShader != null)
            {
                var shaderField = ResolveShaderField(moduleType);
                if (shaderField != null && shaderField.GetValue(module) as Shader != overrideShader)
                    shaderField.SetValue(module, overrideShader);
            }

            var argsType = ResolveArgsType(moduleType);
            if (argsType != null)
            {
                var args = OverlayDispatcher.CurrentArgs ?? Activator.CreateInstance(argsType);
                OverlayDispatcher.CurrentArgs = args;

                var runMethod = ResolveRunMethod(moduleType);
                if (runMethod != null)
                    runMethod.Invoke(module, new[] { args });
                else
                    Debug.LogError($"[CustomRender_Render] Run method not found on module '{moduleKey}'");
            }

            if (IsObjectTargetModule(moduleType))
            {   // Object-target modules own world/material rendering; keep scene output unchanged.
                Graphics.Blit(src, dest);
                return;
            }

            var shader = overrideShader ?? module.Shader;
            if (shader == null)
            {
                Graphics.Blit(src, dest);
                return;
            }

            if (_screenMat == null || _screenShader != shader)
            {
                if (_screenMat != null)
                    UnityEngine.Object.Destroy(_screenMat);
                _screenMat = new Material(shader);
                _screenShader = shader;
            }

            if (OverlayDispatcher.CurrentArgs != null)
                module.ApplyArgs(_screenMat, OverlayDispatcher.CurrentArgs);

            Graphics.Blit(src, dest, _screenMat);
        }

        public static void Release()
        {
            if (_currentModule != null && !string.IsNullOrEmpty(_currentModuleKey))
                _currentModule.GetType().GetMethod("RestoreMaterials")?.Invoke(_currentModule, null);

            if (_screenMat != null)
            {
                UnityEngine.Object.Destroy(_screenMat);
                _screenMat = null;
            }

            _screenShader = null;
            _currentModule = null;
            _currentModuleKey = null;
        }
    }

    [DisallowMultipleComponent]
    public sealed class shadersOverlayEffect : MonoBehaviour
    {
        [Header("UI Background (Exclusive)")]
        public Shader? selectedShader;

        public OverlayRenderMode renderMode = OverlayRenderMode.BehindUI;

        [Header("Custom Render Settings")]
        public string customRenderKey = "AtmoShader";

        private Camera? _attachedCamera;
        private int _lastFrameActive = -1;
        private bool _inactiveNotified;

        private void Awake()
        {   // Cache camera component on initialization
            _attachedCamera = GetComponent<Camera>();
            _lastFrameActive = Time.frameCount;
            _inactiveNotified = false;
        }

        private void OnEnable()
        {   // Reattach effect when component is enabled ensuring camera is valid
            _attachedCamera = GetComponent<Camera>();
            _lastFrameActive = Time.frameCount;
            _inactiveNotified = false;

            if (_attachedCamera != null && selectedShader != null && renderMode != OverlayRenderMode.CustomRender)
                OverlayDispatcher.ForceRefresh(_attachedCamera);
        }

        private void LateUpdate()
        {   // Detect if camera became inactive and clean up materials
            if (_attachedCamera == null || !_attachedCamera.enabled || !_attachedCamera.gameObject.activeInHierarchy)
            {
                if (Time.frameCount - _lastFrameActive > 2 && !_inactiveNotified)
                {   // Camera has been inactive for multiple frames, trigger cleanup
                    GeneratedUI.GeneratedLayout.NotifyCameraInactive();
                    _inactiveNotified = true;
                }
                return;
            }

            _lastFrameActive = Time.frameCount;
            _inactiveNotified = false;
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {   // Validate camera before rendering and handle all modes consistently
            if (_attachedCamera == null || src == null)
            {
                if (dest != null)
                    Graphics.Blit(src != null ? (Texture)src : Texture2D.blackTexture, dest);
                return;
            }

            switch (renderMode)
            {
                case OverlayRenderMode.BehindUI:
                    CurrentUiShader.Value = null;
                    Exclusive_Render.UiBlurOverrideTexture = null;
                    Inclusive_RenderActive.Value = false;
                    BehindUI_Render.RenderScene(src, dest, selectedShader);
                    break;

                case OverlayRenderMode.Exclusive:
                    Exclusive_Render.RenderUIBackground(src, selectedShader);
                    Exclusive_Render.UiBlurOverrideTexture = Exclusive_Render.UiBackgroundTexture;
                    if (dest != null) Graphics.Blit(src, dest);
                    break;

                case OverlayRenderMode.Inclusive:
                    CurrentUiShader.Value = selectedShader;
                    Exclusive_Render.UiBlurOverrideTexture = null;
                    Inclusive_RenderActive.Value = selectedShader != null;
                    Inclusive_Render.Render(src, dest, selectedShader);
                    Exclusive_Render.UiBlurOverrideTexture = Exclusive_Render.UiBackgroundTexture;
                    break;

                case OverlayRenderMode.CustomRender:
                    CustomRender_Render.Render(src, dest, customRenderKey, selectedShader);
                    break;

                default:
                    CurrentUiShader.Value = null;
                    Exclusive_Render.UiBlurOverrideTexture = null;
                    Inclusive_RenderActive.Value = false;
                    if (dest != null) Graphics.Blit(src, dest);
                    break;
            }
        }

        private void OnDisable()
        {   // Clean up resources when disabled
            Inclusive_Render.Release();
            if (renderMode == OverlayRenderMode.CustomRender)
                CustomRender_Render.Release();

            if (_attachedCamera != null)
                OverlayDispatcher.ClearCameraEffect(_attachedCamera);
        }

        private void OnDestroy() => OnDisable();
    }
}
