using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using shaders;
using UnityEngine.UI;
using shaders.Lib.ShaderModules.ShaderPack;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;
using HarmonyLib;
using SFS.Cameras;
using SFS.World;
using SFS.WorldBase;
using SFS.IO;
using SFS.Input;

public static class ShaderAssetRegistry
{   // Runtime registry for discovered shaders and compute shaders
    private static readonly Dictionary<string, Shader> _shaders = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, ComputeShader> _computeShaders = new(StringComparer.OrdinalIgnoreCase);

    public static bool TryGetShader(string key, out Shader shader)
    {
        shader = null;
        if (string.IsNullOrWhiteSpace(key)) return false;
        return _shaders.TryGetValue(key, out shader);
    }

    public static bool TryGetCompute(string key, out ComputeShader shader)
    {
        shader = null;
        if (string.IsNullOrWhiteSpace(key)) return false;
        return _computeShaders.TryGetValue(key, out shader);
    }

    public static void Register(Shader shader, params string[] extraKeys)
    {   // Register a Shader with multiple keys
        if (!shader) return;
        _shaders[shader.name] = shader;
        var last = LastSegment(shader.name);
        if (!string.IsNullOrEmpty(last)) _shaders[last] = shader;
        foreach (var k in extraKeys ?? Array.Empty<string>())
            if (!string.IsNullOrWhiteSpace(k)) _shaders[k] = shader;
    }

    public static void Register(ComputeShader shader, params string[] extraKeys)
    {   // Register a ComputeShader with multiple keys
        if (!shader) return;
        _computeShaders[shader.name] = shader;
        foreach (var k in extraKeys ?? Array.Empty<string>())
            if (!string.IsNullOrWhiteSpace(k)) _computeShaders[k] = shader;
    }

    internal static string LastSegment(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var idx = s.LastIndexOf('/');
        return idx >= 0 && idx + 1 < s.Length ? s[(idx + 1)..] : s;
    }
}

namespace shaders
{
    /// Attach this to shader module classes to declare metadata for normal shaders.
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ShaderModuleAttribute : Attribute
    {
        public string Name { get; }
        public ShaderType Type { get; }
        public string LoadBy { get; }
        public shaders.Lib.Renders.OverlayRenderMode RenderTarget { get; }

        public ShaderModuleAttribute(string name, ShaderType type, string loadBy, shaders.Lib.Renders.OverlayRenderMode renderTarget)
        { Name = name; Type = type; LoadBy = loadBy; RenderTarget = renderTarget; }
    }

    /// Defines config routing metadata for shader argument fields.
    /// Group controls the visible config section and Tab optionally creates tabbed sections within that group.
    ///
    /// Usage patterns:
    /// 1) Group-only fields:
    ///    [ShaderArg(group: "General", property: "_Contrast")]
    /// 2) Group + tab profile fields:
    ///    [ShaderArg(group: "Profiles", tab: "World")]
    ///    [ShaderArg(group: "Profiles", tab: "Scaled")]
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class ShaderArgAttribute : Attribute
    {
        /// Section name used by generated config UI.
        public string Group { get; }
        /// Optional tab name inside the group.
        public string Tab { get; }
        public string Property { get; }
        public object DefaultValue { get; }
        public bool AutoApply { get; }
        /// When false, generated config UI will hide this field.
        public bool ExposeInUi { get; }

        public ShaderArgAttribute(
            string group = "General",
            string tab = null,
            string property = null,
            object defaultValue = null,
            bool autoApply = true,
            bool exposeInUi = true)
        {
            Group = group;
            Tab = tab;
            Property = property;
            DefaultValue = defaultValue;
            AutoApply = autoApply;
            ExposeInUi = exposeInUi;
        }
    }

    public enum ShaderRouteMode
    {
        IncludeOnly,
        ExcludeListed,
    }

    /// Declares where a shader module is allowed to run by scene and camera name.
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ShaderRouteAttribute : Attribute
    {
        public ShaderRouteMode SceneMode { get; }
        public string[] Scenes { get; }
        public ShaderRouteMode CameraMode { get; }
        public string[] Cameras { get; }

        public ShaderRouteAttribute(ShaderRouteMode sceneMode, string[] scenes)
            : this(sceneMode, scenes, ShaderRouteMode.IncludeOnly, Array.Empty<string>())
        {
        }

        public ShaderRouteAttribute(ShaderRouteMode sceneMode, string[] scenes, ShaderRouteMode cameraMode, string[] cameras)
        {
            SceneMode = sceneMode;
            Scenes = scenes ?? Array.Empty<string>();
            CameraMode = cameraMode;
            Cameras = cameras ?? Array.Empty<string>();
        }
    }

    /// Runtime routing API for scene/camera-targeted shader application.
    public static class ShaderRouteApi
    {
        private static string _currentScene = "unknown";
        private static readonly Dictionary<string, Func<string, Camera, bool>> _moduleOverrides = new(StringComparer.Ordinal);

        public static string CurrentScene => _currentScene;

        public static void SetCurrentScene(string sceneName)
        {
            _currentScene = NormalizeToken(sceneName);
        }

        public static void RegisterRouteOverride(string moduleName, Func<string, Camera, bool> route)
        {
            if (string.IsNullOrWhiteSpace(moduleName) || route == null)
                return;
            _moduleOverrides[moduleName] = route;
        }

        public static void ClearRouteOverride(string moduleName)
        {
            if (string.IsNullOrWhiteSpace(moduleName))
                return;
            _moduleOverrides.Remove(moduleName);
        }

        public static bool IsEligible(IShaderModule module, Camera cam)
        {
            if (module == null || cam == null)
                return false;

            if (_moduleOverrides.TryGetValue(module.Name, out var route))
                return route(_currentScene, cam);

            var attr = module.GetType().GetCustomAttribute<ShaderRouteAttribute>();
            if (attr == null)
                return true;

            var sceneOk = IsTokenAllowed(_currentScene, attr.Scenes, attr.SceneMode);
            if (!sceneOk)
                return false;

            var cameraName = NormalizeToken(cam.name);
            if (attr.Cameras == null || attr.Cameras.Length == 0)
                return true;

            return IsTokenAllowed(cameraName, attr.Cameras, attr.CameraMode);
        }

        private static bool IsTokenAllowed(string value, string[] configuredValues, ShaderRouteMode mode)
        {
            var set = configuredValues ?? Array.Empty<string>();
            if (set.Length == 0)
                return true;

            var hit = false;
            for (var i = 0; i < set.Length; i++)
            {
                if (string.Equals(NormalizeToken(set[i]), value, StringComparison.Ordinal))
                {
                    hit = true;
                    break;
                }
            }

            return mode == ShaderRouteMode.IncludeOnly ? hit : !hit;
        }

        private static string NormalizeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "unknown";

            var v = value.Trim().ToLowerInvariant();
            if (v.EndsWith("_pc", StringComparison.Ordinal))
                v = v.Substring(0, v.Length - 3);
            if (v == "mainmenu")
                return "home";
            return v;
        }
    }

    /// Non-generic interface for normal shader registry storage.
    public interface IRestorableShaderModule
    {
        void RestoreTouchedState();
    }

    public interface IShaderModule
    {
        string Name { get; }
        Shader Shader { get; }
        bool IsLoaded { get; }

        // New: Apply arguments to a material
        void ApplyArgs(Material mat, object args);

    }

    /// Base class for typed normal shader modules.
    public abstract class ShaderModule<TArgs, TResult> : IShaderModule
    {
        protected Shader _shader;
        private readonly ShaderModuleAttribute _moduleAttribute;
        private bool _loggedMissing;

        private readonly List<IShaderModule> _subShaders = new List<IShaderModule>();

        // Multi-preset support
        private readonly List<TArgs> _argPresets = new List<TArgs>();
        private int _activePresetIndex;

        protected ShaderModule()
        {
            _moduleAttribute = GetType().GetCustomAttribute<ShaderModuleAttribute>();

            // Initialize with default args
            _argPresets.Add(default);
            _activePresetIndex = 0;
        }

        public virtual string Name => _moduleAttribute?.Name ?? GetType().Name;
        public shaders.Lib.Renders.OverlayRenderMode RenderTarget => _moduleAttribute?.RenderTarget ?? default;

        // Lazy load: allows AssetBundle-discovered shaders to be used after load.
        public Shader Shader => _shader != null ? _shader : LoadShader();
        public bool IsLoaded => Shader != null;
        public Type ArgsType => typeof(TArgs);

        // Multi-preset implementation
        public int ArgPresetCount => _argPresets.Count;

        public int ActivePresetIndex
        {
            get => _activePresetIndex;
            set => _activePresetIndex = Mathf.Clamp(value, 0, Math.Max(0, _argPresets.Count - 1));
        }

        public object GetArgPreset(int index) =>
            index >= 0 && index < _argPresets.Count ? (object)_argPresets[index] : null;

        public object ActiveArgs =>
            _argPresets.Count > 0 ? (object)_argPresets[_activePresetIndex] : null;

        public TArgs ActiveArgsTyped =>
            _argPresets.Count > 0 ? _argPresets[_activePresetIndex] : default;

        public void SetArgPresets(object[] presets)
        {   // Replace all presets with new array, filter by type
            _argPresets.Clear();
            if (presets == null || presets.Length == 0)
            {
                _argPresets.Add(default);
                _activePresetIndex = 0;
                return;
            }

            foreach (var p in presets)
                if (p is TArgs typed) _argPresets.Add(typed);

            if (_argPresets.Count == 0) _argPresets.Add(default);
            _activePresetIndex = Mathf.Clamp(_activePresetIndex, 0, _argPresets.Count - 1);
        }

        public void AddArgPreset(object preset)
        {
            if (preset is TArgs typed) _argPresets.Add(typed);
        }

        public void RemoveArgPreset(int index)
        {
            if (index < 0 || index >= _argPresets.Count || _argPresets.Count <= 1) return;
            _argPresets.RemoveAt(index);
            _activePresetIndex = Mathf.Clamp(_activePresetIndex, 0, _argPresets.Count - 1);
        }

        public void SetActiveArgs(TArgs args)
        {   // Update the currently active preset in-place
            if (_argPresets.Count == 0) _argPresets.Add(args);
            else _argPresets[_activePresetIndex] = args;
        }

        protected virtual Shader LoadShader()
        {   // Load shader from registry, built-in, or resources with detailed diagnostics
            var loadBy = _moduleAttribute?.LoadBy;
            if (_shader != null) return _shader;
            if (_moduleAttribute?.Type != ShaderType.Shader) return null;

            // 1) Try registry (AssetBundle-discovered)
            if (!string.IsNullOrEmpty(loadBy) && ShaderAssetRegistry.TryGetShader(loadBy, out var fromRegistry) && fromRegistry != null)
            {
                Debug.Log($"[ShaderModule] Loaded shader '{loadBy}' for module '{Name}' from AssetBundle registry");
                return _shader = fromRegistry;
            }

            // 2) Try Shader.Find (built-in/global)
            if (!string.IsNullOrEmpty(loadBy))
            {
                _shader = UnityEngine.Shader.Find(loadBy);
                if (_shader != null)
                {
                    Debug.Log($"[ShaderModule] Loaded shader '{loadBy}' for module '{Name}' via Shader.Find");
                    return _shader;
                }
            }

            // 3) Try Resources (custom asset in Resources/)
            if (_shader == null && !string.IsNullOrEmpty(loadBy))
            {
                _shader = UnityEngine.Resources.Load<Shader>(loadBy);
                if (_shader != null)
                {
                    Debug.Log($"[ShaderModule] Loaded shader '{loadBy}' for module '{Name}' from Resources");
                    return _shader;
                }
            }

            if (_shader == null && !_loggedMissing && !string.IsNullOrEmpty(loadBy))
            {   // Log a detailed warning with all attempted load methods
                _loggedMissing = true;
                
                var registryCheck = ShaderAssetRegistry.TryGetShader(loadBy, out var reg) && reg != null;
                var shaderFindCheck = UnityEngine.Shader.Find(loadBy) != null;
                var resourcesCheck = UnityEngine.Resources.Load<Shader>(loadBy) != null;
                
                Debug.LogWarning(
                    $"[ShaderModule] Shader not found for module '{Name}'.\n" +
                    $"  Attempted load key: '{loadBy}'\n" +
                    $"  - AssetBundle registry: {(registryCheck ? "FOUND" : "NOT FOUND")}\n" +
                    $"  - Shader.Find: {(shaderFindCheck ? "FOUND" : "NOT FOUND")}\n" +
                    $"  - Resources.Load: {(resourcesCheck ? "FOUND" : "NOT FOUND")}\n" +
                    $"  Ensure shader is included in build or loaded via AssetBundle before accessing this module."
                );
            }

            return _shader;
        }

        // Material management is handled externally; this class does not create or manage materials.

        public abstract TResult Run(in TArgs args);

        public virtual bool TryValidate(out string error)
        {
            error = null;
            var s = Shader;
            if (s == null)
            {
                error = $"Shader asset not assigned/loaded for '{Name}' (key: '{_moduleAttribute?.LoadBy ?? "null"}').";
                return false;
            }
            return true;
        }

        public virtual bool TrySelfTest(out string report)
        {
            report = "No self-test implemented.";
            return true;
        }

        // New: Default implementation for applying arguments; override in subclasses
        public virtual void ApplyArgs(Material mat, object args)
        {
            if (mat == null || args == null) return;
            if (args is TArgs typedArgs) ApplyArgsAutomatic(mat, typedArgs);
        }

        protected virtual void ApplyArgsAutomatic(Material mat, TArgs args)
        {   // Apply all auto-apply shader args to the material.
            ApplyArgsAutomatic(mat, args, (Func<string, bool>?)null);
        }

        protected void ApplyArgsAutomatic(Material mat, TArgs args, string profilePrefix)
        {   // Apply shared fields plus fields scoped to a profile prefix (e.g. "World" or "Scaled").
            if (string.IsNullOrWhiteSpace(profilePrefix))
            {
                ApplyArgsAutomatic(mat, args, (Func<string, bool>?)null);
                return;
            }

            var scopedPrefix = profilePrefix.Trim() + ".";
            ApplyArgsAutomatic(mat, args, fieldPath =>
                string.IsNullOrWhiteSpace(fieldPath)
                || !fieldPath.Contains(".")
                || fieldPath.StartsWith(scopedPrefix, StringComparison.Ordinal));
        }

        protected virtual void ApplyArgsAutomatic(Material mat, TArgs args, Func<string, bool>? fieldFilter)
        {   // Apply shader arguments to material using attribute metadata
            var flatFields = FlattenFieldsWithMetadata(args, "");

            foreach (var kvp in flatFields)
            {   // Process each flattened field and apply to material
                var fieldPath = kvp.Key;
                if (fieldFilter != null && !fieldFilter(fieldPath))
                    continue;

                var fieldValue = kvp.Value.Value;
                var shaderProp = kvp.Value.ShaderProperty;
                var autoApply = kvp.Value.AutoApply;

                if (!autoApply || fieldValue == null) continue;

                var propName = !string.IsNullOrEmpty(shaderProp) ? shaderProp : "_" + GetLastFieldName(fieldPath);
                var propId = UnityEngine.Shader.PropertyToID(propName);

                if (!mat.HasProperty(propId)) continue;

                switch (fieldValue)
                {   // Apply value based on type
                    case float f: mat.SetFloat(propId, f); break;
                    case int i: mat.SetInt(propId, i); break;
                    case bool b: mat.SetFloat(propId, b ? 1f : 0f); break;
                    case Color c: mat.SetColor(propId, c); break;
                    case Vector3 v3: mat.SetVector(propId, new Vector4(v3.x, v3.y, v3.z, 0f)); break;
                    case Vector4 v4: mat.SetVector(propId, v4); break;
                    case Texture2D tex: mat.SetTexture(propId, tex); break;
                }
            }
        }

        // Per-Type field/attribute shape (which fields exist, their shader property + autoApply
        // metadata) never changes at runtime — only the boxed values do. Caching it once per Type
        // avoids re-walking reflection + attribute lookups on every material/every frame.
        private readonly struct FieldSchemaEntry
        {
            public readonly FieldInfo Field;
            public readonly string ShaderProperty;
            public readonly bool AutoApply;
            public readonly bool IsSimple;
            public readonly bool IsArray;
            public readonly bool IsNested;

            public FieldSchemaEntry(FieldInfo field, string shaderProperty, bool autoApply, bool isSimple, bool isArray, bool isNested)
            {
                Field = field;
                ShaderProperty = shaderProperty;
                AutoApply = autoApply;
                IsSimple = isSimple;
                IsArray = isArray;
                IsNested = isNested;
            }
        }

        private static readonly Dictionary<Type, FieldSchemaEntry[]> _fieldSchemaCache = new();

        private static FieldSchemaEntry[] GetFieldSchema(Type type)
        {
            if (_fieldSchemaCache.TryGetValue(type, out var cached))
                return cached;

            var fields = type.GetFields();
            var schema = new FieldSchemaEntry[fields.Length];
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                var fieldType = field.FieldType;
                var isSimple = IsSimpleType(fieldType);
                var attr = field.GetCustomAttribute<ShaderArgAttribute>();
                schema[i] = new FieldSchemaEntry(
                    field,
                    attr?.Property ?? "",
                    attr?.AutoApply ?? true,
                    isSimple,
                    isArray: !isSimple && fieldType.IsArray,
                    isNested: !isSimple && !fieldType.IsArray && fieldType.IsValueType && !fieldType.IsPrimitive && !fieldType.IsEnum);
            }

            _fieldSchemaCache[type] = schema;
            return schema;
        }

        private static Dictionary<string, (object Value, string ShaderProperty, bool AutoApply)> FlattenFieldsWithMetadata(object obj, string prefix)
        {
            var result = new Dictionary<string, (object, string, bool)>();
            if (obj == null) return result;

            foreach (var entry in GetFieldSchema(obj.GetType()))
            {
                var fieldVal = entry.Field.GetValue(obj);
                var fieldPath = string.IsNullOrEmpty(prefix) ? entry.Field.Name : $"{prefix}.{entry.Field.Name}";

                if (fieldVal == null || entry.IsSimple)
                {
                    result[fieldPath] = (fieldVal, entry.ShaderProperty, entry.AutoApply);
                }
                else if (entry.IsArray && fieldVal is Array arr)
                {
                    for (var i = 0; i < arr.Length; i++)
                    {
                        var element = arr.GetValue(i);
                        var elementPath = $"{fieldPath}[{i}]";

                        if (element != null && !IsSimpleType(element.GetType()))
                            foreach (var nkvp in FlattenFieldsWithMetadata(element, elementPath))
                                result[nkvp.Key] = nkvp.Value;
                        else
                            result[elementPath] = (element, entry.ShaderProperty, entry.AutoApply);
                    }
                }
                else if (entry.IsNested)
                {
                    foreach (var nkvp in FlattenFieldsWithMetadata(fieldVal, fieldPath))
                        result[nkvp.Key] = nkvp.Value;
                }
                else
                {
                    result[fieldPath] = (fieldVal, entry.ShaderProperty, entry.AutoApply);
                }
            }

            return result;
        }

        private static bool IsSimpleType(System.Type type) =>
            type.IsPrimitive || type.IsEnum || type == typeof(string) || 
            type == typeof(Color) || type == typeof(Vector3) || type == typeof(Vector4) ||
            type == typeof(Texture2D);

        private static string GetLastFieldName(string fieldPath)
        {
            var lastDot = fieldPath.LastIndexOf('.');
            var name = lastDot >= 0 ? fieldPath.Substring(lastDot + 1) : fieldPath;
            var bracketIdx = name.IndexOf('[');
            return bracketIdx >= 0 ? name.Substring(0, bracketIdx) : name;
        }

        protected IReadOnlyList<IShaderModule> SubShaders => _subShaders.AsReadOnly();
    }

    /// Base class for shader modules that target specific objects in the scene
    public abstract class ObjectTargetShaderModule<TArgs, TResult> : ShaderModule<TArgs, TResult>, IRestorableShaderModule
    {
        protected sealed class RendererMaterialMap : Dictionary<Renderer, Material[]>
        {
            private readonly ObjectTargetShaderModule<TArgs, TResult> _owner;
            private readonly bool _isOriginalMap;

            public RendererMaterialMap(ObjectTargetShaderModule<TArgs, TResult> owner, bool isOriginalMap)
                : base()
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
                _isOriginalMap = isOriginalMap;
            }

            public new Material[] this[Renderer key]
            {
                get => base[key];
                set => _owner.TrackRendererMapSet(_isOriginalMap, key, value);
            }

            public new void Add(Renderer key, Material[] value) => this[key] = value;

            internal void SetRaw(Renderer key, Material[] value) => base[key] = value;
        }

        protected readonly RendererMaterialMap _originalMaterials;
        protected readonly RendererMaterialMap _customMaterials;
        private readonly Dictionary<string, Action> _restoreCallbacks = new Dictionary<string, Action>(StringComparer.Ordinal);
        private ShaderModuleLiveUpdater? _liveUpdater;
        private TArgs _liveArgs = default;
        private bool _hasLiveArgs;
        private readonly Plane[] _visibilityPlanes = new Plane[6];
        private float _nextVisibilityCheckAt;
        private bool _lastVisibilityState = true;
        protected TArgs? _currentArgs;
        protected bool _isApplied;

        protected ObjectTargetShaderModule()
        {
            _originalMaterials = new RendererMaterialMap(this, isOriginalMap: true);
            _customMaterials = new RendererMaterialMap(this, isOriginalMap: false);
        }

        public virtual void ApplyToTargets(in TArgs args)
        {   // Override in subclass to find objects and apply materials
            _currentArgs = args;
            _isApplied = true;
        }

        public virtual void UpdateArgs(in TArgs args)
        {   // Update shader args on all active custom materials
            _currentArgs = args;
            if (!_isApplied) return;

            foreach (var mats in _customMaterials.Values)
            {
                if (mats == null) continue;
                foreach (var mat in mats)
                    if (mat != null) ApplyArgsAutomatic(mat, args);
            }
        }

        public virtual void RestoreMaterials()
        {   // Restore original materials and cleanup custom materials, including sub-shaders
            if (!_isApplied && _originalMaterials.Count == 0 && _customMaterials.Count == 0 && _restoreCallbacks.Count == 0)
                return;

            foreach (var subShader in SubShaders)
            {
                if (subShader is IRestorableShaderModule restorableSubShader)
                {
                    restorableSubShader.RestoreTouchedState();
                    continue;
                }

                var restoreMethod = subShader?.GetType().GetMethod("RestoreMaterials", BindingFlags.Public | BindingFlags.Instance);
                restoreMethod?.Invoke(subShader, null);
            }

            foreach (var kvp in _originalMaterials)
                if (kvp.Key != null) kvp.Key.sharedMaterials = kvp.Value ?? Array.Empty<Material>();

            foreach (var callback in _restoreCallbacks.Values)
            {
                try { callback?.Invoke(); }
                catch (Exception ex) { Debug.LogError($"[ObjectTargetShaderModule] Restore callback failed for '{Name}': {ex.Message}"); }
            }

            foreach (var mats in _customMaterials.Values)
                DestroyMaterialArray(mats);

            _originalMaterials.Clear();
            _customMaterials.Clear();
            _restoreCallbacks.Clear();
            _isApplied = false;
        }

        public void RestoreTouchedState() => RestoreMaterials();

        protected virtual string LiveUpdaterObjectName => $"[shaders] {Name}LiveUpdater";

        protected virtual void OnLiveUpdate(in TArgs args)
        {   // Modules can override to run deterministic per-frame update logic.
        }

        protected void StartOrUpdateLiveLoop(in TArgs args)
        {   // Ensure a single updater object exists and keep latest args for per-frame ticks.
            _liveArgs = args;
            _hasLiveArgs = true;

            if (_liveUpdater != null)
            {
                _liveUpdater.Tick = RunLiveUpdateTick;
                return;
            }

            var updaterObject = GameObject.Find(LiveUpdaterObjectName)
                ?? new GameObject(LiveUpdaterObjectName) { hideFlags = HideFlags.HideAndDontSave };
            _liveUpdater = updaterObject.GetComponent<ShaderModuleLiveUpdater>() ?? updaterObject.AddComponent<ShaderModuleLiveUpdater>();
            if (_liveUpdater == null)
                throw new InvalidOperationException($"Failed to create live updater for module '{Name}'.");

            _liveUpdater.Tick = RunLiveUpdateTick;
        }

        protected void StopLiveLoop()
        {   // Stop shared live updates when module no longer owns runtime rendering work.
            _hasLiveArgs = false;

            if (_liveUpdater == null)
                return;

            _liveUpdater.Tick = null;
            if (_liveUpdater.gameObject != null)
                UnityEngine.Object.Destroy(_liveUpdater.gameObject);

            _liveUpdater = null;
        }

        private void RunLiveUpdateTick()
        {
            if (!_hasLiveArgs)
                return;

            OnLiveUpdate(_liveArgs);
        }

        protected bool ShouldProcessForCamera(Camera? camera, Renderer[]? renderers, float checkIntervalSeconds = 0.08f)
        {   // Run a cheap, throttled frustum test to skip offscreen target updates.
            if (camera == null || renderers == null || renderers.Length == 0)
                return false;

            var now = Time.unscaledTime;
            if (now < _nextVisibilityCheckAt)
                return _lastVisibilityState;

            _nextVisibilityCheckAt = now + Mathf.Max(0.02f, checkIntervalSeconds);
            GeometryUtility.CalculateFrustumPlanes(camera, _visibilityPlanes);

            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !renderer.enabled || renderer.gameObject == null || !renderer.gameObject.activeInHierarchy)
                    continue;

                if (GeometryUtility.TestPlanesAABB(_visibilityPlanes, renderer.bounds))
                {
                    _lastVisibilityState = true;
                    return true;
                }
            }

            _lastVisibilityState = false;
            return false;
        }

        protected virtual Material CreateCustomMaterial(Material original, TArgs args)
        {
            var shader = Shader;
            if (shader == null)
            {
                Debug.LogWarning($"[ObjectTargetShaderModule] Shader not loaded for '{Name}'. Using original material.");
                return original != null ? new Material(original) : null;
            }

            var mat = new Material(shader);

            if (original != null)
            {
                if (original.HasProperty("_MainTex") && mat.HasProperty("_MainTex"))
                    mat.mainTexture = original.mainTexture;

                if (original.HasProperty("_Color") && mat.HasProperty("_Color"))
                    mat.color = original.color;
            }

            ApplyArgsAutomatic(mat, args);

            return mat;
        }

        protected void StoreAndApplyMaterials(Renderer renderer, Material[] originalMats, TArgs args)
        {   // Helper to store original materials and apply custom ones
            if (renderer == null) return;

            var customMats = new Material[originalMats.Length];
            for (int i = 0; i < originalMats.Length; i++)
                customMats[i] = CreateCustomMaterial(originalMats[i], args);

            RegisterTouchedRenderer(renderer, originalMats, customMats);
        }

        protected void RegisterTouchedRenderer(Renderer renderer, Material[] originalMats, Material[] customMats)
        {   // Register a renderer/material touchpoint so restore is always deterministic.
            if (renderer == null)
                throw new ArgumentNullException(nameof(renderer));

            _originalMaterials[renderer] = originalMats;
            _customMaterials[renderer] = customMats;
        }

        protected void RegisterTouchedState(string key, Action restoreAction)
        {   // Register non-material state touched by module and restore with a stable key.
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Restore key cannot be null or whitespace.", nameof(key));
            if (restoreAction == null)
                throw new ArgumentNullException(nameof(restoreAction));

            _restoreCallbacks[key] = restoreAction;
            _isApplied = true;
        }

        private static void DestroyMaterialArray(Material[] mats)
        {
            if (mats == null)
                return;

            for (var i = 0; i < mats.Length; i++)
                if (mats[i] != null)
                    UnityEngine.Object.Destroy(mats[i]);
        }

        private void TrackRendererMapSet(bool isOriginalMap, Renderer renderer, Material[] materials)
        {
            if (renderer == null)
                throw new ArgumentNullException(nameof(renderer));

            if (isOriginalMap)
            {
                if (_originalMaterials.ContainsKey(renderer))
                    return;

                _originalMaterials.SetRaw(renderer, materials ?? Array.Empty<Material>());
                _isApplied = true;
                return;
            }

            if (!_originalMaterials.ContainsKey(renderer))
                _originalMaterials.SetRaw(renderer, renderer.sharedMaterials ?? Array.Empty<Material>());

            if (_customMaterials.TryGetValue(renderer, out var previousCustom))
                DestroyMaterialArray(previousCustom);

            var applied = materials ?? Array.Empty<Material>();
            _customMaterials.SetRaw(renderer, applied);
            renderer.sharedMaterials = applied;
            _isApplied = true;
        }

        protected abstract void ApplyArgsToMaterial(Material mat, TArgs args);

        public override void ApplyArgs(Material mat, object args)
        {
            if (args is TArgs typedArgs)
            {
                ApplyArgsAutomatic(mat, typedArgs);
                ApplyArgsToMaterial(mat, typedArgs);
            }
        }
    }

    /// Central registry: auto-discovers any IShaderModule with a public parameterless ctor.
    public static class ShaderRegistry
    {
        private static readonly Dictionary<string, IShaderModule> _byName = new(StringComparer.Ordinal);
        private static readonly object _lock = new();
        private static bool _initialized;

        public static bool IsInitialized => _initialized;

        public static void Initialize(bool force = false)
        {   // Explicitly initialize the registry; no longer auto-initialized
            lock (_lock)
            {
                if (_initialized && !force) return;
                _initialized = true;

                Debug.Log("[ShaderRegistry] Initializing shader module registry.");

                _byName.Clear();

                foreach (var t in DiscoverModuleTypes(typeof(IShaderModule)))
                {
                    if (t.GetConstructor(Type.EmptyTypes) == null) continue;

                    try
                    {
                        var instance = (IShaderModule)Activator.CreateInstance(t);
                        Register(instance);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[ShaderRegistry] Failed to create '{t.FullName}': {e}");
                    }
                }
            }
        }

        public static void Register(IShaderModule module)
        {
            if (module == null) return;
            if (string.IsNullOrWhiteSpace(module.Name))
            {
                Debug.LogWarning($"[ShaderRegistry] Skipping module with empty Name: {module.GetType().FullName}");
                return;
            }

            _byName[module.Name] = module;
        }

        public static IShaderModule Get(string name)
        {   // Only return if already initialized; never auto-initialize
            return (_initialized && name != null && _byName.TryGetValue(name, out var m)) ? m : null;
        }

        public static T Get<T>(string name) where T : class, IShaderModule
        {   // Generic typed getter for convenience
            return Get(name) as T;
        }

        public static bool TryGetReady<T>(string name, out T module, out string error) where T : class, IShaderModule
        {   // Resolve a typed module and ensure its shader is loaded before use.
            module = Get<T>(name);
            if (module == null)
            {
                error = $"Module '{name}' was not found in registry.";
                return false;
            }

            if (!module.IsLoaded || module.Shader == null)
            {
                var _ = module.Shader;
            }

            if (!module.IsLoaded || module.Shader == null)
            {
                error = $"Module '{name}' exists but its shader is not loaded.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public static IReadOnlyCollection<IShaderModule> AllModules
        {   // Only return if already initialized; never auto-initialize
            get => _initialized ? _byName.Values.ToArray() : Array.Empty<IShaderModule>();
        }

        private static IEnumerable<Type> DiscoverModuleTypes(Type target)
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(SafeGetTypes)
                .Where(t => SafeIsAssignableFrom(target, t));
        }

        private static bool SafeIsAssignableFrom(Type target, Type candidate)
        {
            if (candidate == null || candidate.IsAbstract || candidate.IsInterface || candidate.IsGenericTypeDefinition)
                return false;
            try { return target.IsAssignableFrom(candidate); }
            catch { return false; }
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly asm)
        {
            try { return asm.GetTypes(); }
            catch (ReflectionTypeLoadException e)
            {
                Debug.LogWarning($"[ShaderRegistry] Partial type load failure in assembly '{asm.FullName}': {e.Message}");
                return e.Types.Where(x => x != null);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ShaderRegistry] Failed to enumerate types in assembly '{asm.FullName}': {ex.Message}");
                return Array.Empty<Type>();
            }
        }
    }

    public enum ShaderType
    {   // Type of shader module (normal only, compute removed)
        Shader
    }

    internal sealed class ShaderModuleLiveUpdater : MonoBehaviour
    {
        public Action? Tick;

        private void Update()
        {   // Drive module live updates and self-clean when the owner callback is gone.
            if (Tick == null)
            {
                Destroy(gameObject);
                return;
            }

            Tick();
        }
    }
}



namespace shaders.Lib
{
    public static class ShaderPackManager
    {
        private sealed class PersistedPackState
        {   // Serialized state for selected pack and per-shader overrides
            public string? SelectedPackName;
            public List<PersistedShaderState> Shaders = new List<PersistedShaderState>();

            public sealed class PersistedShaderState
            {   // Serialized state container for one shader's field overrides
                public string? ShaderName;
                public List<PersistedOverrideEntry> Overrides = new List<PersistedOverrideEntry>();
            }

            public sealed class PersistedOverrideEntry
            {   // Serialized representation of one override value with type metadata
                public string? FieldPath;
                public string? TypeName;
                public string? JsonValue;
            }
        }

        private sealed class RuntimeShaderState
        {   // Runtime state for one shader's resolved args and user field overrides
            public object? CurrentArgs;
            public readonly Dictionary<string, object> UserOverrides = new Dictionary<string, object>(StringComparer.Ordinal);
        }

        private static bool _initialized;
        private static bool _cameraHookRegistered;
        private static bool _sceneHookRegistered;
        private static string? _selectedPackName;
        private static readonly Dictionary<string, RuntimeShaderState> _shaderStateByName = new Dictionary<string, RuntimeShaderState>(StringComparer.Ordinal);

        // Stored next to the mod's own DLL (like Main.UpdatableFiles' ModFolder-relative path)
        // rather than under Application.persistentDataPath, so the config is somewhere a user
        // browsing their Mods folder can actually find and edit/back up.
        private static FilePath SettingsFile => Main.modFolder.ExtendToFile("shaders.Config.txt");

        /// <summary>
        /// Raised whenever the pack list or active pack changes, including asynchronously (e.g. a
        /// pack's CodeAssembly finishes loading after this mod's own Load()). The UI subscribes so
        /// an already-open browser reflects packs/activation that resolve after it was built,
        /// instead of only refreshing in response to direct UI interaction.
        /// </summary>
        public static event Action? StateChanged;

        private static void RaiseStateChanged() => StateChanged?.Invoke();

        /// <summary>
        /// Called after <see cref="ShaderRegistry"/>/<see cref="ShaderPackRegistry"/> re-discover
        /// types (e.g. a shader pack's CodeAssembly finished loading after this mod's own Load()).
        /// Retries the persisted pack activation now that it may actually resolve, and always
        /// notifies listeners so an already-open browser reflects the newly discovered pack list.
        /// </summary>
        public static void NotifyRegistriesRefreshed()
        {
            if (!_initialized)
                return;

            if (ShaderPackRegistry.ActivePack == null)
                TryRestoreSelectedPackActivation();

            RaiseStateChanged();
        }

        public static void Initialize()
        {   // Initialize shader system: registry then packs
            if (_initialized) return;

            LoadPersistedState();
            ShaderRegistry.Initialize();
            ShaderPackRegistry.Initialize();

            if (!_cameraHookRegistered)
            {
                Camera.onPreCull += EnsureActivePackCameraBinding;
                _cameraHookRegistered = true;
            }

            if (!_sceneHookRegistered)
            {
                // SFS loads scenes additively via SceneLoader's own coroutine and only fires
                // ModLoader.Helpers.SceneHelper's events once that whole transition (including
                // Menu.loading.Close()) is done. Raw SceneManager.sceneLoaded/unloaded fire earlier
                // — while SFS's own camera/world singletons may not be wired up yet — and also fire
                // for the internal "Base_PC" bootstrap scene, which caused shader rebinds to
                // intermittently miss or run against not-yet-ready state across scene changes.
                ModLoader.Helpers.SceneHelper.OnSceneLoaded += HandleSceneLoaded;
                ModLoader.Helpers.SceneHelper.OnSceneUnloaded += HandleSceneUnloaded;
                _sceneHookRegistered = true;
            }

            ShaderRouteApi.SetCurrentScene(SceneManager.GetActiveScene().name);
            TryRestoreSelectedPackActivation();

            _initialized = true;
            Debug.Log($"[ShaderPackManager] Initialized with {ShaderPackRegistry.AllPacks.Count} packs");
        }

        private static void TryRestoreSelectedPackActivation()
        {   // Reactivate persisted pack so scene/camera bindings apply immediately after load.
            if (string.IsNullOrWhiteSpace(_selectedPackName))
                return;

            var pack = ShaderPackRegistry.Get(_selectedPackName);
            if (pack == null)
            {
                // Don't clear the persisted name here: this runs once synchronously during
                // Initialize(), before any part-pack's CodeAssembly has necessarily finished
                // loading via the async Assembly.Load(byte[]) path (see CodeAssemblyLoadHooks).
                // Clearing on this first miss would permanently discard the selection before the
                // NotifyRegistriesRefreshed() retry — fired once that pack's assembly does load —
                // ever gets a chance to find it.
                Debug.LogWarning($"[ShaderPackManager] Persisted pack '{_selectedPackName}' was not found yet; will retry once its assembly loads.");
                return;
            }

            if (!ShaderPackRegistry.SetActivePack(_selectedPackName, null))
            {   // Pack not ready yet (shaders may still be loading) — retry next camera pre-cull.
                return;
            }

            var activePack = ShaderPackRegistry.ActivePack;
            if (activePack == null)
                return;

            // Build args AFTER activation so GetShaders() returns the now-loaded modules.
            var shaderArgs = new Dictionary<string, object>(StringComparer.Ordinal);
            var loadedShaders = activePack.GetShaders();
            for (var i = 0; i < loadedShaders.Count; i++)
            {
                var shader = loadedShaders[i];
                if (shader == null || string.IsNullOrWhiteSpace(shader.Name))
                    continue;

                var resolvedArgs = GetCurrentArgs(shader.Name) ?? activePack.GetArgs(shader.Name);
                if (resolvedArgs == null)
                {   // Create default args so the shader renders correctly with proper uniform defaults.
                    var argsType = ResolveArgsType(shader);
                    if (argsType != null)
                        try { resolvedArgs = Activator.CreateInstance(argsType); } catch { }
                }

                if (resolvedArgs != null)
                {
                    shaderArgs[shader.Name] = resolvedArgs;
                    SetCurrentArgs(shader.Name, resolvedArgs);
                }
            }

            ApplyActivePackToCameras(activePack, shaderArgs);
            RaiseStateChanged();
        }

        public static string SelectedPackName => _selectedPackName;

        public static bool SelectPack(string packName, Func<IShaderModule, Type, object> argsResolver)
        {   // Select or toggle a pack using caller-provided argument resolution
            if (!_initialized) Initialize();

            if (string.IsNullOrWhiteSpace(packName))
            {
                DeactivateCurrentPack();
                return true;
            }

            if (string.Equals(_selectedPackName, packName, StringComparison.Ordinal))
            {
                DeactivateCurrentPack();
                return true;
            }

            if (argsResolver == null)
                throw new ArgumentNullException(nameof(argsResolver));

            var targetPack = ShaderPackRegistry.Get(packName);
            if (targetPack == null)
            {
                Debug.LogWarning($"[ShaderPackManager] Cannot select unknown pack '{packName}'");
                return false;
            }

            var shaderArgs = BuildShaderArgs(targetPack, argsResolver);
            if (!ShaderPackRegistry.SetActivePack(packName, shaderArgs))
            {
                Debug.LogWarning($"[ShaderPackManager] Failed to activate pack '{packName}'");
                return false;
            }

            _selectedPackName = packName;
            SavePersistedState();
            RaiseStateChanged();
            return true;
        }

        private static Dictionary<string, object> BuildShaderArgs(IShaderPack pack, Func<IShaderModule, Type, object> argsResolver)
        {   // Build full shader argument map for a pack and persist resolved args in runtime state
            var shaderArgs = new Dictionary<string, object>(StringComparer.Ordinal);
            if (pack == null) return shaderArgs;

            var shaders = pack.GetShaders();
            for (var i = 0; i < shaders.Count; i++)
            {
                var shader = shaders[i];
                var argsType = ResolveArgsType(shader);
                if (argsType == null) continue;

                var argsObject = argsResolver(shader, argsType);
                if (argsObject == null) continue;

                shaderArgs[shader.Name] = argsObject;
                SetCurrentArgs(shader.Name, argsObject);
            }

            return shaderArgs;
        }

        private static Type ResolveArgsType(IShaderModule shader)
        {   // Walk inheritance chain to find ShaderModule<TArgs, TResult> argument type
            var type = shader?.GetType();
            while (type != null)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ShaderModule<,>))
                {
                    var genericArgs = type.GetGenericArguments();
                    return genericArgs.Length == 2 ? genericArgs[0] : null;
                }

                type = type.BaseType;
            }

            return null;
        }

        public static bool ActivatePack(string packName, Dictionary<string, object> shaderArgs = null)
        {   // Activate a shader pack by name with optional arguments
            if (!_initialized) Initialize();

            var success = ShaderPackRegistry.SetActivePack(packName, shaderArgs);
            if (success)
            {
                Debug.Log($"[ShaderPackManager] Activated pack: {packName}");
                _selectedPackName = packName;
                SavePersistedState();

                var active = ShaderPackRegistry.ActivePack;
                if (active != null)
                    ApplyActivePackToCameras(active, shaderArgs);
            }
            else
                Debug.LogWarning($"[ShaderPackManager] Failed to activate pack: {packName}");

            if (success && shaderArgs != null)
            {
                foreach (var kvp in shaderArgs)
                    SetCurrentArgs(kvp.Key, kvp.Value);
            }

            if (success)
                RaiseStateChanged();

            return success;
        }

        public static void DeactivateCurrentPack()
        {   // Deactivate currently active pack
            if (!_initialized) return;

            var activePack = ShaderPackRegistry.ActivePack;
            if (activePack != null)
            {
                ShaderPackRegistry.DeactivateAll();
                Debug.Log($"[ShaderPackManager] Deactivated pack: {activePack.Name}");
            }

            _selectedPackName = null;
            var cameras = Camera.allCameras;
            for (var i = 0; i < cameras.Length; i++)
            {
                var cam = cameras[i];
                if (cam == null)
                    continue;
                var fx = cam.GetComponent<shaders.shadersOverlayEffect>();
                if (fx == null)
                    continue;
                fx.selectedShader = null;
                fx.customRenderKey = string.Empty;
                fx.renderMode = shaders.Lib.Renders.OverlayRenderMode.BehindUI;
            }

            shaders.Lib.Renders.OverlayDispatcher.ClearAllEffects();
            shaders.Lib.Renders.OverlayDispatcher.SelectedModule = null;
            shaders.Lib.Renders.OverlayDispatcher.CurrentArgs = null;
            SavePersistedState();
            RaiseStateChanged();
        }

        public static void SetSelectedPackName(string packName)
        {   // Set the selected pack name for persistence without activating
            if (!_initialized) Initialize();

            _selectedPackName = string.IsNullOrWhiteSpace(packName) ? null : packName;
            SavePersistedState();
        }

        public static void UpdatePackArgs(string shaderName, object args)
        {   // Update arguments for specific shader in active pack
            if (!_initialized) return;
            if (string.IsNullOrWhiteSpace(shaderName) || args == null) return;

            SetCurrentArgs(shaderName, args);

            var activePack = ShaderPackRegistry.ActivePack;
            if (activePack != null && activePack.IsActive)
                activePack.UpdateArgs(shaderName, args);
        }

        public static object GetCurrentArgs(string shaderName)
        {   // Read current resolved args for a shader key
            if (string.IsNullOrWhiteSpace(shaderName)) return null;
            return _shaderStateByName.TryGetValue(shaderName, out var state) ? state.CurrentArgs : null;
        }

        public static IReadOnlyDictionary<string, object> GetUserOverrides(string shaderName)
        {   // Read user overrides for a shader key
            if (string.IsNullOrWhiteSpace(shaderName)) return null;
            return _shaderStateByName.TryGetValue(shaderName, out var state) ? state.UserOverrides : null;
        }

        public static void SetCurrentArgs(string shaderName, object args)
        {   // Store resolved args for a shader key
            if (string.IsNullOrWhiteSpace(shaderName) || args == null) return;
            GetOrCreateShaderState(shaderName).CurrentArgs = args;
        }

        public static void SetUserOverride(string shaderName, string fieldPath, object value)
        {   // Track user override for a specific shader field path
            if (string.IsNullOrWhiteSpace(shaderName) || string.IsNullOrWhiteSpace(fieldPath)) return;
            GetOrCreateShaderState(shaderName).UserOverrides[fieldPath] = value;
            SavePersistedState();
        }

        public static void ClearShaderState(string shaderName)
        {   // Clear cached args and overrides for a shader key
            if (string.IsNullOrWhiteSpace(shaderName)) return;
            _shaderStateByName.Remove(shaderName);
            SavePersistedState();
        }

        public static void FlushPendingSave()
        {   // Force-flush pending settings writes to persistent storage
            SavePersistedState();
        }

        private static void LoadPersistedState()
        {   // Load user settings from disk and restore selected pack + override values
            try
            {
                var settingsFile = SettingsFile;
                if (!settingsFile.FileExists()) return;

                ApplyPersistedJson(settingsFile.ReadText());
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ShaderPackManager] Failed to load settings: {ex.Message}");
            }
        }

        private static void ApplyPersistedJson(string json)
        {
            var state = JsonConvert.DeserializeObject<PersistedPackState>(json);
            if (state == null) return;

            _selectedPackName = string.IsNullOrWhiteSpace(state.SelectedPackName) ? null : state.SelectedPackName;

            _shaderStateByName.Clear();
            if (state.Shaders == null) return;

            foreach (var shader in state.Shaders)
            {
                if (shader == null || string.IsNullOrWhiteSpace(shader.ShaderName))
                    continue;

                var runtime = GetOrCreateShaderState(shader.ShaderName);
                runtime.UserOverrides.Clear();

                if (shader.Overrides == null) continue;

                foreach (var entry in shader.Overrides)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.FieldPath))
                        continue;

                    object value;
                    try
                    {
                        var type = !string.IsNullOrWhiteSpace(entry.TypeName) ? Type.GetType(entry.TypeName) : null;
                        value = type != null
                            ? JsonConvert.DeserializeObject(entry.JsonValue, type)
                            : JsonConvert.DeserializeObject(entry.JsonValue);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[ShaderPackManager] Failed to deserialize override '{entry.FieldPath}' (type '{entry.TypeName ?? "unknown"}'): {ex.Message}");
                        continue;
                    }

                    if (value != null)
                        runtime.UserOverrides[entry.FieldPath] = value;
                }
            }
        }

        private static void SavePersistedState()
        {   // Persist selected pack and user overrides to disk
            try
            {
                var state = new PersistedPackState
                {
                    SelectedPackName = _selectedPackName,
                    Shaders = _shaderStateByName
                        .Where(kvp => kvp.Value != null && kvp.Value.UserOverrides.Count > 0)
                        .Select(kvp => new PersistedPackState.PersistedShaderState
                        {
                            ShaderName = kvp.Key,
                            Overrides = kvp.Value.UserOverrides
                                .Where(ov => !string.IsNullOrWhiteSpace(ov.Key) && ov.Value != null)
                                .Select(ov => new PersistedPackState.PersistedOverrideEntry
                                {
                                    FieldPath = ov.Key,
                                    TypeName = ov.Value.GetType().AssemblyQualifiedName,
                                    JsonValue = JsonConvert.SerializeObject(ov.Value)
                                })
                                .ToList()
                        })
                        .Where(s => s.Overrides.Count > 0)
                        .ToList()
                };

                var json = JsonConvert.SerializeObject(state, Formatting.Indented);
                SettingsFile.WriteText(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ShaderPackManager] Failed to save settings: {ex.Message}");
            }
        }

        private static RuntimeShaderState GetOrCreateShaderState(string shaderName)
        {   // Get existing runtime shader state or create a new one
            if (!_shaderStateByName.TryGetValue(shaderName, out var state))
            {
                state = new RuntimeShaderState();
                _shaderStateByName[shaderName] = state;
            }

            return state;
        }

        private static Dictionary<string, object> BuildResolvedArgsMap(IShaderPack pack, IReadOnlyCollection<IShaderModule> modules, Dictionary<string, object> shaderArgs = null)
        {   // Resolve manager-authoritative args per module (runtime current args first, then pack defaults, then constructed defaults).
            var resolved = new Dictionary<string, object>(StringComparer.Ordinal);
            if (pack == null || modules == null)
                return resolved;

            foreach (var module in modules)
            {
                if (module == null || string.IsNullOrWhiteSpace(module.Name))
                    continue;

                object args = null;
                if (shaderArgs != null && shaderArgs.TryGetValue(module.Name, out var mapped))
                    args = mapped;

                args ??= GetCurrentArgs(module.Name) ?? pack.GetArgs(module.Name);

                if (args == null)
                {   // Create default args instance so the shader renders with correct defaults on startup.
                    var argsType = ResolveArgsType(module);
                    if (argsType != null)
                    {
                        try { args = Activator.CreateInstance(argsType); }
                        catch { }
                    }
                }

                if (args == null)
                    continue;

                resolved[module.Name] = args;
                SetCurrentArgs(module.Name, args);
            }

            return resolved;
        }

        private static void BindCameraForPack(Camera cam, IReadOnlyCollection<IShaderModule> modules, IReadOnlyDictionary<string, object> resolvedArgs)
        {   // Bind or clear exactly one camera based on current scene route eligibility.
            if (cam == null || cam.gameObject == null)
                return;

            var module = modules.FirstOrDefault(m => ShaderRouteApi.IsEligible(m, cam));
            if (module == null)
            {
                shaders.Lib.Renders.OverlayDispatcher.ClearCameraEffect(cam);
                return;
            }

            object args = null;
            resolvedArgs?.TryGetValue(module.Name, out args);

            var mode = module.GetType().GetCustomAttribute<ShaderModuleAttribute>()?.RenderTarget
                ?? shaders.Lib.Renders.OverlayRenderMode.BehindUI;

            ApplyModuleToCamera(cam, module, mode, args);
        }

        private static void ApplyActivePackToCameras(IShaderPack pack, Dictionary<string, object> shaderArgs = null)
        {   // Sync active module shader + args + render mode onto all cameras with overlay effect.
            if (pack == null)
                return;

            ShaderRouteApi.SetCurrentScene(SceneManager.GetActiveScene().name);

            var modules = pack.GetShaders()?.Where(s => s != null && s.Shader != null).ToArray();
            if (modules == null || modules.Length == 0)
            {
                Debug.LogWarning("[ShaderPackManager] Active pack has no loaded shader modules to apply to cameras.");
                return;
            }

            var resolvedArgs = BuildResolvedArgsMap(pack, modules, shaderArgs);

            var cameras = Camera.allCameras;
            for (var i = 0; i < cameras.Length; i++)
            {
                var cam = cameras[i];
                if (cam == null || cam.gameObject == null)
                    continue;

                BindCameraForPack(cam, modules, resolvedArgs);
            }

            Debug.Log($"[ShaderPackManager] Evaluated shader routing for {cameras.Length} camera(s) in scene '{ShaderRouteApi.CurrentScene}'.");
        }

        private static void EnsureActivePackCameraBinding(Camera cam)
        {   // Keep newly available cameras synced to currently active shader pack.
            if (!_initialized || cam == null)
                return;

            ShaderRouteApi.SetCurrentScene(SceneManager.GetActiveScene().name);

            var pack = EnsureActivePackAvailable();
            var modules = pack?.GetShaders()?.Where(s => s != null && s.Shader != null).ToArray();
            if (modules == null || modules.Length == 0)
            {
                shaders.Lib.Renders.OverlayDispatcher.ClearCameraEffect(cam);
                return;
            }

            var resolvedArgs = BuildResolvedArgsMap(pack, modules);
            BindCameraForPack(cam, modules, resolvedArgs);
        }

        private static void ApplyModuleToCamera(Camera cam, IShaderModule module, shaders.Lib.Renders.OverlayRenderMode mode, object args)
        {
            if (cam == null || module == null || cam.gameObject == null)
                return;

            var fx = cam.GetComponent<shaders.shadersOverlayEffect>() ?? cam.gameObject.AddComponent<shaders.shadersOverlayEffect>();
            fx.renderMode = mode;
            fx.selectedShader = module.Shader;
            fx.customRenderKey = module.Name;

            shaders.Lib.Renders.OverlayDispatcher.SelectedModule = module;
            shaders.Lib.Renders.OverlayDispatcher.CurrentArgs = args;
            shaders.Lib.Renders.OverlayDispatcher.ForceRefresh(cam);
        }

        public static void RebindActivePackToCameras()
        {   // Re-evaluate routes and reapply active-pack camera bindings without resetting runtime args.
            if (!_initialized)
                return;

            ShaderRouteApi.SetCurrentScene(SceneManager.GetActiveScene().name);
            var active = EnsureActivePackAvailable();
            if (active == null)
            {
                shaders.Lib.Renders.OverlayDispatcher.ClearAllEffects(clearSharedState: false);
                return;
            }

            ApplyActivePackToCameras(active);
        }

        private static void HandleSceneLoaded(Scene scene)
        {
            ShaderRouteApi.SetCurrentScene(scene.name);
            RebindActivePackToCameras();
        }

        private static IShaderPack EnsureActivePackAvailable()
        {   // Restore persisted selection if the active pack was lost across lifecycle transitions.
            var active = ShaderPackRegistry.ActivePack;
            if (active != null)
                return active;

            if (string.IsNullOrWhiteSpace(_selectedPackName))
                return null;

            TryRestoreSelectedPackActivation();
            return ShaderPackRegistry.ActivePack;
        }

        private static void HandleSceneUnloaded(Scene scene)
        {
            ShaderRouteApi.SetCurrentScene(SceneManager.GetActiveScene().name);
            shaders.Lib.Renders.OverlayDispatcher.ClearAllEffects(clearSharedState: false);
        }

        public static IShaderPack GetActivePack() => 
            _initialized ? ShaderPackRegistry.ActivePack : null;

        public static IReadOnlyCollection<IShaderPack> GetAllPacks() =>
            _initialized ? ShaderPackRegistry.AllPacks : Array.Empty<IShaderPack>();

        public static IReadOnlyCollection<string> GetAvailablePackNames() =>
            GetAllPacks().Select(p => p.Name).ToArray();

        public static bool IsPackActive(string packName)
        {   // Check if a specific pack is currently active
            var activePack = GetActivePack();
            return activePack != null && activePack.Name == packName;
        }
    }

    // Public: shader-pack code is compiled separately (outside this mod assembly, see
    // sfspack's code_assembly step / PackLoader docs below) and needs access to these
    // helpers from its own CodeAssembly DLL.
    public static class SfsWorldUtils
    {
        public static Camera? ResolveActiveCamera()
        {
            try
            {
                if (ActiveCamera.Camera?.camera != null)
                    return ActiveCamera.Camera.camera;
            }
            catch { }

            if (Camera.main != null)
                return Camera.main;

            return Camera.allCamerasCount > 0 ? Camera.allCameras[0] : null;
        }

        public static bool ResolveScaledSpace(Camera? activeCamera)
        {
            try
            {
                var view = WorldView.main;
                if (view != null)
                    return view.scaledSpace.Value;
            }
            catch { }

            try
            {
                var cameraManager = GameCamerasManager.main;
                if (cameraManager?.scaledWorld_Camera?.camera != null)
                    return cameraManager.scaledWorld_Camera.camera == activeCamera;
            }
            catch { }

            return false;
        }

        public static Planet? ResolvePlayerPlanet(bool allowCameraFallback = true)
        {
            var viewPlanet = TryResolveViewPlanet();
            if (viewPlanet != null)
                return viewPlanet;

            try
            {
                var player = PlayerController.main?.player?.Value;
                var playerPlanet = player?.location?.planet?.Value;
                if (playerPlanet != null)
                    return playerPlanet;
            }
            catch { }

            if (!allowCameraFallback)
                return null;

            var cameraPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            return UnityEngine.Object.FindObjectsOfType<Planet>()
                .Where(p => p != null)
                .OrderBy(p => (p.transform.position - cameraPos).sqrMagnitude)
                .FirstOrDefault();
        }

        public static Planet? TryResolveViewPlanet()
        {
            var view = WorldView.main;
            var viewLocation = view?.ViewLocation;
            if (viewLocation == null)
                return null;

            var locationType = viewLocation.GetType();
            var planetMember = (object?)locationType.GetProperty("planet", BindingFlags.Public | BindingFlags.Instance)?.GetValue(viewLocation)
                ?? locationType.GetField("planet", BindingFlags.Public | BindingFlags.Instance)?.GetValue(viewLocation);
            if (planetMember == null)
                return null;

            if (planetMember is Planet directPlanet)
                return directPlanet;

            var wrappedType = planetMember.GetType();
            return (Planet?)wrappedType.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)?.GetValue(planetMember)
                ?? wrappedType.GetField("Value", BindingFlags.Public | BindingFlags.Instance)?.GetValue(planetMember) as Planet;
        }

        public static string ResolvePlanetCode(Planet? planet, Camera? activeCamera = null)
        {
            var direct = planet?.codeName ?? planet?.name;
            if (!string.IsNullOrWhiteSpace(direct))
                return direct;

            if (activeCamera == null)
                return string.Empty;

            var cameraPosition = activeCamera.transform.position;
            var nearestPlanet = UnityEngine.Object.FindObjectsOfType<Planet>()
                .Where(p => p != null)
                .OrderBy(p => (p.transform.position - cameraPosition).sqrMagnitude)
                .FirstOrDefault();

            return nearestPlanet?.codeName ?? nearestPlanet?.name ?? string.Empty;
        }

        public static Vector3 ResolvePlanetCenterRenderSpace(Planet? planet)
        {
            if (planet == null)
                return Vector3.zero;

            try
            {
                var view = WorldView.main;
                if (view == null || view.ViewLocation == null || view.ViewLocation.planet == null)
                    return planet.transform.position;

                var time = WorldTime.main != null ? WorldTime.main.worldTime : 0.0;
                var targetSolar = planet.GetSolarSystemPosition(time);
                var viewPlanet = view.ViewLocation.planet;
                var viewSolar = viewPlanet.GetSolarSystemPosition(time);
                var delta = targetSolar - viewSolar;

                if (view.scaledSpace.Value)
                    return new Vector3((float)(delta.x / WorldView.ScaledSpaceScale), (float)(delta.y / WorldView.ScaledSpaceScale), 0f);

                var local = WorldView.ToLocalPosition(delta);
                return new Vector3(local.x, local.y, 0f);
            }
            catch
            {
                return planet.transform.position;
            }
        }

        public static float ResolvePlanetRadius(Planet? planet, bool scaledSpace)
        {
            if (planet == null)
                return 1f;

            try
            {
                var radius = (float)planet.Radius;
                if (radius <= 0f)
                    return 1f;

                return scaledSpace ? radius / WorldView.ScaledSpaceScale : radius;
            }
            catch
            {
                return 1f;
            }
        }

        public static float ResolvePlanetRotationDeg(Planet? planet)
        {
            if (planet == null)
                return 0f;

            try { return planet.transform.eulerAngles.z; }
            catch { return 0f; }
        }

        public static float ResolvePlanetTextureCutout(Planet? planet)
        {
            if (planet == null)
                return 1f;

            try
            {
                var terrain = planet.data?.terrain;
                if (terrain == null)
                    return 1f;

                var terrainType = terrain.GetType();
                var textureDataObj = terrainType.GetField("TERRAIN_TEXTURE_DATA", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(terrain)
                    ?? terrainType.GetProperty("TERRAIN_TEXTURE_DATA", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(terrain);
                if (textureDataObj == null)
                    return 1f;

                var textureDataType = textureDataObj.GetType();
                var cutoutObj = textureDataType.GetField("planetTextureCutout", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(textureDataObj)
                    ?? textureDataType.GetProperty("planetTextureCutout", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(textureDataObj);

                if (cutoutObj is float cutoutFloat && cutoutFloat > 0f)
                    return cutoutFloat;

                if (cutoutObj is double cutoutDouble && cutoutDouble > 0.0)
                    return (float)cutoutDouble;
            }
            catch { }

            return 1f;
        }

        public static float ResolveDontDistortTextureCutout(Planet? planet)
        {
            if (planet == null)
                return 0f;

            try
            {
                var planetType = planet.GetType();
                var prop = planetType.GetProperty("DontDistortTextureCutout", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null && prop.GetValue(planet) is bool propValue)
                    return propValue ? 1f : 0f;

                var field = planetType.GetField("DontDistortTextureCutout", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && field.GetValue(planet) is bool fieldValue)
                    return fieldValue ? 1f : 0f;
            }
            catch { }

            return 0f;
        }

        public static Vector3 ResolveSunDirection(Planet? playerPlanet)
        {
            var fallback = Vector3.up;
            var sunPlanet = UnityEngine.Object.FindObjectsOfType<Planet>()
                .FirstOrDefault(p => string.Equals(p?.codeName, "Sun", StringComparison.Ordinal));

            if (sunPlanet == null || playerPlanet == null)
                return fallback;

            try
            {
                var sunLoc = sunPlanet.GetLocation(WorldTime.main.worldTime);
                var planetLoc = playerPlanet.GetLocation(WorldTime.main.worldTime);
                var dir = (planetLoc.position - sunLoc.position).normalized;
                return dir.sqrMagnitude > 0f ? dir : fallback;
            }
            catch { return fallback; }
        }

        public static Color ResolveSunColor()
        {
            var sunPlanet = UnityEngine.Object.FindObjectsOfType<Planet>()
                .FirstOrDefault(p => string.Equals(p?.codeName, "Sun", StringComparison.Ordinal));

            if (sunPlanet == null)
                return new Color(1f, 0.95f, 0.9f, 1f);

            try
            {
                var fog = sunPlanet.data?.atmosphereVisuals?.FOG;
                if (fog?.keys?.Length > 0)
                    return fog.Evaluate(fog.keys[0].distance);
            }
            catch { }

            return new Color(1f, 0.95f, 0.9f, 1f);
        }
    }

    // Back-compat: keep your existing static registries.
    public static class Patches
    {
        private static bool _applied;

        public static void ApplyAll()
        {   // Apply all Harmony patches for the library
            if (_applied) return;

            var harmony = new Harmony("shaders.AllPatches");

            OverlaySortingPatches.Apply(harmony);
            SfsPartShaderPatches.Apply(harmony);

            AssetBundleLoadHooks.Apply(harmony);
            CodeAssemblyLoadHooks.Apply(harmony);
            KeyboardInputBlockPatches.Apply(harmony);

            _applied = true;
        }
    }

    /// <summary>
    /// Blocks the game's own keybindings from firing while the user is typing in one of our config
    /// text fields (search box or a shader parameter input) — otherwise typing e.g. "2" into a
    /// number field also fires whatever action is bound to key "2" in-game. Same approach BP-Editor
    /// uses (github.com/uneven-coder/BP-Editor): patch SFS.Input.KeybindingsPC.Key's I_Key.IsKeyDown
    /// /IsKeyStay implementations directly, since that's the shared low-level check every keybind in
    /// the game ultimately goes through, rather than trying to intercept each bound action.
    /// </summary>
    internal static class KeyboardInputBlockPatches
    {
        private static bool _installed;

        public static void Apply(Harmony harmony)
        {
            if (_installed) return;
            _installed = true;

            try
            {
                var keyType = AccessTools.Inner(typeof(SFS.Input.KeybindingsPC), "Key");
                if (keyType == null)
                {
                    Debug.LogWarning("[KeyboardInputBlockPatches] KeybindingsPC.Key not found; typing-block patch skipped.");
                    return;
                }

                var prefix = new HarmonyMethod(typeof(KeyboardInputBlockPatches), nameof(BlockWhileTyping));
                var map = keyType.GetInterfaceMap(typeof(SFS.Input.I_Key));
                foreach (var target in map.TargetMethods)
                {
                    if (target.Name.Contains("IsKeyDown") || target.Name.Contains("IsKeyStay"))
                        harmony.Patch(target, prefix);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[KeyboardInputBlockPatches] Failed to install typing-block patch: {ex.Message}");
            }
        }

        private static bool BlockWhileTyping() => !GeneratedUI.GeneratedUiController.IsTyping;
    }

    /// <summary>
    /// Part packs (Mods/Custom_Assets/Parts/*.pack) can embed a compiled C# CodeAssembly that
    /// SFS.Parts.CustomAssetsPacksLoader loads natively via Assembly.Load(byte[]) — this is how
    /// a shader pack ships its own IShaderModule/IShaderPack classes without ever being compiled
    /// into this mod. ShaderRegistry/ShaderPackRegistry only discover types once (guarded by
    /// _initialized) at ShaderPackManager.Initialize(), which runs during this mod's own Load();
    /// since pack loading happens on its own async timeline and may finish later, this patches
    /// the low-level Assembly.Load(byte[]) entrypoint directly (matching the AssetBundleLoadHooks
    /// pattern below) so newly loaded shader-pack code is always picked up, regardless of order.
    /// </summary>
    internal static class CodeAssemblyLoadHooks
    {
        private static bool _installed;

        public static void Apply(Harmony harmony)
        {
            if (_installed) return;
            _installed = true;

            var loadMethod = AccessTools.Method(typeof(Assembly), nameof(Assembly.Load), new[] { typeof(byte[]) });
            if (loadMethod != null)
                harmony.Patch(loadMethod, postfix: new HarmonyMethod(typeof(CodeAssemblyLoadHooks), nameof(Load_Postfix)));
        }

        public static void Load_Postfix()
        {
            try
            {
                ShaderRegistry.Initialize(force: true);
                ShaderPackRegistry.Initialize();
                ShaderPackManager.NotifyRegistriesRefreshed();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CodeAssemblyLoadHooks] Failed to refresh shader registries after Assembly.Load: {ex.Message}");
            }
        }
    }

    internal static class AssetBundleLoadHooks
    {
        private static readonly HashSet<MethodBase> _patched = new();
        private static bool _installed;

        private static FieldInfo[]? _shaderFields;

        public static void Apply(Harmony harmony)
        {   // Patch all AssetBundle load entrypoints using reflection
            if (_installed) return;
            _installed = true;

            _shaderFields = typeof(ShaderRegistry).GetFields(BindingFlags.Public | BindingFlags.Static);

            var abType =
                Type.GetType("UnityEngine.AssetBundle, UnityEngine.AssetBundleModule")
                ?? typeof(UnityEngine.Object).Assembly.GetType("UnityEngine.AssetBundle");

            if (abType == null) return;

            var methods = abType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < methods.Length; i++)
            {
                var m = methods[i];
                if (m == null) continue;

                if (!m.Name.StartsWith("LoadFrom", StringComparison.Ordinal)) continue;

                var rt = m.ReturnType;
                if (rt.FullName != "UnityEngine.AssetBundle" && rt.FullName != "UnityEngine.AssetBundleCreateRequest") continue;

                if (_patched.Add(m))
                    harmony.Patch(m, postfix: new HarmonyMethod(typeof(AssetBundleLoadHooks), nameof(LoadFrom_Postfix)));
            }
        }

        public static void LoadFrom_Postfix(object __result)
        {   // Handle AssetBundle and AssetBundleCreateRequest via reflection
            if (__result == null) return;

            var abType = Type.GetType("UnityEngine.AssetBundle, UnityEngine.AssetBundleModule");
            var abReqType = Type.GetType("UnityEngine.AssetBundleCreateRequest, UnityEngine.AssetBundleModule");

            if (abType != null && abType.IsInstanceOfType(__result))
            {
                ScanRegisterAndBind_Reflection(__result, abType);
                return;
            }

            if (abReqType != null && abReqType.IsInstanceOfType(__result))
            {
                var req = __result;
                var isDoneProp = abReqType.GetProperty("isDone");
                var assetBundleProp = abReqType.GetProperty("assetBundle");
                var completedEvent = abReqType.GetEvent("completed");

                if (isDoneProp != null && (bool)isDoneProp.GetValue(req))
                {
                    if (assetBundleProp != null)
                        ScanRegisterAndBind_Reflection(assetBundleProp.GetValue(req), abType);
                }
                else if (completedEvent != null)
                {
                    Action<AsyncOperation> handler = op => OnBundleCreateCompleted(op);
                    var handlerDelegate = Delegate.CreateDelegate(completedEvent.EventHandlerType, handler.Target, handler.Method);
                    completedEvent.AddEventHandler(req, handlerDelegate);
                }
            }
        }

        private static void OnBundleCreateCompleted(AsyncOperation op)
        {   // Reflection: get assetBundle from AssetBundleCreateRequest
            var abReqType = Type.GetType("UnityEngine.AssetBundleCreateRequest, UnityEngine.AssetBundleModule");
            var abType = Type.GetType("UnityEngine.AssetBundle, UnityEngine.AssetBundleModule");
            if (abReqType == null || op == null || !abReqType.IsInstanceOfType(op)) return;

            var assetBundleProp = abReqType.GetProperty("assetBundle");
            if (assetBundleProp != null)
                ScanRegisterAndBind_Reflection(assetBundleProp.GetValue(op), abType);
        }

        private static void ScanRegisterAndBind_Reflection(object bundleObj, Type abType)
        {   // Use reflection to call LoadAllAssets<Shader>()
            if (bundleObj == null || abType == null || !abType.IsInstanceOfType(bundleObj)) return;

            var loadAllAssetsShader = abType.GetMethod("LoadAllAssets", new Type[] { typeof(Type) });
            if (loadAllAssetsShader == null) return;

            Shader[] foundShaders = Array.Empty<Shader>();

            try
            {
                var shaderObjs = loadAllAssetsShader.Invoke(bundleObj, new object[] { typeof(Shader) }) as UnityEngine.Object[];
                if (shaderObjs != null)
                    foundShaders = Array.ConvertAll(shaderObjs, o => o as Shader);
            }
            catch { }

            var anyRegistered = false;
            for (int i = 0; i < foundShaders.Length; i++)
            {
                var s = foundShaders[i];
                if (!s) continue;
                ShaderAssetRegistry.Register(s);
                anyRegistered = true;
            }

            BindShaderFields(foundShaders);

            if (anyRegistered)
            {   // Shaders may now satisfy a previously blocked pack — trigger immediate rebind rather than waiting for camera pre-cull.
                try
                {
                    ShaderPackManager.RebindActivePackToCameras();
                }
                catch { }
            }
        }

        private static void BindShaderFields(Shader[] shaders)
        {
            if (shaders == null || shaders.Length == 0 || _shaderFields == null) return;

            for (int f = 0; f < _shaderFields.Length; f++)
            {
                var field = _shaderFields[f];
                if (field == null || field.FieldType != typeof(Shader)) continue;

                var fieldName = field.Name;

                for (int i = 0; i < shaders.Length; i++)
                {
                    var sh = shaders[i];
                    if (!sh) continue;

                    // Shader.name is ShaderLab name (often "Category/Sub/Name").
                    // Match against full name, last segment, and "Shader" suffix variations.
                    if (ShaderFieldMatches(fieldName, sh.name))
                    {
                        field.SetValue(null, sh);
                        break;
                    }
                }
            }
        }

        private static bool ShaderFieldMatches(string fieldName, string shaderLabName)
        {
            if (string.IsNullOrEmpty(fieldName) || string.IsNullOrEmpty(shaderLabName))
                return false;

            // 1) exact match vs full shader name
            if (string.Equals(fieldName, shaderLabName, StringComparison.OrdinalIgnoreCase))
                return true;

            // 2) match vs last segment after '/'
            var last = ShaderAssetRegistry.LastSegment(shaderLabName);
            if (string.Equals(fieldName, last, StringComparison.OrdinalIgnoreCase))
                return true;

            // 3) allow field name to have "Shader" suffix (ExampleShader -> Example)
            var trimmed = TrimSuffix(fieldName, "Shader");
            if (!string.Equals(trimmed, fieldName, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(trimmed, shaderLabName, StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(trimmed, last, StringComparison.OrdinalIgnoreCase)) return true;
            }

            // 4) allow shader name to end with field name (Hidden/ExampleShader)
            return shaderLabName.EndsWith(fieldName, StringComparison.OrdinalIgnoreCase);
        }

        private static string TrimSuffix(string s, string suffix)
            => s.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) ? s[..^suffix.Length] : s;
    }

    internal static class OverlaySortingPatches
    {
        private static PropertyInfo? _tiMaterialProp;
        private static FieldInfo? _tiMaterialField;
        private static PropertyInfo? _tiMaterialForRenderingProp;
        private static Type? _tiType;
        private static readonly int _blurTexPropId = Shader.PropertyToID("_BlurTex");
        private static readonly int _cropRegionPropId = Shader.PropertyToID("_CropRegion");

        public static void Apply(Harmony harmony)
        {   // Patch overlay sorting and TranslucentImage for UI shader override
            var rsmType = AccessTools.TypeByName("SFS.RenderSortingManager");
            var getQueue = AccessTools.Method(rsmType, "GetRenderQueue");
            if (getQueue != null)
                harmony.Patch(getQueue, postfix: new HarmonyMethod(typeof(OverlaySortingPatches), nameof(GetRenderQueue_Postfix)));

            var rsmModuleType = AccessTools.TypeByName("SFS.RenderSortingModule");
            var startMethod = AccessTools.Method(rsmModuleType, "Start");
            if (startMethod != null)
                harmony.Patch(startMethod, postfix: new HarmonyMethod(typeof(OverlaySortingPatches), nameof(RenderSortingModule_Start_Postfix)));

            _tiType = AccessTools.TypeByName("TranslucentImage.TranslucentImage");
            if (_tiType != null)
            {
                _tiMaterialForRenderingProp = _tiType.GetProperty("materialForRendering", BindingFlags.Public | BindingFlags.Instance);
                _tiMaterialProp = _tiType.GetProperty("material", BindingFlags.Public | BindingFlags.Instance);
                if (_tiMaterialProp == null)
                    _tiMaterialField = _tiType.GetField("material", BindingFlags.NonPublic | BindingFlags.Instance);

                var lateUpdate = AccessTools.Method(_tiType, "LateUpdate");
                if (lateUpdate != null)
                    harmony.Patch(lateUpdate, prefix: new HarmonyMethod(typeof(OverlaySortingPatches), nameof(TranslucentImage_LateUpdate_Prefix)));
            }
        }

        public static void GetRenderQueue_Postfix(ref int __result, string layer)
        {
            if (layer == "OverlayOnTop") __result = 32767;
            else if (layer == "UIOverlayInclusive") __result = 3100;
        }

        public static void RenderSortingModule_Start_Postfix(object __instance)
        {
            var selectedLayerField = AccessTools.Field(__instance.GetType(), "selectedLayer");
            var renderQueueField = AccessTools.Field(__instance.GetType(), "renderQueue");

            var selectedLayer = selectedLayerField?.GetValue(__instance) as string;
            if (renderQueueField == null) return;

            if (selectedLayer == "OverlayOnTop") renderQueueField.SetValue(__instance, 32767);
            else if (selectedLayer == "UIOverlayInclusive") renderQueueField.SetValue(__instance, 3100);
        }

        public static bool TranslucentImage_LateUpdate_Prefix(object __instance)
        {   // Patch TranslucentImage.LateUpdate for exclusive rendering by setting the processed render texture as _BlurTex and _CropRegion

            var matProp = _tiMaterialForRenderingProp ?? _tiMaterialProp;
            var mat = matProp?.GetValue(__instance) as Material ?? _tiMaterialField?.GetValue(__instance) as Material;
            if (mat == null)
                return true;

            var shader = CurrentUiShader.Value;
            Texture uiTex = Exclusive_Render.UiBlurOverrideTexture;

            if (!shader || !uiTex)
                return true;

            // Set the processed render texture as the blur background for the UI shader
            mat.SetTexture(_blurTexPropId, uiTex);

            // Fullscreen processed texture => full crop region
            if (mat.HasProperty(_cropRegionPropId))
                mat.SetVector(_cropRegionPropId, new Vector4(0f, 0f, 1f, 1f));

            // Mark UI material dirty so CanvasRenderer updates
            var t = __instance.GetType();
            while (t != null)
            {
                var m = t.GetMethod("SetMaterialDirty", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (m != null) { m.Invoke(__instance, null); break; }
                t = t.BaseType;
            }

            return false;
        }
    }

    internal static class SfsPartShaderPatches
    {
        private const string SfsPartShaderName = "SFS/Part";

        private static readonly int ColorTexId = Shader.PropertyToID("_ColorTex");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int ColorMultId = Shader.PropertyToID("_ColorMult");

        public static void Apply(Harmony harmony)
        {
            var materialType = typeof(Material);

            var setTexture = AccessTools.Method(materialType, nameof(Material.SetTexture), new[] { typeof(int), typeof(Texture) });
            if (setTexture != null)
                harmony.Patch(setTexture, postfix: new HarmonyMethod(typeof(SfsPartShaderPatches), nameof(Material_SetTexture_Postfix)));

            var getTexture = AccessTools.Method(materialType, nameof(Material.GetTexture), new[] { typeof(int) });
            if (getTexture != null)
                harmony.Patch(getTexture, postfix: new HarmonyMethod(typeof(SfsPartShaderPatches), nameof(Material_GetTexture_Postfix)));
        }

        public static void Material_SetTexture_Postfix(Material __instance, int nameID, Texture value)
        {
            var sh = __instance?.shader;
            if (sh == null || sh.name != SfsPartShaderName) return;

            if (nameID == ColorTexId || nameID == MainTexId)
            {
                if (__instance.HasProperty(ColorMultId))
                    __instance.SetVector(ColorMultId, Vector4.one);
            }
        }

        public static void Material_GetTexture_Postfix(Material __instance, int nameID, ref Texture __result)
        {
            var sh = __instance?.shader;
            if (sh == null || sh.name != SfsPartShaderName) return;

            if ((nameID == ColorTexId || nameID == MainTexId) && __result == null)
                __result = Texture2D.whiteTexture;
        }
    }
}

namespace shaders.Lib.ShaderModules.ShaderPack
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ShaderPackAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public string Author { get; }
        public string Version { get; }
        public string UpdateUrl { get; }

        public ShaderPackAttribute(string name, string description = "", string author = "Unknown", string version = "1.0", string updateUrl = "")
        {   // Attribute for shader pack metadata
            Name = name;
            Description = description;
            Author = author;
            Version = version;
            UpdateUrl = updateUrl;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public sealed class ShaderRequirementAttribute : Attribute
    {
        public enum FailCondition { None, WarnOnly, DisablePack }

        public Type ShaderModuleType { get; }
        public FailCondition Condition { get; }
        public string CustomName { get; }

        public ShaderRequirementAttribute(Type shaderModuleType, FailCondition condition = FailCondition.DisablePack, string customName = null)
        {   // Declares a shader module dependency for the pack
            ShaderModuleType = shaderModuleType;
            Condition = condition;
            CustomName = customName;
        }

        public IShaderModule ResolveModule()
        {   // Resolve module instance from registry using type metadata
            if (ShaderModuleType == null) return null;

            var attr = ShaderModuleType.GetCustomAttribute<ShaderModuleAttribute>();
            return attr != null ? ShaderRegistry.Get(attr.Name) : null;
        }
    }

    public interface IShaderPack
    {
        string Name { get; }
        bool IsActive { get; }
        bool CanActivate { get; }
        
        void Activate(Dictionary<string, object> shaderArgs = null);
        void Deactivate();
        void UpdateArgs(string shaderName, object args);
        object GetArgs(string shaderName);
        IReadOnlyList<IShaderModule> GetShaders();
    }

    public abstract class ShaderPackBase : IShaderPack
    {
        protected readonly Dictionary<Type, IShaderModule> _modulesByType = new Dictionary<Type, IShaderModule>();
        protected readonly Dictionary<string, object> _shaderArgs = new Dictionary<string, object>();
        protected bool _isActive;
        private ShaderPackAttribute _packAttribute;
        private List<ShaderRequirementAttribute> _requirements;

        public string Name => _packAttribute?.Name ?? GetType().Name;
        public bool IsActive => _isActive;

        public bool CanActivate
        {
            get
            {   // Always re-evaluate requirements with fresh shader loading status
                LoadRequirements();
                return _requirements.All(req => IsRequirementSatisfied(req, ensureShaderLoaded: true));
            }
        }

        protected ShaderPackBase()
        {
            _packAttribute = GetType().GetCustomAttribute<ShaderPackAttribute>();
            LoadRequirements();
        }

        private void LoadRequirements()
        {
            if (_requirements != null) return;

            _requirements = new List<ShaderRequirementAttribute>();
            var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var attrs = field.GetCustomAttributes<ShaderRequirementAttribute>();
                _requirements.AddRange(attrs);
            }
        }

        private static bool IsRequirementSatisfied(ShaderRequirementAttribute requirement, bool ensureShaderLoaded)
        {
            var module = requirement.ResolveModule();
            if (module == null)
                return requirement.Condition != ShaderRequirementAttribute.FailCondition.DisablePack;

            var shader = ensureShaderLoaded ? module.Shader : null;
            var isLoaded = module.IsLoaded && (!ensureShaderLoaded || shader != null);
            return requirement.Condition != ShaderRequirementAttribute.FailCondition.DisablePack || isLoaded;
        }

        public virtual void Activate(Dictionary<string, object> shaderArgs = null)
        {   // Activate pack with fresh shader availability checks
            if (_isActive) return;
            
            // Re-check requirements at activation time to ensure shaders are loaded
            LoadRequirements();
            var canActivateNow = _requirements.All(req => IsRequirementSatisfied(req, ensureShaderLoaded: true));

            if (!canActivateNow)
            {
                Debug.LogWarning($"[ShaderPack] Cannot activate '{Name}' - missing required shaders");
                return;
            }

            _modulesByType.Clear();
            _shaderArgs.Clear();

            foreach (var req in _requirements)
            {
                var module = req.ResolveModule();
                if (module == null)
                {
                    if (req.Condition == ShaderRequirementAttribute.FailCondition.WarnOnly)
                        Debug.LogWarning($"[ShaderPack] Optional shader '{req.CustomName ?? req.ShaderModuleType?.Name}' not found");
                    continue;
                }

                // Verify shader is actually loaded before proceeding
                var shader = module.Shader;
                if (shader == null && req.Condition != ShaderRequirementAttribute.FailCondition.WarnOnly)
                {
                    Debug.LogWarning($"[ShaderPack] Required shader '{req.CustomName ?? req.ShaderModuleType?.Name}' failed to load");
                    return;
                }

                _modulesByType[req.ShaderModuleType] = module;

                var moduleName = module.Name;
                if (shaderArgs != null && shaderArgs.TryGetValue(moduleName, out var args))
                {
                    _shaderArgs[moduleName] = args;
                    
                    // Apply arguments to object-based modules immediately
                    var objModuleMethod = module.GetType().GetMethod("UpdateArgs", BindingFlags.Public | BindingFlags.Instance);
                    if (objModuleMethod != null)
                        objModuleMethod.Invoke(module, new[] { args });
                }
            }

            OnActivate();
            _isActive = true;
        }

        public virtual void Deactivate()
        {
            if (!_isActive) return;

            OnDeactivate();

            foreach (var module in _modulesByType.Values)
            {
                if (module is IRestorableShaderModule restorableModule)
                {
                    restorableModule.RestoreTouchedState();
                    continue;
                }

                var legacyRestoreMethod = module.GetType().GetMethod("RestoreMaterials", BindingFlags.Public | BindingFlags.Instance);
                legacyRestoreMethod?.Invoke(module, null);
            }

            _modulesByType.Clear();
            _shaderArgs.Clear();
            _isActive = false;
        }

        public void UpdateArgs(string shaderName, object args)
        {
            if (!_isActive) return;

            _shaderArgs[shaderName] = args;

            var module = _modulesByType.Values.FirstOrDefault(m => m.Name == shaderName);
            if (module != null)
                OnUpdateShaderArgs(module, args);
        }

        public object GetArgs(string shaderName) =>
            _shaderArgs.TryGetValue(shaderName, out var args) ? args : null;

        public IReadOnlyList<IShaderModule> GetShaders()
        {   // Return all required shader modules that are loaded and available
            var modules = new List<IShaderModule>();
            
            LoadRequirements();
            foreach (var req in _requirements)
            {
                var module = req.ResolveModule();
                if (module != null && module.IsLoaded)
                    modules.Add(module);
            }
            
            return modules.AsReadOnly();
        }

        protected T GetModule<T>() where T : class, IShaderModule =>
            _modulesByType.TryGetValue(typeof(T), out var module) ? module as T : null;

        protected virtual void OnActivate() { }
        protected virtual void OnDeactivate() { }
        protected virtual void OnUpdateShaderArgs(IShaderModule module, object args) { }
    }

    public class ShaderPackInfo
    {
        public ShaderPackAttribute? Attribute { get; set; }
        public Texture2D? Icon { get; set; }
        public Type? PackType { get; set; }
        public ShaderRequirementAttribute[]? Requirements { get; set; }
    }

    public static class ShaderPackRegistry
    {
        private static readonly Dictionary<string, IShaderPack> _packs = new Dictionary<string, IShaderPack>();
        private static IShaderPack? _activePack;

        public static void Register(IShaderPack pack)
        {   // Register a shader pack instance for later activation
            if (pack == null || string.IsNullOrEmpty(pack.Name)) return;
            _packs[pack.Name] = pack;
        }

        public static IShaderPack Get(string name) =>
            !string.IsNullOrEmpty(name) && _packs.TryGetValue(name, out var pack) ? pack : null;

        public static IShaderPack ActivePack => _activePack;

        public static bool SetActivePack(string name, Dictionary<string, object> shaderArgs = null)
        {   // Activate a pack by name, deactivating current pack if needed
            var pack = Get(name);
            if (pack == null || !pack.CanActivate) return false;

            _activePack?.Deactivate();
            pack.Activate(shaderArgs);
            if (!pack.IsActive)
            {
                Debug.LogWarning($"[ShaderPackRegistry] Pack '{name}' did not become active after activation attempt.");
                _activePack = null;
                return false;
            }

            _activePack = pack;
            return true;
        }

        public static void DeactivateAll()
        {   // Deactivate all active shader packs
            _activePack?.Deactivate();
            _activePack = null;
        }

        public static IReadOnlyCollection<IShaderPack> AllPacks => _packs.Values.ToArray();

        public static void Initialize()
        {   // Discover and register all shader packs in loaded assemblies
            _packs.Clear();

            var packTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(GetTypesFromAssembly)
                .Where(t => t != null && !t.IsAbstract && typeof(IShaderPack).IsAssignableFrom(t)
                    && t.GetConstructor(Type.EmptyTypes) != null);

            foreach (var type in packTypes)
            {
                try
                {
                    var pack = (IShaderPack)Activator.CreateInstance(type);
                    Register(pack);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ShaderPackRegistry] Failed to create pack '{type.FullName}': {e.Message}");
                }
            }
        }

        private static Type[] GetTypesFromAssembly(Assembly asm)
        {   // Safely retrieve types from assembly handling load exceptions
            try { return asm.GetTypes(); }
            catch (ReflectionTypeLoadException e)
            {
                Debug.LogWarning($"[ShaderPackRegistry] Partial type load failure in assembly '{asm.FullName}': {e.Message}");
                return e.Types.Where(t => t != null).ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ShaderPackRegistry] Failed to enumerate types in assembly '{asm.FullName}': {ex.Message}");
                return Array.Empty<Type>();
            }
        }
    }

    public static class ShaderPackLoader
    {
        public static ShaderPackInfo Load(Type type)
        {   // Load shader pack metadata from type using reflection
            var attr = type.GetCustomAttribute<ShaderPackAttribute>();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var requirements = fields.SelectMany(f => f.GetCustomAttributes<ShaderRequirementAttribute>()).ToArray();

            return new ShaderPackInfo
            {
                Attribute = attr,
                PackType = type,
                Requirements = requirements,
                Icon = null
            };
        }
    }
}
