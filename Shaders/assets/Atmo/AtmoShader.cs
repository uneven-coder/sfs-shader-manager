using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using GeneratedUI;
using shaders.Lib;
using shaders.Lib.Renders;
using SFS.Cameras;
using SFS.World;
using SFS.World.Terrain;
using SFS.WorldBase;
using SFS.World.PlanetModules;
using shaders.Lib.ShaderModules.ShaderPack;
using static shaders.Lib.ShaderModules.ShaderPack.ShaderRequirementAttribute;

namespace shaders.Effects.AtmosphereShaderPack
{
    [ShaderPack("Atmosphere ReShade", "Volumetric atmosphere rendering with physically-based light scattering, dynamic sky gradients, and realistic ray-marched clouds.", author: "Cratior", version: "1.0", updateUrl: "https://example.com/myshaderpack/update")]
    public class atmosphericShaderPack : ShaderPackBase
    {
        [ShaderRequirement(typeof(AtmoShaderModule), FailCondition.DisablePack, "Atmospheric Shader")]
        [ShaderRequirement(typeof(CloudsShaderModule), FailCondition.WarnOnly, "Cloud Shader")]
        public object? Requirements;
    }

    [ShaderModule("AtmoShader", ShaderType.Shader, "Hidden/shaders/AtmoShader", OverlayRenderMode.CustomRender)]
    [ShaderRoute(ShaderRouteMode.IncludeOnly, new[] { "world", "hub" })]
    public class AtmoShaderModule : ObjectTargetShaderModule<AtmoShaderModule.Args, object>
    {
        private const string AtmoShaderKey = "AtmoShader";
        private const string CloudsShaderKey = "CloudsShader";

        public struct Args
        {
            // --- Planet Setup ---
            [ShaderArg(group: "Planet Setup", tab: "Atmosphere", property: "_PlanetRadius", defaultValue: 6371000f, exposeInUi: false)] public float PlanetRadius;
            [ShaderArg(group: "Planet Setup", tab: "Atmosphere", property: "_AtmosphereHeight", defaultValue: 60000f, exposeInUi: false)] public float AtmosphereHeight;
            [ShaderArg(group: "Planet Setup", tab: "Atmosphere", property: "_AtmosphereScale", defaultValue: 1f, exposeInUi: false)] public float AtmosphereScale;
            [ShaderArg(group: "Planet Setup", tab: "Atmosphere", property: "_PlanetCenterWS", exposeInUi: false)] public Vector3 PlanetCenterWS;
            [ShaderArg(group: "Planet Setup", tab: "Atmosphere", exposeInUi: false)] public Vector3 PlanetCoreCenterWS;

            // --- Scattering ---
            [ShaderArg(group: "Scattering", tab: "Atmosphere", property: "_AtmosphereDensity", defaultValue: 1.0f)] public float AtmosphereDensity;
            [ShaderArg(group: "Scattering", tab: "Atmosphere", property: "_DensityCurve", defaultValue: 1.0f)] public float DensityCurve;
            [ShaderArg(group: "Scattering", tab: "Atmosphere", property: "_ScatterStrength", defaultValue: 2.4f)] public float ScatterStrength;
            [ShaderArg(group: "Scattering", tab: "Atmosphere", property: "_RayleighStrength", defaultValue: 2.6f)] public float RayleighStrength;
            [ShaderArg(group: "Scattering", tab: "Atmosphere", property: "_MieStrength", defaultValue: 11.0f)] public float MieStrength;
            [ShaderArg(group: "Scattering", tab: "Atmosphere", property: "_MieG", defaultValue: 1.0f)] public float MieAnisotropy;
            [ShaderArg(group: "Scattering", tab: "Atmosphere", property: "_RefractiveIndex", defaultValue: 1.5f)] public float RefractiveIndex;

            // --- Lighting & Effects ---
            [ShaderArg(group: "Lighting & Effects", tab: "Atmosphere", property: "_SunDir", exposeInUi: false)] public Vector3 SunDirection;
            [ShaderArg(group: "Lighting & Effects", tab: "Atmosphere", property: "_SunColor", exposeInUi: false)] public Color SunColor;
            [ShaderArg(group: "Lighting & Effects", tab: "Atmosphere", property: "_TerminatorWidth", defaultValue: 0.4f)] public float TerminatorWidth;
            [ShaderArg(group: "Lighting & Effects", tab: "Atmosphere", property: "_NightAmbientMin", defaultValue: 0.54f)] public float NightAmbientMin;
            [ShaderArg(group: "Lighting & Effects", tab: "Atmosphere", property: "_SunHaloExponent", defaultValue: 11.0f)] public float SunHaloExponent;
            [ShaderArg(group: "Lighting & Effects", tab: "Atmosphere", property: "_SunHaloIntensity", defaultValue: 11.0f)] public float SunHaloIntensity;
            [ShaderArg(group: "Lighting & Effects", tab: "Atmosphere", property: "_GradientMultiplier", defaultValue: 1f, exposeInUi: false)] public float GradientMultiplier;
            [ShaderArg(group: "Lighting & Effects", tab: "Atmosphere", property: "_GradientTex", exposeInUi: false)] public Texture2D GradientTexture;

            // --- Clouds ---
            [ShaderArg(group: "World Clouds", tab: "World Clouds", autoApply: true, defaultValue: true)] public bool EnableClouds;
            [ShaderArg(group: "World Clouds", tab: "World Clouds", autoApply: true, exposeInUi: false)] public CloudsShaderModule.Args CloudLayer;
        }

        private string _activePlanetCode = string.Empty;
        private Renderer[] _activeRenderers = Array.Empty<Renderer>();
        private Renderer[] _activeCloudRenderers = Array.Empty<Renderer>();
        private CloudsShaderModule? _cloudsModule;
        private Camera? _lastCamera;
        private float _lastCameraZoom;
        private bool _lastScaledSpace;
        private bool _lastEnableClouds;
        private bool _loggedMissingCloudModule;

        public AtmoShaderModule()
        {   // Register cloud module for both normal and scaled rendering
            _cloudsModule = ShaderRegistry.Get("CloudsShader") as CloudsShaderModule;
        }

        public override object Run(in Args args)
        {   // Manager/base class owns updater lifecycle; module only provides per-frame logic.
            StartOrUpdateLiveLoop(args);
            return null;
        }

        protected override void OnLiveUpdate(in Args baseArgs)
        {   // Update atmosphere shader every frame handling camera and zoom changes
            if (Shader == null)
                return;

            var currentCamera = ResolveActiveCamera();
            if (currentCamera == null)
                return;

            var currentZoom = currentCamera.orthographicSize;

            var dyn = BuildDynamicArgs(baseArgs, currentCamera, out var playerPlanetCode, out var isScaledSpace);
            if (string.IsNullOrEmpty(playerPlanetCode))
            {
                if (_activeRenderers.Length > 0)
                    RestoreMaterials();

                _lastEnableClouds = dyn.EnableClouds;
                return;
            }

            var needsReinit = _activePlanetCode != playerPlanetCode || _activeRenderers.Length == 0 ||
                            _activeRenderers.Any(r => r == null || r.sharedMaterials?.Length == 0) ||
                            (dyn.EnableClouds && (_activeCloudRenderers.Length == 0 || _activeCloudRenderers.Any(r => r == null || r.sharedMaterials?.Length == 0))) ||
                            (!dyn.EnableClouds && _activeCloudRenderers.Length > 0) ||
                            dyn.EnableClouds != _lastEnableClouds ||
                            isScaledSpace != _lastScaledSpace;

            var shouldProcessVisibleTargets = needsReinit || ShouldProcessForCamera(currentCamera, _activeRenderers);

            if (!shouldProcessVisibleTargets)
            {
                _lastEnableClouds = dyn.EnableClouds;
                _lastCamera = currentCamera;
                _lastCameraZoom = currentZoom;
                _lastScaledSpace = isScaledSpace;

                GeneratedLayout.UpdateShaderProvidedArgs(AtmoShaderKey, dyn);
                GeneratedLayout.UpdateShaderProvidedArgs(CloudsShaderKey, dyn.CloudLayer);
                return;
            }

            if (needsReinit) ReinitializePlanetMaterials(playerPlanetCode, dyn, isScaledSpace);
            else UpdateMaterialsWithArgs(dyn, isScaledSpace);

            _lastEnableClouds = dyn.EnableClouds;

            _lastCamera = currentCamera;
            _lastCameraZoom = currentZoom;
            _lastScaledSpace = isScaledSpace;

            GeneratedLayout.UpdateShaderProvidedArgs(AtmoShaderKey, dyn);
            GeneratedLayout.UpdateShaderProvidedArgs(CloudsShaderKey, dyn.CloudLayer);
        }

        private void ReinitializePlanetMaterials(string playerPlanetCode, in Args args, bool isScaledSpace)
        {   // Reinitialize materials with atmosphere and cloud layers based on scale mode
            _activePlanetCode = playerPlanetCode;
            base.RestoreMaterials();

            _activeRenderers = ResolveAtmosphereRenderers(playerPlanetCode);
            _activeCloudRenderers = Array.Empty<Renderer>();

            var updatedArgs = BuildSpatiallySyncedArgs(args, playerPlanetCode);

            var customCloudsReady = updatedArgs.EnableClouds && EnsureCloudsModuleLoaded();
            if (customCloudsReady)
                _activeCloudRenderers = _activeRenderers;
            
            if (_activeRenderers.Length > 0)
            {
                foreach (var r in _activeRenderers)
                {   // Create atmosphere material with optional cloud material stacked above it
                    var originalMats = r.sharedMaterials;
                    var originalMat = originalMats.Length > 0 ? originalMats[0] : null;
                    var atmoMat = CreateAtmoMaterial(originalMat, updatedArgs);
                    var cloudMat = customCloudsReady
                        ? _cloudsModule!.CreateMaterialWithArgs(originalMat, updatedArgs.CloudLayer, isScaledSpace)
                        : null;

                    var customMats = cloudMat != null ? new[] { atmoMat, cloudMat } : new[] { atmoMat };
                    RegisterTouchedRenderer(r, originalMats, customMats);
                }
            }

            if (updatedArgs.EnableClouds && !customCloudsReady)
                Debug.LogWarning("[AtmoShaderModule] Clouds are enabled but CloudsShader is not available/loaded, so no cloud material was attached.");
        }

        private (Vector3 center, float scale) CalculatePlanetCoreFallback(string playerPlanetCode)
        {   // Resolve fallback center/scale when atmosphere renderers are unavailable.
            var planet = UnityEngine.Object.FindObjectsOfType<Planet>()
                .FirstOrDefault(p => p != null &&
                    (string.Equals(p.codeName, playerPlanetCode, StringComparison.Ordinal)
                        || string.Equals(p.name, playerPlanetCode, StringComparison.Ordinal)));

            if (planet == null)
                return (Vector3.zero, AtmoScaleForCurrentView());

            return (planet.transform.position, AtmoScaleForCurrentView());
        }

        private (Vector3 center, float scale) CalculateAtmosphereData()
        {   // Shaders use (_AtmosphereScale / 1_000_000) to map stored _PlanetRadius (metres) to scene units:
            //   world view  = 1 m/unit  → scale 1_000_000
            //   scaled view = ScaledSpaceScale m/unit → scale 1_000_000 / ScaledSpaceScale
            var center = Vector3.zero;
            bool first = true;

            foreach (var r in _activeRenderers.Where(r => r != null))
            {
                if (first) { center = r.transform.position; first = false; }
            }

            return (center, AtmoScaleForCurrentView());
        }

        private float AtmoScaleForCurrentView()
        {
            bool isScaled = false;
            try { isScaled = WorldView.main?.scaledSpace.Value ?? false; } catch { }
            if (!isScaled) return 1_000_000f;
            try { return 1_000_000f / (float)WorldView.ScaledSpaceScale; }
            catch { return 10f; }
        }

        private Material CreateAtmoMaterial(Material original, in Args args)
        {   // Create atmosphere material with shader properties applied
            if (Shader == null) { Debug.LogWarning("[AtmoShaderModule] Shader not loaded"); return original != null ? new Material(original) : null; }

            var mat = new Material(Shader) { name = $"CustomAtmo_{original?.name ?? "Material"}" };
            mat.renderQueue = 3000;
            if (original?.HasProperty("_MainTex") == true && mat.HasProperty("_MainTex")) 
                mat.SetTexture("_MainTex", original.GetTexture("_MainTex"));

            ApplyArgsAutomatic(mat, args);
            return mat;
        }

        private void UpdateMaterialsWithArgs(in Args args, bool isScaledSpace)
        {   // Update existing materials with new arguments and handle cloud layer changes
            var hasAtmoTargets = _activeRenderers != null && _activeRenderers.Length > 0;
            var updatedArgs = BuildSpatiallySyncedArgs(args, _activePlanetCode);

            var customCloudsReady = !updatedArgs.EnableClouds || EnsureCloudsModuleLoaded();

            if (hasAtmoTargets)
            {
                foreach (var r in _activeRenderers.Where(r => r != null))
                {
                    var currentMats = r.sharedMaterials;
                    if (currentMats?.Length == 0) { ReinitializePlanetMaterials(_activePlanetCode, args, isScaledSpace); return; }

                    var hasAtmo = false;
                    var hasCloud = false;
                    foreach (var mat in currentMats)
                    {
                        if (mat == null)
                            continue;

                        if (mat.shader == Shader)
                        {
                            ApplyArgsAutomatic(mat, updatedArgs);
                            hasAtmo = true;
                            continue;
                        }

                        if (customCloudsReady && _cloudsModule != null && mat.shader == _cloudsModule.Shader)
                        {
                            _cloudsModule.UpdateMaterialArgs(mat, updatedArgs.CloudLayer, isScaledSpace);
                            hasCloud = true;
                        }
                    }

                    if (!hasAtmo || (updatedArgs.EnableClouds && !hasCloud) || (!updatedArgs.EnableClouds && hasCloud) || !customCloudsReady)
                    {
                        ReinitializePlanetMaterials(_activePlanetCode, args, isScaledSpace);
                        return;
                    }
                }
            }

        }

        private bool EnsureCloudsModuleLoaded()
        {   // Resolve and load CloudsShader module lazily to avoid constructor-order null references.
            if (!ShaderRegistry.TryGetReady(CloudsShaderKey, out CloudsShaderModule readyModule, out var error))
            {
                if (!_loggedMissingCloudModule)
                {
                    Debug.LogWarning($"[AtmoShaderModule] {error}");
                    _loggedMissingCloudModule = true;
                }

                return false;
            }

            _cloudsModule = readyModule;
            _loggedMissingCloudModule = false;
            return true;
        }

        private Args BuildSpatiallySyncedArgs(in Args args, string planetCode)
        {   // Keep atmosphere center/scale and cloud layer in sync for the active target body.
            var hasAtmoTargets = _activeRenderers != null && _activeRenderers.Length > 0;
            var (planetCenter, scale) = hasAtmoTargets
                ? CalculateAtmosphereData()
                : CalculatePlanetCoreFallback(planetCode);

            var updated = args;
            updated.AtmosphereScale = scale;
            updated.PlanetCenterWS = planetCenter;
            updated.CloudLayer = SyncCloudArgs(updated.CloudLayer, planetCenter, scale);
            return updated;
        }

        private CloudsShaderModule.Args SyncCloudArgs(CloudsShaderModule.Args cloudArgs, Vector3 center, float scale)
        {   // Synchronize cloud arguments with atmosphere settings
            cloudArgs.PlanetCenterWS = center;
            cloudArgs.AtmosphereScale = scale;
            return cloudArgs;
        }

        private Args BuildDynamicArgs(in Args baseArgs, Camera activeCamera, out string playerPlanetCode, out bool isScaledSpace)
        {   // Build dynamic arguments with runtime values from game state and planet data
            var a = MergeUiArgs(baseArgs);
            playerPlanetCode = string.Empty;
            isScaledSpace = false;

            // Determine player planet and scaled space mode
            var playerPlanet = ResolvePlayerPlanet();
            playerPlanetCode = ResolveTargetPlanetCode(playerPlanet, activeCamera);
            a.PlanetCoreCenterWS = playerPlanet != null ? playerPlanet.transform.position : Vector3.zero;

            isScaledSpace = ResolveScaledSpace(activeCamera);

            var userOverrides = GeneratedLayout.GetUserArgs(AtmoShaderKey);
            ApplyAtmosphereDefaults(ref a, userOverrides);

            // Load cloud defaults if needed
            if (_cloudsModule == null)
                EnsureCloudsModuleLoaded();

            if ((a.CloudLayer.World.CloudRaymarchSteps <= 0 || a.CloudLayer.Scaled.CloudRaymarchSteps <= 0) && _cloudsModule != null)
            {
                a.CloudLayer = CloudsShaderModule.CreateDefaultArgs();
                GeneratedLayout.UpdateShaderProvidedArgs(CloudsShaderKey, a.CloudLayer);
            }

            // Apply merged cloud args from UI
            var cloudMerged = GeneratedLayout.GetCurrentArgs(CloudsShaderKey) as CloudsShaderModule.Args?;
            if (cloudMerged.HasValue) a.CloudLayer = cloudMerged.Value;

            SyncAtmoSharedToCloud(ref a);

            // Extract planet atmosphere data
            a = ApplyPlanetAtmosphereData(a, playerPlanet, userOverrides);

            // Update sun direction and color
            var sunPlanet = UnityEngine.Object.FindObjectsOfType<Planet>()
                .FirstOrDefault(p => p?.codeName == "Sun");

            if (sunPlanet != null && playerPlanet != null)
            {
                if (!WasEdited(userOverrides, "SunDirection"))
                {
                    try
                    {
                        var sunLoc = sunPlanet.GetLocation(WorldTime.main.worldTime);
                        var planetLoc = playerPlanet.GetLocation(WorldTime.main.worldTime);
                        a.SunDirection = (planetLoc.position - sunLoc.position).normalized;
                    }
                    catch { }
                }

                if (!WasEdited(userOverrides, "SunColor"))
                {
                    try
                    {
                        var sunFog = sunPlanet.data?.atmosphereVisuals?.FOG;
                        if (sunFog?.keys?.Length > 0) 
                            a.SunColor = sunFog.Evaluate(sunFog.keys[0].distance);
                        else if (sunPlanet.data?.postProcessing?.keys?.Length > 0)
                        {
                            var pp = sunPlanet.data.postProcessing.Evaluate(0f);
                            a.SunColor = new Color(pp.red, pp.green, pp.blue, 1f);
                        }
                    }
                    catch { }
                }

                a.CloudLayer.SunDirection = a.SunDirection;
                a.CloudLayer.SunColor = a.SunColor;
            }

            return a;
        }

        private Args MergeUiArgs(in Args baseArgs)
        {   // Start from base args then merge user-edited runtime args if available.
            var merged = GeneratedLayout.GetCurrentArgs(AtmoShaderKey) as Args?;
            return merged.HasValue ? merged.Value : baseArgs;
        }

        private static bool WasEdited(System.Collections.Generic.Dictionary<string, object>? overrides, string key)
        {
            return overrides?.ContainsKey(key) ?? false;
        }

        private static void ApplyAtmosphereDefaults(ref Args args, System.Collections.Generic.Dictionary<string, object>? userOverrides)
        {   // Apply resilient defaults only when user has not explicitly overridden the setting.
            if (!WasEdited(userOverrides, "EnableClouds"))
                args.EnableClouds = true;

            if (!WasEdited(userOverrides, "NightAmbientMin") && args.NightAmbientMin <= 0f)
                args.NightAmbientMin = 0.54f;

            if (!WasEdited(userOverrides, "SunHaloExponent") && args.SunHaloExponent <= 0f)
                args.SunHaloExponent = 11.0f;

            if (!WasEdited(userOverrides, "SunHaloIntensity") && args.SunHaloIntensity <= 0f)
                args.SunHaloIntensity = 11.0f;
        }

        private static void SyncAtmoSharedToCloud(ref Args args)
        {   // Keep shared atmosphere lighting/scale values mirrored to cloud args.
            args.CloudLayer.PlanetRadius = args.PlanetRadius;
            args.CloudLayer.SunDirection = args.SunDirection;
            args.CloudLayer.SunColor = args.SunColor;
        }

        private string ResolveTargetPlanetCode(Planet? playerPlanet, Camera activeCamera)
        {   // Resolve a stable planet code using player/view state with atmosphere proximity fallback.
            var direct = playerPlanet?.codeName ?? playerPlanet?.name;
            if (!string.IsNullOrWhiteSpace(direct))
                return direct;

            var cameraPosition = activeCamera != null ? activeCamera.transform.position : Vector3.zero;
            var nearestAtmosphere = UnityEngine.Object.FindObjectsOfType<Atmosphere>()
                .Where(a => a?.planet != null)
                .OrderBy(a => (a.transform.position - cameraPosition).sqrMagnitude)
                .FirstOrDefault();

            return nearestAtmosphere?.planet?.codeName
                ?? nearestAtmosphere?.planet?.name
                ?? string.Empty;
        }

        private static Renderer[] ResolveAtmosphereRenderers(string playerPlanetCode)
        {   // Resolve active atmosphere renderers with fallback for updated game object structures.
            var atmospheres = UnityEngine.Object.FindObjectsOfType<Atmosphere>()
                .Where(a => a?.planet != null
                    && (string.Equals(a.planet.codeName, playerPlanetCode, StringComparison.Ordinal)
                        || string.Equals(a.planet.name, playerPlanetCode, StringComparison.Ordinal)))
                .SelectMany(a =>
                {
                    var direct = a.GetComponent<MeshRenderer>();
                    var child = a.GetComponentInChildren<MeshRenderer>(true);
                    return new[] { direct, child };
                })
                .Where(mr => mr != null)
                .Cast<Renderer>()
                .Distinct()
                .ToArray();

            if (atmospheres.Length > 0)
                return atmospheres;

            var fallback = UnityEngine.Object.FindObjectsOfType<MeshRenderer>()
                .Where(renderer =>
                {
                    if (renderer == null || renderer.gameObject == null)
                        return false;

                    var rendererName = renderer.name ?? string.Empty;
                    if (rendererName.IndexOf("atmo", StringComparison.OrdinalIgnoreCase) < 0
                        && rendererName.IndexOf("atmosphere", StringComparison.OrdinalIgnoreCase) < 0)
                        return false;

                    var planet = renderer.GetComponentInParent<Planet>();
                    return planet != null &&
                        (string.Equals(planet.codeName, playerPlanetCode, StringComparison.Ordinal)
                            || string.Equals(planet.name, playerPlanetCode, StringComparison.Ordinal));
                })
                .Cast<Renderer>()
                .Distinct()
                .ToArray();

            if (fallback.Length > 0)
                return fallback;

            return UnityEngine.Resources.FindObjectsOfTypeAll<MeshRenderer>()
                .Where(renderer =>
                {
                    if (renderer == null || renderer.gameObject == null)
                        return false;

                    if (!renderer.gameObject.scene.IsValid())
                        return false;

                    var rendererName = renderer.name ?? string.Empty;
                    if (rendererName.IndexOf("atmo", StringComparison.OrdinalIgnoreCase) < 0
                        && rendererName.IndexOf("atmosphere", StringComparison.OrdinalIgnoreCase) < 0)
                        return false;

                    var planet = renderer.GetComponentInParent<Planet>();
                    return planet != null &&
                        (string.Equals(planet.codeName, playerPlanetCode, StringComparison.Ordinal)
                            || string.Equals(planet.name, playerPlanetCode, StringComparison.Ordinal));
                })
                .Cast<Renderer>()
                .Distinct()
                .ToArray();
        }

        private static Camera? ResolveActiveCamera() => SfsWorldUtils.ResolveActiveCamera();
        private static bool ResolveScaledSpace(Camera? activeCamera) => SfsWorldUtils.ResolveScaledSpace(activeCamera);
        private Planet? ResolvePlayerPlanet() => SfsWorldUtils.ResolvePlayerPlanet(allowCameraFallback: true);
        private static Planet? TryResolveViewPlanet() => SfsWorldUtils.TryResolveViewPlanet();

        private Args ApplyPlanetAtmosphereData(Args args, Planet? playerPlanet, System.Collections.Generic.Dictionary<string, object>? userOverrides)
        {   // Pull atmosphere and cloud defaults from current player planet visuals/physics
            if (playerPlanet?.HasAtmosphereVisuals != true)
                return args;

            try
            {
                var atmoMat = playerPlanet.atmosphereMaterial;
                if (atmoMat != null)
                {
                    args.GradientTexture = atmoMat.GetTexture("_GradientTex") as Texture2D;
                    args.GradientMultiplier = atmoMat.GetFloat("_GradientMultiplier");
                }
            }
            catch { }

            try
            {
                var gradient = playerPlanet.data?.atmosphereVisuals?.GRADIENT;
                if (gradient == null)
                    return args;

                // Prefer the planet's real physics atmosphere extent (the same height the game
                // uses for drag/heating/parachutes) over the purely cosmetic gradient-texture
                // scale, so the rendered shell actually matches how thick this planet's atmosphere
                // really is instead of an Earth-tuned visual constant — this is what lets the
                // shader render correctly on any planet, not just the one it was authored against.
                args.AtmosphereHeight = playerPlanet.HasAtmospherePhysics
                    ? (float)playerPlanet.AtmosphereHeightPhysics
                    : (float)gradient.height;

                args.PlanetRadius = (float)playerPlanet.Radius;
                args.CloudLayer.PlanetRadius = args.PlanetRadius;

                // Match this planet's real atmospheric density falloff shape (the same formula
                // Planet.GetAtmosphericDensity uses for gameplay drag/heating) instead of one
                // artist-tuned curve fixed for a single planet — but only when the user hasn't
                // explicitly dialed in their own values for these fields.
                var physics = playerPlanet.HasAtmospherePhysics ? playerPlanet.data?.atmospherePhysics : null;
                if (physics != null)
                {
                    if (!WasEdited(userOverrides, "AtmosphereDensity") && physics.density >= 0.0)
                        args.AtmosphereDensity = (float)physics.density;

                    if (!WasEdited(userOverrides, "DensityCurve") && physics.curve >= 0.0)
                        args.DensityCurve = (float)physics.curve;
                }

                var cloudOverrides = GeneratedLayout.GetUserArgs("CloudsShader");
                var hasWorldStartOverride = cloudOverrides?.ContainsKey("World.CloudStartHeight") == true;
                var hasWorldMaxOverride = cloudOverrides?.ContainsKey("World.CloudMaxHeight") == true;
                var hasScaledStartOverride = cloudOverrides?.ContainsKey("Scaled.CloudStartHeight") == true;
                var hasScaledMaxOverride = cloudOverrides?.ContainsKey("Scaled.CloudMaxHeight") == true;

                var startHeight = Mathf.Max(500f, args.AtmosphereHeight * 0.06f);
                var maxHeight = Mathf.Max(startHeight + 1000f, args.AtmosphereHeight * 0.35f);

                if (!hasWorldStartOverride)
                    args.CloudLayer.World.CloudStartHeight = startHeight;

                if (!hasWorldMaxOverride)
                    args.CloudLayer.World.CloudMaxHeight = maxHeight;

                if (!hasScaledStartOverride)
                    args.CloudLayer.Scaled.CloudStartHeight = startHeight;

                if (!hasScaledMaxOverride)
                    args.CloudLayer.Scaled.CloudMaxHeight = maxHeight;
            }
            catch { }

            return args;
        }

        public override void RestoreMaterials()
        {   // Restore touched renderer/material state and stop per-frame loop ownership.
            base.RestoreMaterials();
            StopLiveLoop();
            _activeRenderers = Array.Empty<Renderer>();
            _activeCloudRenderers = Array.Empty<Renderer>();
            _activePlanetCode = string.Empty;
            _lastCamera = null;
            _lastCameraZoom = 0f;
            _lastScaledSpace = false;
            _lastEnableClouds = false;
        }

        protected override void ApplyArgsToMaterial(Material mat, Args args) { }

        protected override Material CreateCustomMaterial(Material original, Args args) => CreateAtmoMaterial(original, args);
    }

    [ShaderModule("CloudsShader", ShaderType.Shader, "Hidden/shaders/CloudsShader", OverlayRenderMode.CustomRender)]
    public class CloudsShaderModule : ObjectTargetShaderModule<CloudsShaderModule.Args, object>
    {
        public struct CameraProfile
        {
            // --- Shape ---
            [ShaderArg(group: "Shape", property: "_CloudStartHeight", defaultValue: 3000f)] public float CloudStartHeight;
            [ShaderArg(group: "Shape", property: "_CloudMaxHeight", defaultValue: 22000f)] public float CloudMaxHeight;
            [ShaderArg(group: "Shape", property: "_CloudScale", defaultValue: 0.00056f)] public float CloudScale;
            [ShaderArg(group: "Shape", property: "_CloudThreshold", defaultValue: 0.6f)] public float CloudThreshold;
            [ShaderArg(group: "Shape", property: "_CloudCoverage", defaultValue: 0.04f)] public float CloudCoverage;
            [ShaderArg(group: "Shape", property: "_CloudType", defaultValue: 0.3f)] public float CloudType;
            [ShaderArg(group: "Shape", property: "_CloudSoftness", defaultValue: 0.3f)] public float CloudSoftness;
            [ShaderArg(group: "Shape", property: "_CloudThresholdVariation", defaultValue: 0.8f)] public float CloudThresholdVariation;
            [ShaderArg(group: "Shape", property: "_CloudThresholdNoiseScale", defaultValue: 0.00007f)] public float CloudThresholdNoiseScale;
            [ShaderArg(group: "Shape", property: "_CloudDetailIntensity", defaultValue: 0.0f)] public float CloudDetailIntensity;

            // --- Movement ---
            [ShaderArg(group: "Movement", property: "_CloudScrollSpeed", defaultValue: 100f)] public float CloudScrollSpeed;
            [ShaderArg(group: "Movement", property: "_CloudMovementDirection")] public Vector3 CloudMovementDirection;
            [ShaderArg(group: "Movement", property: "_CloudRotationAxis")] public Vector3 CloudRotationAxis;
            [ShaderArg(group: "Movement", property: "_CloudRotationSpeed", defaultValue: 0.009f)] public float CloudRotationSpeed;

            // --- Lighting & Quality ---
            [ShaderArg(group: "Lighting & Quality", property: "_CloudDensity", defaultValue: 0.6f)] public float CloudDensity;
            [ShaderArg(group: "Lighting & Quality", property: "_CloudAlpha", defaultValue: 1.0f)] public float CloudAlpha;
            [ShaderArg(group: "Lighting & Quality", property: "_CloudLightAbsorption", defaultValue: 1f)] public float CloudLightAbsorption;
            [ShaderArg(group: "Lighting & Quality", property: "_CloudAmbient", defaultValue: 0.35f)] public float CloudAmbient;
            [ShaderArg(group: "Lighting & Quality", property: "_CloudMultiScatter", defaultValue: 1.5f)] public float CloudMultiScatter;
            [ShaderArg(group: "Lighting & Quality", property: "_CloudBloom", defaultValue: 0.1f)] public float CloudBloom;
            [ShaderArg(group: "Lighting & Quality", property: "_CloudRaymarchSteps", defaultValue: 22)] public int CloudRaymarchSteps;
            [ShaderArg(group: "Lighting & Quality", property: "_CloudLightSteps", defaultValue: 2)] public int CloudLightSteps;
            [ShaderArg(group: "Lighting & Quality", property: "_CloudDepthFade", defaultValue: 5000000f)] public float CloudDepthFade;
            [ShaderArg(group: "Lighting & Quality", property: "_CloudDepthFadeSoftness", defaultValue: 2000000f)] public float CloudDepthFadeSoftness;
        }

        // NOTE: attribute defaultValues above stay at the shader's original tuned baseline (used
        // for "reset to default" resolution); the actual startup defaults handed out at runtime
        // come from CreateDefaultWorldProfile()/CreateDefaultScaledProfile() below, which are kept
        // in sync with the user's saved settings instead.

        public struct Args
        {
            [ShaderArg(group: "World Clouds", property: "_PlanetRadius", defaultValue: 6371000f, exposeInUi: false)] public float PlanetRadius;
            [ShaderArg(group: "World Clouds", property: "_PlanetCenterWS", exposeInUi: false)] public Vector3 PlanetCenterWS;
            [ShaderArg(group: "World Clouds", property: "_AtmosphereScale", defaultValue: 1f, exposeInUi: false)] public float AtmosphereScale;
            [ShaderArg(group: "World Clouds", property: "_SunDir", exposeInUi: false)] public Vector3 SunDirection;
            [ShaderArg(group: "World Clouds", property: "_SunColor", exposeInUi: false)] public Color SunColor;

            [ShaderArg(group: "World Clouds", tab: "World Clouds")] public CameraProfile World;
            [ShaderArg(group: "Scaled Clouds", tab: "Scaled Clouds")] public CameraProfile Scaled;
        }
        public override object Run(in Args args) => null;

        public static Args CreateDefaultArgs()
        {   // Build default profile values for world and scaled camera rendering
            return new Args
            {
                PlanetRadius = 6371000f,
                AtmosphereScale = 1f,
                World = CreateDefaultWorldProfile(),
                Scaled = CreateDefaultScaledProfile()
            };
        }

        private static CameraProfile CreateDefaultWorldProfile()
        {   // World-camera defaults tuned for close-range atmosphere visuals
            return new CameraProfile
            {
                CloudStartHeight = 0.0f,
                CloudMaxHeight = 12200f,
                CloudScale = 0.000175f,
                CloudThreshold = 0.453f,
                CloudDensity = 0.6f,
                CloudAlpha = 0.5f,
                CloudCoverage = 0.01478f,
                CloudType = 1.0f,
                CloudSoftness = 1.0f,
                CloudScrollSpeed = 111f,
                CloudMovementDirection = new Vector3(1f, 0f, 0f),
                CloudRotationAxis = new Vector3(5f, 0f, 1f),
                CloudRotationSpeed = 0.0049f,
                CloudDetailIntensity = 0.0f,
                CloudThresholdVariation = 0.6393f,
                CloudThresholdNoiseScale = 2.3e-05f,
                CloudRaymarchSteps = 22,
                CloudLightSteps = 5,
                CloudDepthFade = 67111f,
                CloudDepthFadeSoftness = 0.0f,
                CloudLightAbsorption = 6.0f,
                CloudAmbient = 2.0f,
                CloudMultiScatter = 1.0f,
                CloudBloom = 0.8f
            };
        }

        private static CameraProfile CreateDefaultScaledProfile()
        {   // Scaled-camera defaults tuned for distant rendering stability and readability
            return new CameraProfile
            {
                CloudStartHeight = -1000f,
                CloudMaxHeight = 8000f,
                CloudScale = 0.0001954f,
                CloudThreshold = 0.3751f,
                CloudDensity = 12.0f,
                CloudAlpha = 11.0f,
                CloudCoverage = 0.054f,
                CloudType = 0.3f,
                CloudSoftness = 11.0f,
                CloudScrollSpeed = 111f,
                CloudMovementDirection = new Vector3(1f, 0f, 0f),
                CloudRotationAxis = new Vector3(5f, 0f, 1f),
                CloudRotationSpeed = 0.01f,
                CloudDetailIntensity = 0.0f,
                CloudThresholdVariation = 1f,
                CloudThresholdNoiseScale = 1.7e-05f,
                CloudRaymarchSteps = 9,
                CloudLightSteps = 22,
                CloudDepthFade = 3.4f,
                CloudDepthFadeSoftness = 1.0f,
                CloudLightAbsorption = 2.0f,
                CloudAmbient = 3.3f,
                CloudMultiScatter = 11.0f,
                CloudBloom = 3.0f
            };
        }

        public Material CreateMaterialWithArgs(Material original, in Args args, bool isScaledSpace)
        {   // Create cloud material with camera-mode profile args applied
            if (Shader == null) { Debug.LogWarning("[CloudsShaderModule] Shader not loaded"); return original != null ? new Material(original) : null; }

            var mat = new Material(Shader) { name = $"Custom{(isScaledSpace ? "Scaled" : "")}Clouds_{original?.name ?? "Material"}" };
            mat.renderQueue = 3100;
            if (original?.HasProperty("_MainTex") == true && mat.HasProperty("_MainTex")) 
                mat.SetTexture("_MainTex", original.GetTexture("_MainTex"));

            ApplyProfileToMaterial(mat, args, isScaledSpace);
            return mat;
        }

        private void ApplyProfileToMaterial(Material mat, in Args args, bool isScaledSpace)
        {   // Apply shared args + active profile args directly from ShaderArg metadata.
            ApplyArgsAutomatic(mat, args, isScaledSpace ? nameof(Args.Scaled) : nameof(Args.World));
            mat.SetFloat("_ShaderTime", Time.unscaledTime);
        }

        public void UpdateMaterialArgs(Material mat, in Args args, bool isScaledSpace)
        {
            if (mat?.shader == Shader)
                ApplyProfileToMaterial(mat, args, isScaledSpace);
        }

        protected override void ApplyArgsToMaterial(Material mat, Args args) { }

        protected override Material CreateCustomMaterial(Material original, Args args) => CreateMaterialWithArgs(original, args, false);
    }

}
