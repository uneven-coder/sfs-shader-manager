using System;
using System.Collections.Generic;
using System.Linq;
using SFS.UI.ModGUI;
using UITools;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Globalization;
using System.Reflection;
using Newtonsoft.Json;
using shaders.Lib;
using shaders.Lib.ShaderModules.ShaderPack;
using shaders;


namespace GeneratedUI
{
    public static class GeneratedLayout
    {

        public static void Init() => GeneratedUiController.Init();
        public static void NotifyCameraInactive() => GeneratedUiController.NotifyCameraInactive();
        public static void UpdateShaderProvidedArgs(string shaderName, object args) => GeneratedUiController.UpdateShaderProvidedArgs(shaderName, args);
        public static Dictionary<string, object> GetUserArgs(string shaderName) { return GeneratedUiController.GetUserArgs(shaderName); }
        public static object GetCurrentArgs(string shaderName) { return GeneratedUiController.GetCurrentArgs(shaderName); }

        public sealed class ShaderBrowserState
        {
            public string Title { get; set; } = "Main Window";
            public string SearchText { get; set; } = string.Empty;
            public string ReadyToggleText { get; set; } = "Show: ALL";
            public string PackTitle { get; set; } = "ShaderPack Title";
            public string PackVersion { get; set; } = "Pack Version";
            public string PackType { get; set; } = "Pack Type";
            public string PackScenes { get; set; } = "Used Scenes: Any";
            public string PackCameras { get; set; } = "Used Cameras: Any";
            public bool ConfigOnlyMode { get; set; }
            public string PopoutButtonText { get; set; } = "Popout Config";
            public Action<string>? OnSearchChanged { get; set; }
            public Action? OnSearch { get; set; }
            public Action? OnClear { get; set; }
            public Action? OnToggleReadyFilter { get; set; }
            public Action? OnTogglePopout { get; set; }
            public List<PackCardModel> Packs { get; } = [];
            public List<RequirementCardModel> Requirements { get; } = [];
            public List<ConfigTabModel> ConfigTabs { get; } = [];
            public List<ConfigGroupModel> ConfigGroups { get; } = [];

            public void AddPack(string name, string version, string description, bool isSelected, Action? onToggle, Action? onConfig) => Packs.Add(new PackCardModel { Name = name, Version = version, Description = description, IsSelected = isSelected, OnToggle = onToggle, OnConfig = onConfig });
            public void AddRequirement(string name, string path, string status, string type, bool isLoaded) => Requirements.Add(new RequirementCardModel { Name = name, Path = path, Status = status, Type = type, IsLoaded = isLoaded });
            public ConfigGroupModel AddConfigGroup(string name, bool isCollapsed, Action? onToggleCollapsed)
            {
                var group = new ConfigGroupModel { Name = name, IsCollapsed = isCollapsed, OnToggleCollapsed = onToggleCollapsed };
                ConfigGroups.Add(group);
                return group;
            }

            public sealed class ConfigTabModel
            {
                public string Name = string.Empty;
                public bool IsActive;
                public Action? OnSelect;
            }

            public sealed class PackCardModel
            {
                public string Name = string.Empty;
                public string Version = string.Empty;
                public string Description = string.Empty;
                public bool IsSelected;
                public Action? OnToggle;
                public Action? OnConfig;
            }

            public sealed class RequirementCardModel
            {
                public string Name = string.Empty;
                public string Path = string.Empty;
                public string Status = string.Empty;
                public string Type = string.Empty;
                public bool IsLoaded;
            }

            public sealed class ConfigFieldModel
            {
                public string Label = string.Empty;
                public string Value = string.Empty;
                public Action<string>? OnValueChanged;
                public Action<TextInput>? OnInputBuilt;
                public bool UseToggle;
                public Func<bool>? ToggleValueGetter;
                public Action? OnToggle;
            }

            public sealed class ConfigGroupModel
            {
                public string Name = "Group";
                public bool IsCollapsed;
                public Action? OnToggleCollapsed;
                public List<ConfigTabModel> Tabs { get; } = [];
                public List<ConfigFieldModel> Fields { get; } = [];
                public void AddTab(string name, bool isActive, Action? onSelect) => Tabs.Add(new ConfigTabModel { Name = name, IsActive = isActive, OnSelect = onSelect });
                public void AddField(string label, string value, Action<string>? onValueChanged, Action<TextInput>? onInputBuilt) => Fields.Add(new ConfigFieldModel { Label = label, Value = value, OnValueChanged = onValueChanged, OnInputBuilt = onInputBuilt });
                public void AddBoolField(string label, Func<bool>? toggleValueGetter, Action? onToggle) => Fields.Add(new ConfigFieldModel { Label = label, UseToggle = true, ToggleValueGetter = toggleValueGetter, OnToggle = onToggle });
            }
        }

        private static ShaderBrowserState _shaderBrowserState = new ShaderBrowserState();
        private const string ColorText = "#ffffff";
        private const string ColorWindowRoot = "#1b2334";
        private const string ColorPanelBackground = "#282828";
        private const string ColorConfigPanel = "#202f42";
        private const string ColorGlobalTabBackground = "#2c4852";
        private const string ColorTabBackground = "#2f2f2f";
        private const string ColorTabActiveBackground = "#6b95c2";
        private const string ColorPackCardBackground = "#94aeeb";
        private const string ColorConfigGroupBackground = "#2c4852";
        private const string ColorConfigTabGroupBackground = "#510009";
        private const string ColorConfigTabContentBackground = "#485c7b";
        private const string ColorButtonAccent = "#00ff00";
        private const string ColorPackListBackground = "#000000";
        private const string ColorRequirementReady = "#489f7e";
        private const string ColorRequirementMissing = "#9f4848";
        private const string ColorNeutralDark = "#555555";
        private const string ColorNeutralLight = "#f9f9f9";

        public static void ConfigureShaderBrowser(ShaderBrowserState state, bool refresh = true)
        {
            _shaderBrowserState = state ?? new ShaderBrowserState();
            if (refresh)
                Refresh();
        }

        public static IReadOnlyList<UiNode> Define()
        {
            var windowWidth = _shaderBrowserState.ConfigOnlyMode ? 1000 : 1920;
            var windowHeight = _shaderBrowserState.ConfigOnlyMode ? 1400 : 1200;

            return new List<UiNode>
            {
                Node("element_1", UiNodeType.Window, "Window_1", windowWidth, windowHeight)
                    .At(30, 30)
                    .WithText(string.IsNullOrWhiteSpace(_shaderBrowserState.Title) ? "" : _shaderBrowserState.Title)
                    .Visual(TextAnchor.MiddleCenter, false, false, ColorText, false, "", "")
                    .AddChildren(
                        Node("element_2", UiNodeType.Box, "WindowRoot", 221, 1180)
                            .Visual(TextAnchor.MiddleLeft, false, false, ColorText, true, ColorWindowRoot, "")
                            .LayoutConfig(SFS.UI.ModGUI.Type.Vertical, TextAnchor.UpperCenter, 12, 12, 12, 12, 12)
                            .Sizing(UiSizeMode.Auto, UiSizeMode.Auto, 1)
                            .AddChildren(
                                BuildHeaderRow(),
                                Node("element_7", UiNodeType.Container, "MainRoot", 2800, 1000)
                                    .LayoutConfig(SFS.UI.ModGUI.Type.Horizontal, TextAnchor.UpperCenter, 25, 12, 12, 12, 12)
                                    .Sizing(UiSizeMode.Auto, UiSizeMode.Auto, 2)
                                    .AddChildren(BuildMainContentNodes().ToArray())
                            )
                    ),
            };
        }

        private static IEnumerable<UiNode> BuildPackCards()
        {
            return _shaderBrowserState.Packs.Count == 0
                ? new[]
                {
                    Node("element_10/empty", UiNodeType.Label, "No_Packs", 220, 48)
                        .WithText("No packs match current filters.")
                        .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 1),
                }
                : _shaderBrowserState.Packs.Select((pack, i) =>
                    Node($"element_pack_{i}/0", UiNodeType.Box, "Example_Shader", 220, 340)
                        .At(30, 30)
                        .Visual(TextAnchor.MiddleLeft, false, false, ColorText, false, ColorPackCardBackground, "")
                        .LayoutConfig(SFS.UI.ModGUI.Type.Vertical, TextAnchor.UpperLeft, 12, 24, 24, 12, 24)
                        .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 1)
                        .AddChildren(
                            Node($"element_pack_{i}/0/0", UiNodeType.Container, "Top_row", 220, 70)
                                .LayoutConfig(SFS.UI.ModGUI.Type.Horizontal, TextAnchor.MiddleCenter, 1, 0, 0, 0, 0)
                                .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 3)
                                .AddChildren(
                                    Node($"element_pack_{i}/0/0/0", UiNodeType.Label, "Shader_Name", 12, 70)
                                        .WithText(pack.Name)
                                        .Sizing(UiSizeMode.Auto, UiSizeMode.Auto, 2),
                                    Node($"element_pack_{i}/0/0/1", UiNodeType.Label, "Version", 100, 30)
                                        .WithText(pack.Version)
                                        .Visual(TextAnchor.MiddleRight, false, false, ColorText, false, "", "")
                                        .Sizing(UiSizeMode.Manual, UiSizeMode.Manual, 2)
                                ),
                            Node($"element_pack_{i}/0/1", UiNodeType.Container, "Center", 220, 70)
                                .Sizing(UiSizeMode.Auto, UiSizeMode.Auto, 3)
                                .AddChildren(
                                    Node($"element_pack_{i}/0/1/0", UiNodeType.Label, "Description", 220, 70)
                                        .WithText(pack.Description)
                                        .Sizing(UiSizeMode.Auto, UiSizeMode.Auto, 1)
                                ),
                            Node($"element_pack_{i}/0/2", UiNodeType.Container, "Bottom_Row", 220, 52)
                                .LayoutConfig(SFS.UI.ModGUI.Type.Horizontal, TextAnchor.MiddleLeft, 12, 0, 0, 0, 0)
                                .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 3)
                                .AddChildren(
                                    Node($"element_pack_{i}/0/2/0", UiNodeType.Toggle, "Toggle_133", 60, 52)
                                        .OnToggle(() => pack.IsSelected, pack.OnToggle)
                                        .Sizing(UiSizeMode.Manual, UiSizeMode.Manual, 2),
                                    Node($"element_pack_{i}/0/2/1", UiNodeType.Button, "Open_Config", 220, 70)
                                        .WithText("Config")
                                        .OnClick(pack.OnConfig)
                                        .Visual(TextAnchor.MiddleLeft, false, false, ColorText, false, ColorButtonAccent, "")
                                        .Sizing(UiSizeMode.Auto, UiSizeMode.Auto, 2)
                                )
                        ));
        }

        private static IEnumerable<UiNode> BuildMainContentNodes()
        {
            if (_shaderBrowserState.ConfigOnlyMode)
            {
                yield return Node("element_84", UiNodeType.Box, "Config_Only_Panel", 220, 70)
                    .Visual(TextAnchor.MiddleLeft, false, false, ColorText, true, ColorConfigPanel, "")
                    .LayoutConfig(SFS.UI.ModGUI.Type.Vertical, TextAnchor.UpperLeft, 12, 120, 12, 12, 12)
                    .Sizing(UiSizeMode.Auto, UiSizeMode.Auto, 3)
                    .Scroll(true, false)
                    .AddChildren(
                        Node("element_cfg_title", UiNodeType.Label, "Config_Title", 220, 48)
                            .WithText("Config Editor")
                            .Visual(TextAnchor.MiddleCenter, false, false, ColorText, false, "", "")
                            .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 2)
                    )
                    .AddChildren(BuildConfigSectionNodes("element_cfg_inline", includeGlobalTabs: true, wrapInBox: false));

                yield break;
            }

            yield return BuildPackListPanel();
            yield return BuildPackDetailsPanel();
        }

        private static UiNode BuildHeaderRow()
        {
            return Node("element_3", UiNodeType.Box, "Header", 220, 90)
                .LayoutConfig(SFS.UI.ModGUI.Type.Horizontal, TextAnchor.MiddleCenter, 12, 12, 12, 12, 12)
                .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 2)
                .AddChildren(
                    Node("element_4", UiNodeType.TextInput, "Search_Input", 219, 70)
                        .WithText(_shaderBrowserState.SearchText ?? string.Empty)
                        .OnTextChanged(_shaderBrowserState.OnSearchChanged)
                        .Sizing(UiSizeMode.Auto, UiSizeMode.Auto, 4),
                    Node("element_4b", UiNodeType.Button, "Search", 180, 70)
                        .WithText("Search")
                        .OnClick(_shaderBrowserState.OnSearch)
                        .Sizing(UiSizeMode.Manual, UiSizeMode.Auto, 4),
                    Node("element_5", UiNodeType.Button, "Clear", 180, 70)
                        .WithText("Clear")
                        .OnClick(_shaderBrowserState.OnClear)
                        .Sizing(UiSizeMode.Manual, UiSizeMode.Auto, 4),
                    Node("element_6", UiNodeType.Button, "Button_10", 240, 70)
                        .WithText(string.IsNullOrWhiteSpace(_shaderBrowserState.ReadyToggleText) ? "Show: ALL" : _shaderBrowserState.ReadyToggleText)
                        .OnClick(_shaderBrowserState.OnToggleReadyFilter)
                        .Sizing(UiSizeMode.Manual, UiSizeMode.Auto, 4),
                    Node("element_6b", UiNodeType.Button, "Popout_Config", 280, 70)
                        .WithText(string.IsNullOrWhiteSpace(_shaderBrowserState.PopoutButtonText) ? "Popout Config" : _shaderBrowserState.PopoutButtonText)
                        .OnClick(_shaderBrowserState.OnTogglePopout)
                        .Sizing(UiSizeMode.Manual, UiSizeMode.Auto, 4)
                );
        }

        private static UiNode BuildPackListPanel()
        {
            return Node("element_8", UiNodeType.Box, "Left_Pannel", 450, 70)
                .Visual(TextAnchor.MiddleLeft, false, false, ColorText, false, ColorPackListBackground, "")
                .Sizing(UiSizeMode.Manual, UiSizeMode.Auto, 2)
                .Scroll(true, false)
                .AddChildren(
                    Node("element_9", UiNodeType.Label, "Pack_List", 220, 48)
                        .WithText("Pack List")
                        .Visual(TextAnchor.MiddleCenter, false, false, ColorText, false, ColorPanelBackground, "")
                        .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 5)
                )
                .AddChildren(BuildPackCards().ToArray());
        }

        private static UiNode BuildPackDetailsPanel()
        {
            return Node("element_46", UiNodeType.Box, "Right_pannel", 220, 70)
                .Visual(TextAnchor.MiddleLeft, false, false, ColorText, false, ColorPanelBackground, "")
                .Sizing(UiSizeMode.Auto, UiSizeMode.Auto, 2)
                .Scroll(true, false)
                .AddChildren(
                    Node("element_47", UiNodeType.Label, "Pack_details", 220, 48)
                        .WithText("Pack Details")
                        .Visual(TextAnchor.MiddleCenter, false, false, ColorText, false, "", "")
                        .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 2),
                    Node("element_48", UiNodeType.Box, "Pack_details_container", 220, 70)
                        .LayoutConfig(SFS.UI.ModGUI.Type.Vertical, TextAnchor.UpperLeft, 24, 24, 24, 8, 24)
                        .Sizing(UiSizeMode.Auto, UiSizeMode.Auto, 2)
                        .AddChildren(
                            Node("element_49", UiNodeType.Label, "Pack_name", 220, 50)
                                .WithText(_shaderBrowserState.PackTitle ?? "ShaderPack Title")
                                .Visual(TextAnchor.MiddleCenter, false, false, ColorText, false, "", "")
                                .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 3),
                            Node("element_50", UiNodeType.Container, "Row", 220, 200)
                                .LayoutConfig(SFS.UI.ModGUI.Type.Horizontal, TextAnchor.UpperLeft, 6, 12, 12, 12, 12)
                                .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 3)
                                .AddChildren(
                                    Node("element_51", UiNodeType.Container, "Pack_tech", 300, 70)
                                        .LayoutConfig(SFS.UI.ModGUI.Type.Vertical, TextAnchor.UpperCenter, 4, 0, 0, 0, 0)
                                        .Sizing(UiSizeMode.Manual, UiSizeMode.Auto, 2)
                                        .AddChildren(
                                            Node("element_52", UiNodeType.Label, "Pack_version", 300, 30)
                                                .WithText(_shaderBrowserState.PackVersion ?? "Pack Version")
                                                .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 4),
                                            Node("element_53", UiNodeType.Label, "Pack_Type", 300, 30)
                                                .WithText(_shaderBrowserState.PackType ?? "Pack Type")
                                                .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 4),
                                            Node("element_54", UiNodeType.Label, "Pack_Scenes", 300, 30)
                                                .WithText(_shaderBrowserState.PackScenes ?? "Used Scenes: Any")
                                                .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 4),
                                            Node("element_55", UiNodeType.Label, "Pack_Cameras", 300, 30)
                                                .WithText(_shaderBrowserState.PackCameras ?? "Used Cameras: Any")
                                                .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 4)
                                        ),
                                    Node("element_56", UiNodeType.Box, "Pack_dependancies", 120, 340)
                                        .LayoutConfig(SFS.UI.ModGUI.Type.Vertical, TextAnchor.UpperLeft, 2, 12, 12, 8, 12)
                                        .Sizing(UiSizeMode.Auto, UiSizeMode.Auto, 2)
                                        .Scroll(true, false)
                                        .AddChildren(
                                            Node("element_57", UiNodeType.Container, "Pack_details_Title", 120, 60)
                                                .LayoutConfig(SFS.UI.ModGUI.Type.Horizontal, TextAnchor.MiddleLeft, 12, 6, 6, 6, 6)
                                                .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 4)
                                                .AddChildren(
                                                    Node("element_58", UiNodeType.Label, "Requirements", 120, 58)
                                                        .WithText("Requirements")
                                                        .Sizing(UiSizeMode.Auto, UiSizeMode.Auto, 2),
                                                    Node("element_59", UiNodeType.Label, "Status", 120, 40)
                                                        .WithText("Pack Status")
                                                        .Visual(TextAnchor.LowerRight, false, false, ColorText, false, "", "")
                                                        .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 2)
                                                )
                                        )
                                        .AddChildren(BuildRequirementCards().ToArray())
                                        )
                                    )
                                    .AddChildren(BuildConfigSectionNodes("element_cfg_tabs_above_box", includeGlobalTabs: true, wrapInBox: true))
                );
        }

        private static IEnumerable<UiNode> BuildRequirementCards()
        {
            IEnumerable<ShaderBrowserState.RequirementCardModel> requirements = _shaderBrowserState.Requirements.Count == 0
                ? new[]
                {
                    new ShaderBrowserState.RequirementCardModel
                    {
                        Name = "No requirements",
                        Path = "N/A",
                        Status = "Ready",
                        Type = "None",
                        IsLoaded = true,
                    },
                }
                : _shaderBrowserState.Requirements;

            return requirements.Select((requirement, i) =>
                Node($"element_req_{i}/0", UiNodeType.Box, "Dependancy", 220, 100)
                    .At(30, 30)
                    .Visual(TextAnchor.MiddleLeft, false, false, ColorText, true, requirement.IsLoaded ? ColorRequirementReady : ColorRequirementMissing, "")
                    .LayoutConfig(SFS.UI.ModGUI.Type.Horizontal, TextAnchor.UpperLeft, 12, 12, 12, 12, 12)
                    .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 1)
                    .AddChildren(
                        Node($"element_req_{i}/0/0", UiNodeType.Container, "Container_62", 220, 70)
                            .LayoutConfig(SFS.UI.ModGUI.Type.Vertical, TextAnchor.UpperLeft, 8, 0, 0, 0, 0)
                            .Sizing(UiSizeMode.Auto, UiSizeMode.Auto, 2)
                            .AddChildren(
                                Node($"element_req_{i}/0/0/0", UiNodeType.Label, "Name", 600, 70)
                                    .WithText(requirement.Name)
                                    .Sizing(UiSizeMode.Auto, UiSizeMode.Auto, 2),
                                Node($"element_req_{i}/0/0/1", UiNodeType.Box, "Box_61", 400, 30)
                                    .Visual(TextAnchor.MiddleLeft, false, false, ColorText, true, ColorNeutralDark, "")
                                    .LayoutConfig(SFS.UI.ModGUI.Type.Vertical, TextAnchor.UpperLeft, 0, 0, 0, 0, 0)
                                    .Sizing(UiSizeMode.Manual, UiSizeMode.Manual, 2)
                                    .AddChildren(
                                        Node($"element_req_{i}/0/0/1/0", UiNodeType.Label, "Shader_Path", 220, 70)
                                            .WithText(requirement.Path)
                                            .Visual(TextAnchor.MiddleCenter, false, false, ColorText, false, ColorNeutralLight, "")
                                            .Sizing(UiSizeMode.Auto, UiSizeMode.Auto, 1)
                                    )
                            ),
                        Node($"element_req_{i}/0/1", UiNodeType.Container, "Container_62", 220, 70)
                            .LayoutConfig(SFS.UI.ModGUI.Type.Vertical, TextAnchor.UpperLeft, 0, 0, 0, 0, 0)
                            .Sizing(UiSizeMode.Auto, UiSizeMode.Auto, 2)
                            .AddChildren(
                                Node($"element_req_{i}/0/1/0", UiNodeType.Label, "Shader Status", 220, 70)
                                    .WithText(requirement.Status)
                                    .Visual(TextAnchor.MiddleRight, false, false, ColorText, false, "", "")
                                    .Sizing(UiSizeMode.Auto, UiSizeMode.Auto, 2),
                                Node($"element_req_{i}/0/1/1", UiNodeType.Label, "Shader Type", 220, 70)
                                    .WithText(requirement.Type)
                                    .Visual(TextAnchor.MiddleRight, false, false, ColorText, false, "", "")
                                    .Sizing(UiSizeMode.Auto, UiSizeMode.Auto, 2)
                            )
                    ));
        }

        private static UiNode BuildTabButton(string path, string defaultName, int index, ShaderBrowserState.ConfigTabModel tab)
        {
            var title = string.IsNullOrWhiteSpace(tab.Name) ? $"{defaultName} {index + 1}" : tab.Name;
            return Node(path, UiNodeType.Button, $"Tab_Btn_{index + 1}", 220, 70)
                .WithText(tab.IsActive ? $"> {title}" : title)
                .OnClick(tab.OnSelect)
                .Visual(TextAnchor.MiddleCenter, false, false, ColorText, true, tab.IsActive ? ColorTabActiveBackground : ColorTabBackground, "")
                .Sizing(UiSizeMode.Manual, UiSizeMode.Manual, 3);
        }

        private static UiNode[] BuildConfigSectionNodes(string idPrefix, bool includeGlobalTabs, bool wrapInBox)
        {
            var nodes = new List<UiNode>(3);
            var hasGlobalTabs = includeGlobalTabs && _shaderBrowserState.ConfigTabs.Count > 1;
            if (hasGlobalTabs)
            {
                var tabButtons = _shaderBrowserState.ConfigTabs.Select((tab, i) =>
                    BuildTabButton($"{idPrefix}/tabs/0/0/{i}", "Tab", i, tab))
                    .ToArray();

                nodes.Add(
                    Node($"{idPrefix}/tabs/0", UiNodeType.Box, "Main Tab Group", 220, 70)
                        .At(30, 30)
                        .Visual(TextAnchor.MiddleLeft, false, false, ColorText, false, ColorGlobalTabBackground, "")
                        .LayoutConfig(SFS.UI.ModGUI.Type.Vertical, TextAnchor.UpperLeft, 0, 12, 12, 12, 12)
                        .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 1)
                        .AddChildren(
                            Node($"{idPrefix}/tabs/0/0", UiNodeType.Container, "Main_Tab_Row", 220, 70)
                                .LayoutConfig(SFS.UI.ModGUI.Type.Horizontal, TextAnchor.UpperLeft, 12, 12, 12, 12, 12)
                                .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 2)
                                .Scroll(false, true)
                                .AddChildren(tabButtons)
                        )
                );
            }

            var groupNodes = _shaderBrowserState.ConfigGroups.Count == 0
                ? new[]
                {
                    Node($"{idPrefix}_empty", UiNodeType.Label, "No_Config", 220, 48)
                        .WithText("No editable config fields.")
                        .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 1),
                }
                : _shaderBrowserState.ConfigGroups
                    .Select((group, i) => ConfigGroupElement.Build($"{idPrefix}_group_{i}", group))
                    .ToArray();

            if (!wrapInBox)
            {
                nodes.AddRange(groupNodes);
                return nodes.ToArray();
            }

            nodes.Add(
                Node("element_84", UiNodeType.Box, "Box_156", 220, 70)
                    .Visual(TextAnchor.MiddleLeft, false, false, ColorText, true, ColorConfigPanel, "")
                    .LayoutConfig(SFS.UI.ModGUI.Type.Vertical, TextAnchor.UpperLeft, 12, 120, 12, 12, 12)
                    .Sizing(UiSizeMode.Auto, UiSizeMode.Auto, 3)
                    .Scroll(true, false)
                    .AddChildren(groupNodes)
            );

            return nodes.ToArray();
        }

        private const int MaxConfigScrollViewportHeight = 380;

        private sealed class ConfigGroupElement
        {
            public static UiNode Build(string id, ShaderBrowserState.ConfigGroupModel group)
            {
                var hasTabs = group.Tabs.Count > 0;
                var isCollapsed = group.IsCollapsed;
                var fieldCount = Math.Max(1, group.Fields.Count);
                var estimatedBodyHeight = 24 + fieldCount * 56;
                var bodyHeight = hasTabs
                    ? Math.Max(80, estimatedBodyHeight)
                    : Math.Min(MaxConfigScrollViewportHeight, Math.Max(120, estimatedBodyHeight));

                var bodyNodes = isCollapsed
                    ? Array.Empty<UiNode>()
                    : new[]
                    {
                        Node($"{id}/0/2", UiNodeType.Container, "Row", 220, bodyHeight)
                            .LayoutConfig(SFS.UI.ModGUI.Type.Vertical, TextAnchor.UpperLeft, 12, 12, 12, 12, 12)
                            .Sizing(UiSizeMode.Auto, hasTabs ? UiSizeMode.Auto : UiSizeMode.Manual, 2)
                            .Scroll(true, false)
                            .AddChildren(group.Fields.Select((field, i) =>
                            {
                                var path = $"{id}/0/2/0/{i}";
                                return field.UseToggle
                                    ? Node(path, UiNodeType.ToggleWithLabel, "Config_Toggle", 220, 50)
                                        .WithText(field.Label)
                                        .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 4)
                                        .LabeledTexts(field.Label, string.Empty)
                                        .OnToggle(field.ToggleValueGetter ?? (() => false), field.OnToggle)
                                    : Node(path, UiNodeType.InputWithLabel, "Config_Input", 220, 50)
                                        .WithText(field.Label)
                                        .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 4)
                                        .LabeledTexts(field.Label, field.Value)
                                        .OnTextChanged(field.OnValueChanged)
                                        .OnTextInputBuilt(field.OnInputBuilt);
                            }).ToArray())
                    };

                var contentNode = !isCollapsed && hasTabs
                    ? Node($"{id}/0/1/0", UiNodeType.Box, "Tab Group", 220, 78 + bodyHeight + 36)
                        .At(30, 30)
                        .Visual(TextAnchor.MiddleLeft, false, false, ColorText, false, ColorConfigTabGroupBackground, "")
                        .LayoutConfig(SFS.UI.ModGUI.Type.Vertical, TextAnchor.UpperLeft, 0, 12, 12, 12, 44)
                        .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 1)
                        .AddChildren(
                            Node($"{id}/0/1/0/0", UiNodeType.Container, "Tab_Row", 220, 70)
                                .LayoutConfig(SFS.UI.ModGUI.Type.Horizontal, TextAnchor.UpperLeft, 12, 12, 12, 12, 12)
                                .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 2)
                                .Scroll(false, true)
                                .AddChildren(group.Tabs.Count == 0
                                    ? new[]
                                    {
                                        Node($"{id}/0/1/0/0/0", UiNodeType.Button, "Tab_Btn_1", 220, 70)
                                            .WithText("Config")
                                            .Sizing(UiSizeMode.Manual, UiSizeMode.Manual, 3),
                                    }
                                    : group.Tabs.Select((tab, i) =>
                                        BuildTabButton($"{id}/0/1/0/0/{i}", "Tab", i, tab))
                                        .ToArray()),
                            Node($"{id}/0/1/0/1", UiNodeType.Box, "Box_156", 220, bodyHeight)
                                .Visual(TextAnchor.MiddleLeft, false, false, ColorText, true, ColorConfigTabContentBackground, "")
                                .Scroll(true, false)
                                .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 2)
                                .AddChildren(bodyNodes)
                        )
                    : null;

                var groupHeight = isCollapsed
                    ? 90
                    : (hasTabs
                        ? Math.Max(220, 70 + (78 + bodyHeight + 36) + 6 + 2)
                        : Math.Max(170, 70 + bodyHeight + 6 + 2));

                return Node($"{id}/0", UiNodeType.Box, "Group", 220, groupHeight)
                    .At(30, 30)
                    .Visual(TextAnchor.MiddleLeft, false, false, ColorText, false, ColorConfigGroupBackground, "")
                    .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 1)
                    .AddChildren(
                        Node($"{id}/0/0", UiNodeType.Box, "Top Row", 220, 70)
                            .LayoutConfig(SFS.UI.ModGUI.Type.Horizontal, TextAnchor.UpperLeft, 12, 12, 12, 12, 12)
                            .Sizing(UiSizeMode.Auto, UiSizeMode.Manual, 2)
                            .AddChildren(
                                Node($"{id}/0/0/0", UiNodeType.Button, "Button_92", 50, 70)
                                    .WithText(isCollapsed ? ">" : "▼")
                                    .OnClick(group.OnToggleCollapsed)
                                    .Sizing(UiSizeMode.Manual, UiSizeMode.Auto, 2),
                                Node($"{id}/0/0/1", UiNodeType.Label, "Config_group_name", 220, 70)
                                    .WithText(string.IsNullOrWhiteSpace(group.Name) ? "Config" : group.Name)
                                    .Visual(TextAnchor.MiddleCenter, false, false, ColorText, false, "", "")
                                    .Sizing(UiSizeMode.Auto, UiSizeMode.Auto, 2)
                            )
                    )
                    .AddChildren(contentNode == null ? bodyNodes : new[] { contentNode });
            }
        }

        private static Transform? _activeParent;
        private static readonly List<GameObject> _activeRoots = [];
        private static Window? _activeWindowElement;

        /// <summary>
        /// Updates the live root window's title text directly, without tearing down and rebuilding
        /// the whole generated tree. Used for cheap, frequent changes (e.g. the save indicator
        /// suffix) that would otherwise cause a full-tree rebuild — and the scroll/focus jitter
        /// that comes with it — for a few characters of text.
        /// </summary>
        public static void SetWindowTitle(string title)
        {
            if (_activeWindowElement == null)
                return;

            try { _activeWindowElement.Title = title ?? string.Empty; }
            catch { _activeWindowElement = null; }
        }

        public enum UiNodeType
        {
            Window,
            ClosableWindow,
            Container,
            Box,
            Label,
            Button,
            TextInput,
            InputWithLabel,
            Toggle,
            ToggleWithLabel,
        }

        public enum UiSizeMode
        {
            Manual,
            Auto,
        }

        public static GameObject? RenderToParent(Transform parent)
        {
            return RenderToParentCore(parent, true);
        }

        private static GameObject? RenderToParentCore(Transform parent, bool returnFirstRoot)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            RemoveTrackedRoots();
            _activeParent = parent;
            Transform? firstRoot = null;
            foreach (var node in Define())
            {
                var built = node.Build(parent);
                if (returnFirstRoot && firstRoot == null)
                    firstRoot = built;
                if (built != null)
                    _activeRoots.Add(built.gameObject);
            }

            return returnFirstRoot && firstRoot != null ? firstRoot.gameObject : null;
        }

        public static void Refresh()
        {
            if (_activeParent != null)
            {
                RenderToParentCore(_activeParent, false);
                return;
            }

            UiNode.UpdateUi();
        }

        public static void Remove()
        {
            RemoveTrackedRoots();
            _activeParent = null;
            _activeWindowElement = null;
        }

        private static void RemoveTrackedRoots()
        {
            foreach (var root in _activeRoots)
            {
                var rootTransform = root != null ? root.transform : null;
                if (rootTransform == null)
                    continue;

                var isForeign = _activeParent != null && (ReferenceEquals(rootTransform, _activeParent) || rootTransform.parent != _activeParent);
                if (!isForeign)
                    UnityEngine.Object.Destroy(root);
            }

            _activeRoots.Clear();
        }

        private static UiNode Node(string id, UiNodeType type, string name, int width, int height)
        {
            return new UiNode
            {
                Id = id,
                Type = type,
                Name = name,
                Width = width,
                Height = height,
            };
        }

        public sealed class UiNode
        {
            public string Id { get; set; } = string.Empty;
            public UiNodeType Type { get; set; } = UiNodeType.Container;
            public string Name { get; set; } = string.Empty;
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public string Text { get; set; } = string.Empty;
            public TextAnchor TextAlignment { get; set; } = TextAnchor.MiddleLeft;
            public bool TextColorOverride { get; set; }
            public string TextColor { get; set; } = "#ffffff";
            public bool BackgroundColorOverride { get; set; }
            public string BackgroundColor { get; set; } = string.Empty;
            public string BorderColor { get; set; } = string.Empty;
            public bool Multiline { get; set; }
            public SFS.UI.ModGUI.Type Layout { get; set; } = SFS.UI.ModGUI.Type.Vertical;
            public TextAnchor ChildAlignment { get; set; } = TextAnchor.UpperLeft;
            public int Spacing { get; set; } = 12;
            public int PaddingLeft { get; set; } = 12;
            public int PaddingRight { get; set; } = 12;
            public int PaddingTop { get; set; } = 12;
            public int PaddingBottom { get; set; } = 12;
            public UiSizeMode WidthMode { get; set; } = UiSizeMode.Manual;
            public UiSizeMode HeightMode { get; set; } = UiSizeMode.Manual;
            public int SiblingCount { get; set; } = 1;
            public bool ScrollVertical { get; set; }
            public bool ScrollHorizontal { get; set; }
            public string LabelText { get; set; } = string.Empty;
            public string ControlText { get; set; } = string.Empty;
            public Action? OnClickAction { get; set; }
            public Func<bool>? ToggleValueGetter { get; set; }
            public Action? OnToggleAction { get; set; }
            public UnityAction<string>? OnTextChangedAction { get; set; }
            public Action<TextInput>? OnTextInputBuiltAction { get; set; }
            public List<UiNode> Children { get; set; } = [];
            private int? _allocatedWidth;
            private int? _allocatedHeight;

            public UiNode At(int x, int y) { X = x; Y = y; return this; }
            public UiNode WithText(string text) { Text = text; return this; }
            public UiNode Visual(TextAnchor textAlignment, bool multiline, bool textColorOverride, string textColor, bool backgroundColorOverride, string backgroundColor, string borderColor) { TextAlignment = textAlignment; Multiline = multiline; TextColorOverride = textColorOverride; TextColor = textColor; BackgroundColorOverride = backgroundColorOverride; BackgroundColor = backgroundColor; BorderColor = borderColor; return this; }
            public UiNode LayoutConfig(SFS.UI.ModGUI.Type layout, TextAnchor childAlignment, int spacing, int paddingLeft, int paddingRight, int paddingTop, int paddingBottom) { Layout = layout; ChildAlignment = childAlignment; Spacing = spacing; PaddingLeft = paddingLeft; PaddingRight = paddingRight; PaddingTop = paddingTop; PaddingBottom = paddingBottom; return this; }
            public UiNode Sizing(UiSizeMode widthMode, UiSizeMode heightMode, int siblingCount) { WidthMode = widthMode; HeightMode = heightMode; SiblingCount = Math.Max(1, siblingCount); return this; }
            public UiNode Scroll(bool vertical, bool horizontal) { ScrollVertical = vertical; ScrollHorizontal = horizontal; return this; }
            public UiNode LabeledTexts(string labelText, string controlText) { LabelText = labelText; ControlText = controlText; return this; }
            // Note: these run while constructing the plain-data node tree, before Build() creates
            // any GameObject, so they must not touch Canvas.ForceUpdateCanvases() — see UpdateUi().
            public UiNode AddChildren(params UiNode[] children) { if (children == null || children.Length == 0) return this; Children.AddRange(children); return this; }
            public UiNode OnClick(Action? action) { OnClickAction = action; return this; }
            public UiNode OnToggle(Func<bool>? valueGetter, Action? action) { ToggleValueGetter = valueGetter; OnToggleAction = action; return this; }
            public UiNode OnTextChanged(Action<string>? action) { OnTextChangedAction = action == null ? null : new UnityAction<string>(action); return this; }
            public UiNode OnTextInputBuilt(Action<TextInput>? action) { OnTextInputBuiltAction = action; return this; }

            public static void UpdateUi()
            {   // force a canvas update to ensure any dynamic changes to generated UI are applied.
                Canvas.ForceUpdateCanvases();
            }

            public Transform Build(Transform parent)
            {
                var resolvedWidth = ResolveWidth(parent);
                var resolvedHeight = ResolveHeight(parent);
                object element;
                Transform transform;
                Transform childParent;
                var title = string.IsNullOrWhiteSpace(Text) ? Name : Text;

                switch (Type)
                {
                    case UiNodeType.Window:
                    case UiNodeType.ClosableWindow:
                    {
                        var draggableWindow = _shaderBrowserState != null && _shaderBrowserState.ConfigOnlyMode;

                        if (Type == UiNodeType.ClosableWindow)
                        {
                            var w = UIToolsBuilder.CreateClosableWindow(parent, DeterministicId(Id), resolvedWidth, resolvedHeight, X, Y, draggable: draggableWindow, savePosition: draggableWindow, titleText: title, minimized: false);
                            if (w.rectTransform != null)
                                ClampRectTransformToScreen(w.rectTransform);
                            ConfigureLayout(w);
                            if (ScrollVertical)
                                w.EnableScrolling(SFS.UI.ModGUI.Type.Vertical);
                            if (ScrollHorizontal)
                                w.EnableScrolling(SFS.UI.ModGUI.Type.Horizontal);
                            element = w;
                            transform = w.gameObject.transform;
                            childParent = w.ChildrenHolder;
                        }
                        else
                        {
                            var w = Builder.CreateWindow(parent, DeterministicId(Id), resolvedWidth, resolvedHeight, X, Y, draggable: draggableWindow, savePosition: draggableWindow, titleText: title);
                            if (w.rectTransform != null)
                                ClampRectTransformToScreen(w.rectTransform);
                            ConfigureLayout(w);
                            if (ScrollVertical)
                                w.EnableScrolling(SFS.UI.ModGUI.Type.Vertical);
                            if (ScrollHorizontal)
                                w.EnableScrolling(SFS.UI.ModGUI.Type.Horizontal);
                            element = w;
                            transform = w.gameObject.transform;
                            childParent = w.ChildrenHolder;
                            _activeWindowElement = w;
                        }
                        break;
                    }
                    case UiNodeType.Container:
                    case UiNodeType.Box:
                    {
                        Transform root;
                        if (Type == UiNodeType.Container)
                        {
                            var c = Builder.CreateContainer(parent, X, Y);
                            c.Size = new Vector2(resolvedWidth, resolvedHeight);
                            root = c.gameObject.transform;
                            element = c;
                        }
                        else
                        {
                            var b = Builder.CreateBox(parent, resolvedWidth, resolvedHeight, X, Y, opacity: 0.35f);
                            root = b.gameObject.transform;
                            element = b;
                        }

                        DisableContentSizeFitter(root);
                        var contentParent = ScrollVertical || ScrollHorizontal ? EnsureScrollableContentHost(root, ScrollVertical, ScrollHorizontal) : root;
                        ConfigureLayout(contentParent);
                        ApplyScrolling(element, root, ScrollVertical, ScrollHorizontal);
                        transform = root;
                        childParent = contentParent;
                        break;
                    }
                    case UiNodeType.Label:
                    {
                        var label = Builder.CreateLabel(parent, resolvedWidth, resolvedHeight, X, Y, Text);
                        element = label;
                        transform = label.gameObject.transform;
                        childParent = label.gameObject.transform;
                        break;
                    }
                    case UiNodeType.Button:
                    {
                        var clickAction = OnClickAction == null
                            ? null
                            : new Action(() =>
                            {
                                try { OnClickAction(); }
                                catch (Exception ex) { Debug.LogError($"[GeneratedUi] Button '{Id}' click handler failed: {ex}"); }
                                GeneratedUiController.RequestUiRebuild();
                            });
                        var button = Builder.CreateButton(parent, resolvedWidth, resolvedHeight, X, Y, clickAction, Text);
                        element = button;
                        transform = button.gameObject.transform;
                        childParent = button.gameObject.transform;
                        break;
                    }
                    case UiNodeType.TextInput:
                    {
                        var input = Builder.CreateTextInput(parent, resolvedWidth, resolvedHeight, X, Y, Text, OnTextChangedAction);
                        OnTextInputBuiltAction?.Invoke(input);
                        element = input;
                        transform = input.gameObject.transform;
                        childParent = input.gameObject.transform;
                        break;
                    }
                    case UiNodeType.InputWithLabel:
                    {
                        (element, transform, childParent) = BuildLabeledNode(parent, resolvedWidth, resolvedHeight, false);
                        break;
                    }
                    case UiNodeType.Toggle:
                    {
                        var toggleValue = ToggleValueGetter ?? (() => false);
                        var toggle = Builder.CreateToggle(parent, toggleValue, X, Y, () =>
                        {
                            try
                            {
                                if (OnToggleAction != null)
                                    OnToggleAction();
                                else
                                    OnClickAction?.Invoke();
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"[GeneratedUi] Toggle '{Id}' handler failed: {ex}");
                            }

                            GeneratedUiController.RequestUiRebuild();
                        });
                        element = toggle;
                        transform = toggle.gameObject.transform;
                        childParent = toggle.gameObject.transform;
                        break;
                    }
                    case UiNodeType.ToggleWithLabel:
                    {
                        (element, transform, childParent) = BuildLabeledNode(parent, resolvedWidth, resolvedHeight, true);
                        break;
                    }
                    default:
                        throw new InvalidOperationException($"Unsupported UiNode type: {Type}");
                }

                ApplyVisualStyle(element);
                ApplyChildAutoSizing(resolvedWidth, resolvedHeight);

                foreach (var child in Children)
                    child.Build(childParent);

                return transform;
            }

            private (object element, Transform transform, Transform childParent) BuildLabeledNode(Transform parent, int resolvedWidth, int resolvedHeight, bool toggle)
            {
                var holder = Builder.CreateContainer(parent, X, Y);
                holder.Size = new Vector2(resolvedWidth, resolvedHeight);
                holder.CreateLayoutGroup(SFS.UI.ModGUI.Type.Horizontal, TextAnchor.MiddleLeft, Math.Max(0, Spacing), new RectOffset(PaddingLeft, PaddingRight, PaddingTop, PaddingBottom), true);
                var labelText = string.IsNullOrWhiteSpace(LabelText) ? Text : LabelText;
                var controlText = string.IsNullOrWhiteSpace(ControlText) ? Text : ControlText;
                var fullHeight = Math.Max(20, resolvedHeight);
                var labelWidth = Math.Clamp(resolvedWidth / 2, 20, Math.Max(20, resolvedWidth));
                Builder.CreateLabel(holder.gameObject.transform, labelWidth, fullHeight, 0, 0, labelText);
                if (toggle)
                {
                    Builder.CreateToggle(holder.gameObject.transform, ToggleValueGetter ?? (() => false), 0, 0, () =>
                    {
                        try
                        {
                            if (OnToggleAction != null)
                                OnToggleAction();
                            else
                                OnClickAction?.Invoke();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[GeneratedUi] Toggle '{Id}' handler failed: {ex}");
                        }

                        GeneratedUiController.RequestUiRebuild();
                    });
                }
                else
                {
                    var w = Math.Max(20, resolvedWidth - labelWidth - Math.Max(0, Spacing));
                    var i = Builder.CreateTextInput(holder.gameObject.transform, w, fullHeight, 0, 0, controlText, OnTextChangedAction);
                    OnTextInputBuiltAction?.Invoke(i);
                }

                return (holder, holder.gameObject.transform, holder.gameObject.transform);
            }

            private int ResolveWidth(Transform parent) => _allocatedWidth != null ? Math.Max(1, _allocatedWidth.Value) : ResolveAxisSize(parent, true, WidthMode, Width);
            private int ResolveHeight(Transform parent) => _allocatedHeight != null ? Math.Max(1, _allocatedHeight.Value) : ResolveAxisSize(parent, false, HeightMode, Height);

            private int ResolveAxisSize(Transform parent, bool isWidthAxis, UiSizeMode mode, int manualSize)
            {
                var m = Math.Max(1, manualSize);
                if (mode != UiSizeMode.Auto || parent is not RectTransform r)
                    return m;
                var a = isWidthAxis ? r.rect.width : r.rect.height;
                var g = parent.GetComponent<HorizontalOrVerticalLayoutGroup>();
                if (g != null)
                {
                    a = Math.Max(1f, a - (isWidthAxis ? g.padding.left + g.padding.right : g.padding.top + g.padding.bottom));
                    var c = Math.Max(1, SiblingCount);
                    if ((isWidthAxis && g is HorizontalLayoutGroup) || (!isWidthAxis && g is VerticalLayoutGroup))
                        a = Math.Max(1f, (a - g.spacing * Math.Max(0, c - 1)) / c);
                }
                return Math.Max(1, (int)Math.Round(a));
            }

            private void ApplyChildAutoSizing(int parentWidth, int parentHeight)
            {
                if (Children.Count == 0)
                    return;

                var innerWidth = Math.Max(1, parentWidth - PaddingLeft - PaddingRight);
                var innerHeight = Math.Max(1, parentHeight - PaddingTop - PaddingBottom);
                var horizontal = Layout == SFS.UI.ModGUI.Type.Horizontal;
                var spacingTotal = Math.Max(0, Children.Count - 1) * Math.Max(0, Spacing);

                var innerPrimary = horizontal ? innerWidth : innerHeight;
                var primaryAvailable = Math.Max(1, innerPrimary - spacingTotal);
                var manualTotal = Children
                    .Where(c => (horizontal ? c.WidthMode : c.HeightMode) != UiSizeMode.Auto)
                    .Sum(c => Math.Max(1, horizontal ? c.Width : c.Height));
                var autoCount = Children.Count(c => (horizontal ? c.WidthMode : c.HeightMode) == UiSizeMode.Auto);
                var remaining = Math.Max(1, primaryAvailable - manualTotal);
                var autoPrimary = autoCount > 0 ? Math.Max(1, remaining / autoCount) : 0;

                foreach (var child in Children)
                {
                    child._allocatedWidth = horizontal
                        ? (child.WidthMode == UiSizeMode.Auto ? autoPrimary : null)
                        : (child.WidthMode == UiSizeMode.Auto ? innerWidth : null);
                    child._allocatedHeight = horizontal
                        ? (child.HeightMode == UiSizeMode.Auto ? innerHeight : null)
                        : (child.HeightMode == UiSizeMode.Auto ? autoPrimary : null);
                }
            }

            private void ConfigureLayout(Window window) => window.CreateLayoutGroup(Layout, ChildAlignment, Spacing, new RectOffset(PaddingLeft, PaddingRight, PaddingTop, PaddingBottom), true);

            private void ConfigureLayout(Transform target)
            {
                var g = Layout == SFS.UI.ModGUI.Type.Horizontal
                    ? (HorizontalOrVerticalLayoutGroup)(target.GetComponent<HorizontalLayoutGroup>() ?? target.gameObject.AddComponent<HorizontalLayoutGroup>())
                    : target.GetComponent<VerticalLayoutGroup>() ?? target.gameObject.AddComponent<VerticalLayoutGroup>();
                g.childAlignment = ChildAlignment;
                g.spacing = Spacing;
                g.padding = new RectOffset(PaddingLeft, PaddingRight, PaddingTop, PaddingBottom);
                g.childControlWidth = g.childControlHeight = g.childForceExpandWidth = g.childForceExpandHeight = false;
            }

            private static Transform EnsureScrollableContentHost(Transform host, bool vertical, bool horizontal)
            {
                var hostRect = host as RectTransform ?? host.gameObject.GetComponent<RectTransform>();
                if (hostRect == null)
                    throw new InvalidOperationException("Scrollable host must have a RectTransform.");

                var viewport = host.Find("__ui_maker_viewport") as RectTransform;
                if (viewport == null)
                {
                    var viewportObject = new GameObject("__ui_maker_viewport", typeof(RectTransform));
                    viewport = viewportObject.GetComponent<RectTransform>();
                    viewport.SetParent(host, false);
                }

                viewport.anchorMin = Vector2.zero;
                viewport.anchorMax = Vector2.one;
                viewport.pivot = new Vector2(0.5f, 0.5f);
                viewport.anchoredPosition = Vector2.zero;
                viewport.offsetMin = Vector2.zero;
                viewport.offsetMax = Vector2.zero;

                var viewportMask = viewport.GetComponent<RectMask2D>();
                if (viewportMask == null)
                    viewportMask = viewport.gameObject.AddComponent<RectMask2D>();
                viewportMask.enabled = vertical || horizontal;

                var viewportImage = viewport.GetComponent<Image>();
                if (viewportImage == null)
                    viewportImage = viewport.gameObject.AddComponent<Image>();
                viewportImage.color = new Color(0f, 0f, 0f, 0f);
                viewportImage.raycastTarget = true;

                var content = viewport.Find("__ui_maker_content") as RectTransform;
                if (content == null)
                {
                    var contentObject = new GameObject("__ui_maker_content", typeof(RectTransform));
                    content = contentObject.GetComponent<RectTransform>();
                    content.SetParent(viewport, false);
                }

                content.localScale = Vector3.one;
                content.anchoredPosition = Vector2.zero;

                if (vertical && !horizontal)
                {
                    content.anchorMin = new Vector2(0f, 1f);
                    content.anchorMax = new Vector2(1f, 1f);
                    content.pivot = new Vector2(0.5f, 1f);
                    content.sizeDelta = Vector2.zero;
                }
                else if (horizontal && !vertical)
                {
                    content.anchorMin = new Vector2(0f, 0f);
                    content.anchorMax = new Vector2(0f, 1f);
                    content.pivot = new Vector2(0f, 0.5f);
                    content.sizeDelta = Vector2.zero;
                }
                else
                {
                    content.anchorMin = new Vector2(0f, 1f);
                    content.anchorMax = new Vector2(0f, 1f);
                    content.pivot = new Vector2(0f, 1f);
                    content.sizeDelta = Vector2.zero;
                }

                var contentFitter = content.GetComponent<ContentSizeFitter>();
                if (contentFitter == null)
                    contentFitter = content.gameObject.AddComponent<ContentSizeFitter>();
                contentFitter.horizontalFit = horizontal ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
                contentFitter.verticalFit = vertical ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;

                var scrollRect = host.gameObject.GetComponent<ScrollRect>();
                if (scrollRect == null)
                    scrollRect = host.gameObject.AddComponent<ScrollRect>();
                scrollRect.viewport = viewport;
                scrollRect.content = content;
                scrollRect.vertical = vertical;
                scrollRect.horizontal = horizontal;
                scrollRect.scrollSensitivity = 25f;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.inertia = true;
                scrollRect.enabled = vertical || horizontal;
                scrollRect.horizontalNormalizedPosition = 0f;
                scrollRect.verticalNormalizedPosition = 1f;

                return content;
            }

            private static void DisableContentSizeFitter(Transform transform)
            {
                var f = transform.GetComponent<ContentSizeFitter>();
                if (f == null)
                    return;
                f.horizontalFit = f.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                f.enabled = false;
            }

            private static void ApplyScrolling(object element, Transform transform, bool vertical, bool horizontal)
            {
                if (element != null)
                {
                    var t = element.GetType();
                    var m = t.GetMethod("EnableScrolling", new[] { typeof(SFS.UI.ModGUI.Type) });
                    if (m != null)
                    {
                        if (vertical)
                            m.Invoke(element, new object[] { SFS.UI.ModGUI.Type.Vertical });
                        if (horizontal)
                            m.Invoke(element, new object[] { SFS.UI.ModGUI.Type.Horizontal });
                    }
                    foreach (var n in new[] { "scrollVertical", "scrollHorizontal", "ScrollVertical", "ScrollHorizontal", "vertical", "horizontal", "Vertical", "Horizontal" })
                        TrySetBoolMember(t, element, n, n.IndexOf("Vertical", StringComparison.OrdinalIgnoreCase) >= 0 ? vertical : horizontal);
                }
                if (transform == null)
                    return;
                var s = transform.GetComponent<ScrollRect>() ?? transform.GetComponentInChildren<ScrollRect>(true);
                if (s != null)
                {
                    s.vertical = vertical;
                    s.horizontal = horizontal;
                    s.scrollSensitivity = 25f;
                    s.inertia = true;
                    s.movementType = ScrollRect.MovementType.Clamped;
                    s.enabled = vertical || horizontal;
                }
                var r = transform.GetComponentInChildren<RectMask2D>(true);
                if (r != null)
                    r.enabled = vertical || horizontal;
            }

            private static void TrySetBoolMember(System.Type type, object target, string memberName, bool value)
            {
                var f = type.GetField(memberName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.IgnoreCase);
                if (f != null && f.FieldType == typeof(bool))
                    f.SetValue(target, value);
                else
                {
                    var p = type.GetProperty(memberName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.IgnoreCase);
                    if (p != null && p.CanWrite && p.PropertyType == typeof(bool))
                        p.SetValue(target, value, null);
                }
            }


            private void ApplyVisualStyle(object element)
            {
                if (TextColorOverride && TryParseColor(TextColor, out var textColor))
                {
                    TrySetColorProperty(element, "TextColor", textColor);
                    TrySetColorProperty(element, "TitleColor", textColor);
                }

                if (BackgroundColorOverride && TryParseColor(BackgroundColor, out var backgroundColor))
                {
                    TrySetColorProperty(element, "WindowColor", backgroundColor);
                    TrySetColorProperty(element, "FieldColor", backgroundColor);
                    TrySetColorProperty(element, "Color", backgroundColor);
                }

                TrySetTextAlignmentProperty(element, "TextAlignment", TextAlignment);
                if (element is Component rootComponent)
                    ApplyTextAlignmentToChildren(rootComponent.transform, TextAlignment);
            }

            private static bool TryParseColor(string value, out Color color) { color = default; return !string.IsNullOrWhiteSpace(value) && ColorUtility.TryParseHtmlString(value, out color); }

            private static void TrySetColorProperty(object target, string propertyName, Color color) { var p = target.GetType().GetProperty(propertyName); if (p != null && p.CanWrite && p.PropertyType == typeof(Color)) p.SetValue(target, color, null); }

            private static void TrySetTextAlignmentProperty(object target, string propertyName, TextAnchor value)
            {
                var p = target.GetType().GetProperty(propertyName);
                if (p == null || !p.CanWrite || !p.PropertyType.IsEnum)
                    return;
                try { p.SetValue(target, Enum.Parse(p.PropertyType, MapTextAlignmentName(p.PropertyType, value), true), null); } catch { }
            }

            private static string MapTextAlignmentName(System.Type enumType, TextAnchor value)
            {
                var fullName = enumType.FullName ?? enumType.Name;
                if (fullName.IndexOf("TMPro.TextAlignmentOptions", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return value switch
                    {
                        TextAnchor.UpperLeft => "TopLeft",
                        TextAnchor.UpperCenter => "Top",
                        TextAnchor.UpperRight => "TopRight",
                        TextAnchor.MiddleLeft => "Left",
                        TextAnchor.MiddleCenter => "Center",
                        TextAnchor.MiddleRight => "Right",
                        TextAnchor.LowerLeft => "BottomLeft",
                        TextAnchor.LowerCenter => "Bottom",
                        TextAnchor.LowerRight => "BottomRight",
                        _ => "Center",
                    };
                }

                return value.ToString();
            }

            private static void ApplyTextAlignmentToChildren(Transform root, TextAnchor value)
            {
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null)
                        continue;

                    var typeName = component.GetType().Name;
                    if (!typeName.Contains("Text", StringComparison.OrdinalIgnoreCase) && !typeName.Contains("Label", StringComparison.OrdinalIgnoreCase))
                        continue;

                    TrySetTextAlignmentProperty(component, "alignment", value);
                    TrySetTextAlignmentProperty(component, "Alignment", value);
                    TrySetTextAlignmentProperty(component, "TextAlignment", value);
                }
            }

            private static void ClampRectTransformToScreen(RectTransform rectTransform)
            {
                var corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);

                var minX = Mathf.Min(corners[0].x, corners[2].x);
                var minY = Mathf.Min(corners[0].y, corners[2].y);
                var maxX = Mathf.Max(corners[0].x, corners[2].x);
                var maxY = Mathf.Max(corners[0].y, corners[2].y);

                var deltaX = 0f;
                var deltaY = 0f;
                if (minX < 0f)
                    deltaX = -minX;
                else if (maxX > Screen.width)
                    deltaX = Screen.width - maxX;

                if (minY < 0f)
                    deltaY = -minY;
                else if (maxY > Screen.height)
                    deltaY = Screen.height - maxY;

                if (Mathf.Abs(deltaX) < 0.01f && Mathf.Abs(deltaY) < 0.01f)
                    return;

                rectTransform.position += new Vector3(deltaX, deltaY, 0f);
            }

            private static int DeterministicId(string id)
            {
                unchecked
                {
                    var hash = 23;
                    for (var i = 0; i < id.Length; i++)
                        hash = hash * 31 + id[i];
                    return hash & int.MaxValue;
                }
            }
        }
    }

    public static class GeneratedUiController
    {
        private sealed class PackModel
        {
            public string Name = string.Empty;
            public string Description = string.Empty;
            public string Author = "Unknown";
            public string Version = "1.0";
            public bool CanActivate;
            public IShaderPack Pack;
            public List<ShaderRequirementAttribute> Requirements = new List<ShaderRequirementAttribute>();
        }

        private sealed class EditableField
        {
            public string ShaderName = string.Empty;
            public string GroupName = string.Empty;
            public string TabName = string.Empty;
            public string Path = string.Empty;
            public string Label = string.Empty;
            public string Value = string.Empty;
            public System.Type ValueType;
            public bool IsBoolean;
        }

        private sealed class UiShaderState
        {
            public readonly Dictionary<string, TextInput> Inputs = new Dictionary<string, TextInput>(StringComparer.Ordinal);
            public string LastSnapshot = string.Empty;
        }

        [Serializable]
        private sealed class PersistentUiState
        {
            public string SearchText = string.Empty;
            public bool ShowReadyOnly;
            public string PackFilterMode = "All";
            public bool ShaderBrowserExpanded = true;
            public bool ConfigPopoutMode;
            public string SelectedPackName = string.Empty;
            public string ViewedPackName = string.Empty;
            public List<PersistentCollapsedGroup> CollapsedGroups = new List<PersistentCollapsedGroup>();
            public List<PersistentTabSelection> SelectedTabs = new List<PersistentTabSelection>();
        }

        [Serializable]
        private sealed class PersistentCollapsedGroup
        {
            public string PackName = string.Empty;
            public string GroupKey = string.Empty;
        }

        [Serializable]
        private sealed class PersistentTabSelection
        {
            public string PackName = string.Empty;
            public string GroupKey = string.Empty;
            public string TabName = string.Empty;
        }

        /// <summary>
        /// Marks whichever content (collapsed hint or expanded browser) is actually live in the
        /// scene right now. Toggle decisions are made by reading this off the real GameObject
        /// instead of trusting _shaderBrowserExpanded's history of toggles — the host can enable/
        /// disable/rebuild our content in ways our own flags never see, so the flags reliably
        /// drift from what's actually on screen. The marker can't drift: it either exists and is
        /// active, or it doesn't.
        /// </summary>
        private sealed class BrowserStateMarker : MonoBehaviour
        {
            public bool Expanded;
        }

        /// <summary>
        /// Looks at what's actually built under <paramref name="parent"/> right now rather than
        /// trusting any cached flag. Returns true (expanded browser live), false (collapsed hint
        /// live), or null (nothing of ours currently live under this parent).
        /// </summary>
        private static bool? DetermineCurrentlyExpanded(Transform parent)
        {
            if (parent == null)
                return null;

            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child == null || !child.gameObject.activeInHierarchy)
                    continue;

                var marker = child.GetComponent<BrowserStateMarker>();
                if (marker != null)
                    return marker.Expanded;
            }

            return null;
        }

        // Lives on Window(Clone) itself (not on _menuRoot which SFS destroys after BuildMenuPage
        // returns). Applies the expanded size and hides the native panel every frame until
        // RestoreAndDestroy() is called. Holding the restore values here makes the state
        // independent of the static fields that CleanupMenuState() clears.
        private sealed class ExpandResizeHelper : MonoBehaviour
        {
            public RectTransform Window;
            public Vector2 ExpandedSize;
            public GameObject PanelToHide;
            public Vector2 OriginalSize;
            public Vector2 OriginalAnchorMin;
            public Vector2 OriginalAnchorMax;
            public Vector2 OriginalPivot;
            public Vector2 OriginalAnchoredPosition;
            public bool PanelOriginallyActive;
            private int _countdown = 2;
            private bool _loggedFirstApply;

            private void Update()
            {
                if (_countdown > 0) { _countdown--; return; }
                if (Window != null)
                {
                    var c = new Vector2(0.5f, 0.5f);
                    Window.anchorMin = c;
                    Window.anchorMax = c;
                    Window.pivot = c;
                    Window.sizeDelta = ExpandedSize;
                    Window.anchoredPosition = Vector2.zero;
                    if (!_loggedFirstApply) { _loggedFirstApply = true; Debug.Log($"[ExpandResizeHelper] First apply size={Window.sizeDelta}"); }
                }
                if (PanelToHide != null && PanelToHide.activeSelf)
                    PanelToHide.SetActive(false);
            }

            public void RestoreAndDestroy()
            {
                if (Window != null)
                {
                    Window.anchorMin = OriginalAnchorMin;
                    Window.anchorMax = OriginalAnchorMax;
                    Window.pivot = OriginalPivot;
                    Window.sizeDelta = OriginalSize;
                    Window.anchoredPosition = OriginalAnchoredPosition;
                    Debug.Log($"[ExpandResizeHelper] Restored size={Window.sizeDelta}");
                }
                if (PanelToHide != null)
                    PanelToHide.SetActive(PanelOriginallyActive);
                Destroy(this);
            }
        }

        private sealed class MenuLifecycle : MonoBehaviour
        {
            private void OnDisable() { if (!SuppressCleanup) CleanupMenuState(); }
            private void OnDestroy() { if (!SuppressCleanup) CleanupMenuState(); }

            private void Update()
            {

                // _menuRoot != null alone isn't enough: it's non-null for BOTH the collapsed
                // hint panel and the full expanded browser, and only the expanded browser owns
                // these per-frame behaviors. _shaderBrowserExpanded records which of the two is
                // currently built, so both conditions are needed together.
                var isOpen = _menuRoot != null;
                var isExpanded = isOpen && _shaderBrowserExpanded;

                if (isOpen && Input.GetKeyDown(KeyCode.Escape))
                    HandleEscapeClose();

                if (isExpanded)
                {
                    TickSaveIndicator();
                    UpdateTypingState();
                }

                if (isOpen)
                    TryProcessPendingPackApply();

                if (isExpanded)
                    TryProcessPendingUiRebuild();


                if (_pendingScrollRestore)
                    TryRestoreScrollState();

                if (isExpanded && _pendingSearchFocusRestore)
                    TryRestoreSearchInputFocus();
            }

            /// <summary>
            /// Set directly on this instance right before an intentional destroy/rebuild.
            /// UnityEngine.Object.Destroy() is deferred to end-of-frame, so a shared static
            /// "suppress" flag that gets reset synchronously right after the Destroy() call
            /// races the real, later OnDestroy — silently wiping all menu state after every
            /// rebuild. Carrying the flag on the dying instance itself is immune to that race.
            /// </summary>
            public bool SuppressCleanup;
        }

        /// <summary>
        /// Commits a config text field's pending value on Enter or on losing focus. SFS.UI.ModGUI's
        /// TextInput only exposes a single per-keystroke onChange callback, no true submit/blur
        /// event — the same gap BP-Editor's NumSubmitTracker works around for the same UI framework.
        /// </summary>
        private sealed class FieldCommitTracker : MonoBehaviour
        {
            public Func<string>? GetPending;
            public Action? OnCommit;
            private bool _wasSelected;

            private void Update()
            {
                var selected = IsSelected();

                if (selected && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
                    OnCommit?.Invoke();
                else if (_wasSelected && !selected)
                    OnCommit?.Invoke();

                _wasSelected = selected;
            }

            private bool IsSelected()
            {
                var selectedObject = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
                if (selectedObject == null)
                    return false;

                for (var t = selectedObject.transform; t != null; t = t.parent)
                    if (t.gameObject == gameObject)
                        return true;

                return false;
            }
        }

        private static readonly List<PackModel> _packs = new List<PackModel>(16);
        private static readonly Dictionary<string, UiShaderState> _shaderUiState = new Dictionary<string, UiShaderState>(StringComparer.Ordinal);
        private static readonly Dictionary<string, HashSet<string>> _collapsedGroupsByPack = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> _selectedTabByPackAndGroup = new Dictionary<string, string>(StringComparer.Ordinal);
        private const string PersistentUiStateKey = "shaders.generated-ui.browser-state.v1";

        private enum PackFilterMode
        {
            All,
            Ready,
            Failed,
        }

        private static bool _menuRegistered;
        private static bool _menuOpen;
        private static bool _shaderBrowserExpanded = false;
        private static bool _configPopoutMode;
        private static bool _persistentUiStateLoaded;
        private static bool _settingsStateCaptured;
        private static int _lastConfigClickPackIndex = -1;
        private static float _lastConfigClickTime;
        private static float _lastCameraInactiveRebindAt;
        private static float _saveIndicatorUntil;
        private static bool _pendingSearchFocusRestore;
        private static bool _committingPendingInputEdits;

        /// <summary>
        /// True while one of our own text fields (search box or a config input) has keyboard
        /// focus. A Harmony patch on SFS.Input.KeybindingsPC's key-state methods (see
        /// KeyboardInputBlockPatches in Shaders.cs) reads this to stop the game's own keybindings
        /// from firing while the user is typing a value here — otherwise e.g. typing "2" into a
        /// number field also fires whatever action is bound to that key in-game.
        /// </summary>
        public static bool IsTyping { get; private set; }

        private static void UpdateTypingState()
        {
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            IsTyping = selected != null && selected.GetComponentInParent<InputField>() != null;
        }
        private static int _selectedPackIndex = -1;
        private static int _viewedPackIndex = -1;
        private static string _persistedSelectedPackName = string.Empty;
        private static string _persistedViewedPackName = string.Empty;
        private static string _searchText = string.Empty;
        private static PackFilterMode _packFilterMode = PackFilterMode.All;
        private static string _saveIndicatorText = string.Empty;
        private const float PackApplyDebounceSeconds = 0.04f;
        private static bool _pendingPackApply;
        private static float _nextPackApplyAt;
        private const float UiRebuildDebounceSeconds = 0.06f;
        private static bool _pendingUiRebuild;
        private static bool _pendingUiRebuildPersistState;
        private static float _nextUiRebuildAt;
        private static float _lastUiRebuildAt;
        private static bool _pendingExpandResize;
        private static int _pendingExpandResizeFrames;

        private static GameObject _menuRoot;
        private static Transform _menuParent;
        private static readonly Dictionary<string, Vector2> _scrollStateByPath = new Dictionary<string, Vector2>(StringComparer.Ordinal);
        private static bool _pendingScrollRestore;
        private static int _scrollRestorePassesRemaining;
        // Found by exact scene path from the "UI" root — this is what the pre-refactor, confirmed
        // working version did (git history), rather than deriving the window transform from the
        // ChildrenHolder passed into BuildMenuPage (which turned out to be a different, inner
        // transform than the actual on-screen window rect).
        private static RectTransform _settingsWindow;
        private static RectTransform _modWindowRect;
        private static RectTransform _hostRect;
        private static GameObject _nativeSettingsMenuHolder;
        private static bool _nativeSettingsMenuHolderWasActive;
        private static Vector2 _settingsWindowOriginalPos;
        private static Vector2 _settingsWindowOriginalAnchorMin;
        private static Vector2 _settingsWindowOriginalAnchorMax;
        private static Vector2 _settingsWindowOriginalPivot;
        private static Vector2 _settingsWindowOriginalSize;
        private static Vector2 _modWindowOriginalAnchorMin;
        private static Vector2 _modWindowOriginalAnchorMax;
        private static Vector2 _modWindowOriginalPivot;
        private static Vector2 _modWindowOriginalSizeDelta;
        private static Vector2 _modWindowOriginalAnchoredPosition;
        private static Vector2 _hostRectOriginalSize;

        // Window(Clone)'s own RectTransform is only the outer bounding box — the actual clipped
        // viewport is its "Mask" child (and that mask's "Children Holder" child), which can carry
        // their own fixed anchors/size instead of stretching with the parent. Growing the outer
        // window alone then has no visible effect since the mask still clips to its old bounds, so
        // both are forced to stretch-fill while expanded and restored on collapse. A
        // ContentSizeFitter on the window (if present) is disabled for the same reason: it would
        // silently overwrite our forced sizeDelta on the next layout pass.
        private static RectTransform _modWindowMaskRect;
        private static Vector2 _modWindowMaskOriginalAnchorMin;
        private static Vector2 _modWindowMaskOriginalAnchorMax;
        private static Vector2 _modWindowMaskOriginalPivot;
        private static Vector2 _modWindowMaskOriginalSizeDelta;
        private static Vector2 _modWindowMaskOriginalAnchoredPosition;
        private static RectTransform _modWindowChildrenHolderRect;
        private static Vector2 _modWindowChildrenHolderOriginalAnchorMin;
        private static Vector2 _modWindowChildrenHolderOriginalAnchorMax;
        private static Vector2 _modWindowChildrenHolderOriginalPivot;
        private static Vector2 _modWindowChildrenHolderOriginalSizeDelta;
        private static Vector2 _modWindowChildrenHolderOriginalAnchoredPosition;
        private static bool _modWindowFitterWasEnabled;

        private static bool _stateChangedSubscribed;

        public static void Init()
        {
            Try<object>.Run(() =>
            {
                ShaderPackManager.Initialize();
                LoadPersistentUiState();
                LoadPackModels();
                RegisterMenu();

                if (!_stateChangedSubscribed)
                {
                    _stateChangedSubscribed = true;
                    ShaderPackManager.StateChanged += OnShaderPackManagerStateChanged;
                }

                return null;
            }).Match(_ => { }, ex => Debug.LogError($"[GeneratedUiController] Initialization failed: {ex.Message}"));
        }

        /// <summary>
        /// Packs can finish loading (or activate) asynchronously after the browser is already
        /// open — e.g. a pack's CodeAssembly resolves seconds after this mod's Load(). Rebuild the
        /// already-open browser instead of leaving it showing stale pack/requirement state.
        /// </summary>
        private static void OnShaderPackManagerStateChanged()
        {
            // Only the expanded browser has pack cards/config to refresh; rebuilding while the
            // collapsed hint panel is showing would silently replace it with the full browser.
            if (_menuRoot == null || !_shaderBrowserExpanded)
                return;

            QueueUiRebuild(persistState: false);
        }

        public static void HandleEscapeClose()
        {
            if (_menuRoot == null)
                return;

            if (_configPopoutMode)
                return;

            // Escape should leave the browser in the same "collapsed hint" state that clicking
            // the category button to collapse does, not tear the whole page down to nothing.
            var parent = _menuParent != null ? _menuParent : (_menuRoot.transform != null ? _menuRoot.transform.parent : null);
            if (parent == null)
            {
                CloseShaderBrowser();
                return;
            }

            _shaderBrowserExpanded = false;
            SavePersistentUiState();
            CollapseToHint(parent);
        }

        public static void NotifyCameraInactive()
        {
            var now = Time.unscaledTime;
            if ((now - _lastCameraInactiveRebindAt) < 0.35f)
                return;

            _lastCameraInactiveRebindAt = now;

            // Delegate entirely to ShaderPackManager's own persisted-selection restore instead of
            // re-activating from this UI's cached _selectedPackIndex/_packs snapshot: that cache is
            // only refreshed when the browser rebuilds, so it can drift from the manager's true
            // state (e.g. right after a deactivate) and spuriously reactivate a pack the user just
            // turned off, or fail to clear stale effects when nothing should be active.
            ShaderPackManager.RebindActivePackToCameras();
        }

        public static void UpdateShaderProvidedArgs(string shaderName, object args)
        {
            if (string.IsNullOrWhiteSpace(shaderName) || args == null)
                return;

            ShaderPackManager.SetCurrentArgs(shaderName, args);

            var state = GetOrCreateShaderUiState(shaderName);
            PruneDeadInputs(state);
            var flat = FlattenObject(args, string.Empty);
            var snapshot = string.Join("|", flat.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
            if (string.Equals(snapshot, state.LastSnapshot, StringComparison.Ordinal))
                return;

            state.LastSnapshot = snapshot;
            var overrides = ShaderPackManager.GetUserOverrides(shaderName);

            foreach (var kvp in flat)
            {
                if (overrides != null && overrides.ContainsKey(kvp.Key))
                    continue;
                var key = $"{shaderName}:{kvp.Key}";
                if (!state.Inputs.TryGetValue(key, out var input) || input == null)
                    continue;

                var text = FormatValue(kvp.Value);
                if (!string.Equals(input.Text, text, StringComparison.Ordinal))
                    input.Text = text;
            }
        }

        public static Dictionary<string, object>? GetUserArgs(string shaderName)
        {
            if (string.IsNullOrWhiteSpace(shaderName))
                return null;

            var overrides = ShaderPackManager.GetUserOverrides(shaderName);
            return overrides?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);
        }

        public static object? GetCurrentArgs(string shaderName)
        {
            if (string.IsNullOrWhiteSpace(shaderName))
                return null;

            return ShaderPackManager.GetCurrentArgs(shaderName);
        }

        public static void RequestUiRebuild()
        {
            // Only the expanded browser's own buttons/toggles/fields call this; the collapsed
            // hint panel has none. Requiring both the live root and _shaderBrowserExpanded keeps
            // this from ever replacing the collapsed hint with the full browser by mistake.
            if (_menuRoot == null || !_shaderBrowserExpanded)
                return;

            if (_menuParent == null && _menuRoot.transform != null)
                _menuParent = _menuRoot.transform.parent;

            if (_menuParent == null)
                return;

            _menuOpen = true;
            QueueUiRebuild(false, immediate: true);
        }

        private static void RegisterMenu()
        {
            if (_menuRegistered)
                return;

            _menuRegistered = true;
            ConfigurationMenu.Add("Shader Manager", new (string, Func<Transform, GameObject>)[]
            {
                ("Shaders", BuildMenuPage)
            });
        }

        private static void LoadPackModels()
        {
            var previousViewedPackName = _viewedPackIndex >= 0 && _viewedPackIndex < _packs.Count
                ? _packs[_viewedPackIndex].Name
                : null;

            _packs.Clear();

            foreach (var pack in ShaderPackManager.GetAllPacks())
            {
                var packType = pack.GetType();
                var metadata = packType.GetCustomAttribute<ShaderPackAttribute>();
                var requirements = packType
                    .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .SelectMany(field => field.GetCustomAttributes<ShaderRequirementAttribute>())
                    .ToList();

                var canActivate = requirements.All(req =>
                {
                    var module = req.ResolveModule();
                    if (module == null)
                        return req.Condition != ShaderRequirementAttribute.FailCondition.DisablePack;

                    return req.Condition != ShaderRequirementAttribute.FailCondition.DisablePack || (module.IsLoaded && module.Shader != null);
                });

                _packs.Add(new PackModel
                {
                    Name = pack.Name ?? packType.Name,
                    Description = metadata?.Description ?? $"Shader pack: {pack.Name}",
                    Author = metadata?.Author ?? "Unknown",
                    Version = metadata?.Version ?? "1.0",
                    CanActivate = canActivate,
                    Pack = pack,
                    Requirements = requirements
                });
            }

            var selectedName = ShaderPackManager.SelectedPackName;
            _selectedPackIndex = string.IsNullOrWhiteSpace(selectedName)
                ? -1
                : _packs.FindIndex(p => string.Equals(p.Name, selectedName, StringComparison.Ordinal));

            if (_selectedPackIndex < 0 && !string.IsNullOrWhiteSpace(_persistedSelectedPackName))
                _selectedPackIndex = _packs.FindIndex(p => string.Equals(p.Name, _persistedSelectedPackName, StringComparison.Ordinal));

            if (!string.IsNullOrWhiteSpace(previousViewedPackName))
            {
                _viewedPackIndex = _packs.FindIndex(p => string.Equals(p.Name, previousViewedPackName, StringComparison.Ordinal));
                if (_viewedPackIndex < 0)
                    _viewedPackIndex = _selectedPackIndex;
            }
            else
            {
                _viewedPackIndex = !string.IsNullOrWhiteSpace(_persistedViewedPackName)
                    ? _packs.FindIndex(p => string.Equals(p.Name, _persistedViewedPackName, StringComparison.Ordinal))
                    : _selectedPackIndex;

                if (_viewedPackIndex < 0)
                    _viewedPackIndex = _selectedPackIndex;
            }

            var availablePackNames = new HashSet<string>(_packs.Select(p => p.Name), StringComparer.Ordinal);
            foreach (var staleKey in _collapsedGroupsByPack.Keys.Where(key => !availablePackNames.Contains(key)).ToList())
                _collapsedGroupsByPack.Remove(staleKey);

            foreach (var staleKey in _selectedTabByPackAndGroup.Keys
                .Where(key =>
                {
                    var separator = key.IndexOf('|');
                    if (separator <= 0)
                        return true;
                    var packName = key.Substring(0, separator);
                    return !availablePackNames.Contains(packName);
                })
                .ToList())
                _selectedTabByPackAndGroup.Remove(staleKey);

            CleanupShaderUiState(availablePackNames);
            PersistCurrentPackNames();
        }

        private static GameObject? BuildMenuPage(Transform parent)
        {
            try
            {
                _menuParent = parent;

                // While popped out, the live browser lives under the scene UI root, not this
                // settings-tab parent — DetermineCurrentlyExpanded(parent) would find nothing here
                // and (wrongly) conclude the browser needs to collapse, tearing down the actual
                // popout window via CloseShaderBrowser(). Leave it untouched and show a small
                // standalone notice in the settings tab instead.
                if (_configPopoutMode)
                {
                    DestroyPopoutHintPanel();
                    _popoutHintPanel = BuildPopoutActiveHint(parent);
                    return _popoutHintPanel;
                }

                DestroyPopoutHintPanel();

                // Ask the scene what's actually live rather than blindly negating a flag: the
                // host can enable/disable/rebuild this page in ways our own toggle never
                // observes, so a plain "!_shaderBrowserExpanded" reliably drifts from reality.
                // Nothing live yet (fresh selection) starts collapsed.
                var currentlyExpanded = DetermineCurrentlyExpanded(parent);
                _shaderBrowserExpanded = currentlyExpanded != true;
                if (currentlyExpanded == null)
                    _shaderBrowserExpanded = false;

                Debug.Log($"[GeneratedUiController] BuildMenuPage: currentlyExpanded={currentlyExpanded} -> newState={(_shaderBrowserExpanded ? "expand" : "collapse")} configPopoutMode={_configPopoutMode} parentPath={GetTransformPath(parent)}");

                SavePersistentUiState();

                if (!_shaderBrowserExpanded)
                    return CollapseToHint(parent);

                return BuildExpandedBrowser(parent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GeneratedUiController] BuildMenuPage failed, restoring native settings to avoid leaving it hidden: {ex}");
                CleanupMenuState();
                return null;
            }
        }

        private static GameObject BuildExpandedBrowser(Transform parent)
        {
            var targetParent = _configPopoutMode ? ResolvePopoutParent(parent) : parent;
            _menuParent = targetParent;

            CloseShaderBrowser();
            LoadPackModels();

            if (_configPopoutMode)
            {   // Detached mode should not rely on or reshape the settings window host.
                RestoreSettingsUiState();
            }
            else
            {
                SaveExpandedUiState();
                EnterExpandedBrowserUiMode();
                // SFS's framework may reset the window size/anchors after BuildMenuPage returns
                // (canvas rebuild, layout pass, etc.). Schedule a re-apply on the next few Update
                // ticks so our expanded size wins after SFS finishes its post-callback work.
                _pendingExpandResize = true;
                _pendingExpandResizeFrames = 2;
            }

            return RenderGeneratedBrowser(targetParent, false);
        }

        private static GameObject CollapseToHint(Transform parent)
        {
            _menuParent = parent;
            CloseShaderBrowser();
            _menuRoot = BuildCollapsedShaderBrowserHint(parent);
            _menuOpen = _menuRoot != null;
            if (_menuRoot != null)
            {
                var lc = _menuRoot.GetComponent<MenuLifecycle>() ?? _menuRoot.AddComponent<MenuLifecycle>();
                lc.SuppressCleanup = true;

                var marker = _menuRoot.GetComponent<BrowserStateMarker>() ?? _menuRoot.AddComponent<BrowserStateMarker>();
                marker.Expanded = false;
            }

            return _menuRoot;
        }

        private static GameObject? _popoutHintPanel;

        private static void DestroyPopoutHintPanel()
        {
            if (_popoutHintPanel != null)
                UnityEngine.Object.Destroy(_popoutHintPanel);
            _popoutHintPanel = null;
        }

        private static GameObject BuildPopoutActiveHint(Transform parent)
        {
            var contentSize = ConfigurationMenu.ContentSize;
            var width = Math.Max(360, Mathf.RoundToInt(contentSize.x * 0.9f));
            var height = Math.Max(140, Mathf.RoundToInt(contentSize.y * 0.25f));

            var panel = Builder.CreateWindow(parent, Builder.GetRandomID(), width, height, 0, 0, false, false, 0.1f, "Pack Browser");
            panel.CreateLayoutGroup(SFS.UI.ModGUI.Type.Vertical, TextAnchor.UpperLeft, 5, new RectOffset(1, 1, 1, 1), true);
            var textWidth = Mathf.Max(260, width - 2);
            var hintRect = Builder.CreateLabel(panel, textWidth, 70, 0, 0, "Shader config is open in a separate popped-out window.").rectTransform;
            var hintText = hintRect != null ? hintRect.GetComponentInChildren<Text>() : null;
            if (hintText != null)
                hintText.alignment = TextAnchor.UpperLeft;

            return panel.gameObject;
        }

        private static Transform ResolvePopoutParent(Transform fallbackParent)
        {   // Prefer stable scene-level UI roots so popout can stay usable outside settings panels.
            var uiElement = GameObject.Find("UI");
            if (uiElement != null && uiElement.transform != null)
                return uiElement.transform;

            return fallbackParent;
        }

        private static GameObject BuildCollapsedShaderBrowserHint(Transform parent)
        {
            var contentSize = ConfigurationMenu.ContentSize;
            var width = Math.Max(360, Mathf.RoundToInt(contentSize.x * 0.9f));
            var height = Math.Max(180, Mathf.RoundToInt(contentSize.y * 0.35f));

            var panel = Builder.CreateWindow(parent, Builder.GetRandomID(), width, height, 0, 0, false, false, 0.1f, "Pack Browser");
            panel.CreateLayoutGroup(SFS.UI.ModGUI.Type.Vertical, TextAnchor.UpperLeft, 5, new RectOffset(1, 1, 1, 1), true);
            var textWidth = Mathf.Max(260, width - 2);
            var collapsedRect = Builder.CreateLabel(panel, textWidth, 38, 0, 0, "Shader Browser is collapsed.").rectTransform;
            var hintRect = Builder.CreateLabel(panel, textWidth, 56, 0, 0, "Click the Shaders mod settings button again\nto open the full browser.").rectTransform;
            var collapsedText = collapsedRect != null ? collapsedRect.GetComponentInChildren<Text>() : null;
            if (collapsedText != null)
                collapsedText.alignment = TextAnchor.UpperLeft;

            var hintText = hintRect != null ? hintRect.GetComponentInChildren<Text>() : null;
            if (hintText != null)
                hintText.alignment = TextAnchor.UpperLeft;

            return panel.gameObject;
        }

        private static GeneratedLayout.ShaderBrowserState BuildGeneratedBrowserState()
        {
            var state = BuildBaseBrowserState();
            PopulatePackCards(state);

            if (_viewedPackIndex < 0 || _viewedPackIndex >= _packs.Count)
            {
                ApplyNoSelectionPackDetails(state);
                return state;
            }

            var viewedPack = _packs[_viewedPackIndex];
            EnsurePackArgsLoadedForViewing(_viewedPackIndex);
            PopulateViewedPackDetails(state, viewedPack);
            BuildConfigTabsAndGroups(state, viewedPack);
            return state;
        }

        private static GeneratedLayout.ShaderBrowserState BuildBaseBrowserState()
        {
            var saveSuffix = string.IsNullOrWhiteSpace(_saveIndicatorText) ? string.Empty : $" - {_saveIndicatorText}";
            return new GeneratedLayout.ShaderBrowserState
            {
                Title = "Main Window" + saveSuffix,
                SearchText = _searchText ?? string.Empty,
                ReadyToggleText = _packFilterMode switch
                {
                    PackFilterMode.Ready => "Show: READY",
                    PackFilterMode.Failed => "Show: FAILED",
                    _ => "Show: ALL",
                },
                ConfigOnlyMode = _configPopoutMode,
                PopoutButtonText = _configPopoutMode ? "Back To Settings" : "Popout Config",
                OnSearchChanged = value => _searchText = value ?? string.Empty,
                OnSearch = () => PersistAndRebuild(false),
                OnClear = () =>
                {
                    _searchText = string.Empty;
                    PersistAndRebuild();
                },
                OnToggleReadyFilter = () =>
                {
                    _packFilterMode = _packFilterMode switch
                    {
                        PackFilterMode.All => PackFilterMode.Ready,
                        PackFilterMode.Ready => PackFilterMode.Failed,
                        _ => PackFilterMode.All,
                    };
                    PersistAndRebuild();
                },
                OnTogglePopout = () =>
                {
                    if (_configPopoutMode)
                    {
                        _configPopoutMode = false;
                        _shaderBrowserExpanded = false;
                        SavePersistentUiState();
                        CloseShaderBrowser();
                        return;
                    }

                    _configPopoutMode = true;
                    SavePersistentUiState();

                    var currentParent = _menuParent;
                    if (currentParent == null && _menuRoot != null && _menuRoot.transform != null)
                        currentParent = _menuRoot.transform.parent;

                    if (currentParent != null)
                    {
                        BuildExpandedBrowser(currentParent);
                        return;
                    }

                    PersistAndRebuild();
                }
            };
        }

        private static void PopulatePackCards(GeneratedLayout.ShaderBrowserState state)
        {
            var activeName = ShaderPackManager.GetActivePack()?.Name;
            var visible = FilterPacks();
            for (var i = 0; i < visible.Count; i++)
            {
                var sourceIndex = visible[i];
                var pack = _packs[sourceIndex];
                var isActive = string.Equals(activeName, pack.Name, StringComparison.Ordinal);
                var isChecked = isActive || sourceIndex == _selectedPackIndex;
                var localIndex = sourceIndex;

                state.AddPack(
                    pack.Name,
                    pack.Version,
                    pack.Description,
                    isChecked,
                    () =>
                    {
                        if (_selectedPackIndex == localIndex)
                        {
                            _selectedPackIndex = -1;
                            ShaderPackManager.SetSelectedPackName(null);
                            ShaderPackManager.DeactivateCurrentPack();
                        }
                        else
                        {
                            _selectedPackIndex = localIndex;
                            _viewedPackIndex = localIndex;
                            ApplySelectedPack();
                        }

                        PersistCurrentPackNames();
                        PersistAndRebuild();
                    },
                    () =>
                    {
                        var now = Time.unscaledTime;
                        var isDoubleClick = _lastConfigClickPackIndex == localIndex && (now - _lastConfigClickTime) <= 0.35f;
                        _lastConfigClickPackIndex = localIndex;
                        _lastConfigClickTime = now;

                        _viewedPackIndex = localIndex;
                        EnsurePackArgsLoadedForViewing(localIndex);
                        if (isDoubleClick)
                        {
                            _selectedPackIndex = localIndex;
                            ApplySelectedPack();
                        }

                        PersistCurrentPackNames();
                        PersistAndRebuild();
                    });
            }
        }

        private static void ApplyNoSelectionPackDetails(GeneratedLayout.ShaderBrowserState state)
        {
            state.PackTitle = "ShaderPack Title";
            state.PackVersion = "Pack Version";
            state.PackType = "Pack Type";
            state.PackScenes = "Used Scenes: Any";
            state.PackCameras = "Used Cameras: Any";
            state.AddRequirement("Select a pack", "N/A", "Idle", "Info", true);
        }

        private static void PopulateViewedPackDetails(GeneratedLayout.ShaderBrowserState state, PackModel viewedPack)
        {
            state.PackTitle = viewedPack.Name;
            state.PackVersion = string.IsNullOrWhiteSpace(viewedPack.Version) ? "Pack Version" : viewedPack.Version;
            state.PackType = viewedPack.CanActivate ? "Ready" : "Missing requirements";
            var routeSummary = BuildPackRouteSummary(viewedPack);
            state.PackScenes = routeSummary.Scenes;
            state.PackCameras = routeSummary.Cameras;

            foreach (var req in viewedPack.Requirements)
            {
                var module = req.ResolveModule();
                var name = !string.IsNullOrWhiteSpace(req.CustomName)
                    ? req.CustomName
                    : module?.Name ?? req.ShaderModuleType?.Name ?? "Unknown Module";
                var isLoaded = module != null && module.IsLoaded && module.Shader != null;
                state.AddRequirement(
                    name,
                    module?.Shader != null ? module.Shader.name : "Shader not loaded",
                    isLoaded ? "Loaded" : "Missing",
                    req.Condition.ToString(),
                    isLoaded);
            }

            if (state.Requirements.Count == 0)
                state.AddRequirement("No requirements", "N/A", "Ready", "None", true);
        }

        private static void BuildConfigTabsAndGroups(GeneratedLayout.ShaderBrowserState state, PackModel viewedPack)
        {
            var fields = BuildEditableFields(viewedPack);

            static string ResolveFieldTab(EditableField field)
            {
                if (!string.IsNullOrWhiteSpace(field.TabName))
                    return field.TabName.Trim();

                return "General";
            }

            var fieldsWithTabs = fields
                .Select(field => (Field: field, Tab: ResolveFieldTab(field)))
                .ToList();

            var tabFirstIndex = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < fieldsWithTabs.Count; i++)
            {
                var tab = fieldsWithTabs[i].Tab;
                if (!tabFirstIndex.ContainsKey(tab))
                    tabFirstIndex[tab] = i;
            }

            var globalTabs = fieldsWithTabs
                .Select(item => item.Tab)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(tab => tabFirstIndex[tab])
                .ToList();

            var globalTabGroupKey = "__global_tabs__";
            var selectedGlobalTab = globalTabs.Count > 1
                ? GetSelectedTabForPackGroup(viewedPack.Name, globalTabGroupKey, globalTabs)
                : globalTabs.FirstOrDefault() ?? "General";

            if (globalTabs.Count > 1)
            {
                foreach (var tabName in globalTabs)
                {
                    var localTabName = tabName;
                    state.ConfigTabs.Add(new GeneratedLayout.ShaderBrowserState.ConfigTabModel
                    {
                        Name = localTabName,
                        IsActive = string.Equals(localTabName, selectedGlobalTab, StringComparison.Ordinal),
                        OnSelect = () =>
                        {
                            _selectedTabByPackAndGroup[$"{viewedPack.Name}|{globalTabGroupKey}"] = localTabName;
                            PersistAndRebuild();
                        }
                    });
                }
            }

            var validGroups = new HashSet<string>(StringComparer.Ordinal);
            var validPackGroupKeys = new HashSet<string>(StringComparer.Ordinal);
            if (globalTabs.Count > 1)
                validPackGroupKeys.Add($"{viewedPack.Name}|{globalTabGroupKey}");

            foreach (var group in fieldsWithTabs.GroupBy(item =>
                $"{item.Field.ShaderName}|{(string.IsNullOrWhiteSpace(item.Field.GroupName) ? "General" : item.Field.GroupName)}",
                StringComparer.Ordinal))
            {
                var firstItem = group.First();
                var shaderName = firstItem.Field.ShaderName;
                var groupName = string.IsNullOrWhiteSpace(firstItem.Field.GroupName) ? "General" : firstItem.Field.GroupName;
                var groupKey = string.IsNullOrWhiteSpace(groupName) ? shaderName : $"{shaderName}:{groupName}";
                validGroups.Add(groupKey);
                var packGroupKey = $"{viewedPack.Name}|{groupKey}";
                validPackGroupKeys.Add(packGroupKey);

                var visibleFields = group
                    .Where(item => string.Equals(item.Tab, selectedGlobalTab, StringComparison.Ordinal))
                    .Select(item => item.Field)
                    .OrderBy(item => item.Path, StringComparer.Ordinal)
                    .ToList();

                if (visibleFields.Count == 0)
                    continue;

                var groupModel = state.AddConfigGroup(
                    $"{shaderName} / {groupName}",
                    _collapsedGroupsByPack.TryGetValue(viewedPack.Name, out var collapsedGroups) && collapsedGroups.Contains(groupKey),
                    () =>
                    {
                        ToggleConfigGroupCollapsed(viewedPack.Name, groupKey);
                        PersistAndRebuild();
                    });

                foreach (var field in visibleFields)
                {
                    var fieldShaderName = field.ShaderName;
                    var path = field.Path;
                    if (field.IsBoolean)
                    {
                        groupModel.AddBoolField(
                            field.Label,
                            () => ReadBoolFieldValue(fieldShaderName, path),
                            () => ToggleBoolFieldValue(fieldShaderName, path));
                        continue;
                    }

                    var pendingText = field.Value;
                    groupModel.AddField(
                        field.Label,
                        field.Value,
                        // Only tracked here, not applied: committing partial in-progress text like
                        // "0." or "-" would fail to parse, fall back to the last valid value, and
                        // rewrite the input's text back to that fallback mid-edit — silently eating
                        // the character just typed. The actual commit happens in FieldCommitTracker
                        // below (Enter or losing focus), plus onEndEdit as a redundant-safe backup.
                        value => pendingText = value ?? string.Empty,
                        input =>
                        {
                            if (input == null || string.IsNullOrWhiteSpace(fieldShaderName) || string.IsNullOrWhiteSpace(path))
                                return;

                            var stateForInput = GetOrCreateShaderUiState(fieldShaderName);
                            PruneDeadInputs(stateForInput);
                            stateForInput.Inputs[$"{fieldShaderName}:{path}"] = input;

                            // SFS.UI.ModGUI's TextInput only exposes a single onChange (fires per
                            // keystroke); it has no true submit/blur event of its own. This tracker
                            // — the same technique used by BP-Editor's NumSubmitTracker for exactly
                            // this UI framework — polls Enter and focus loss directly instead, which
                            // is what actually and reliably commits a value.
                            var tracker = input.gameObject.AddComponent<FieldCommitTracker>();
                            tracker.GetPending = () => pendingText;
                            tracker.OnCommit = () => ApplyFieldValue(fieldShaderName, path, pendingText, applySelectedPack: true, rebuildUi: false);

                            var inputField = input.gameObject != null
                                ? input.gameObject.GetComponent<InputField>() ?? input.gameObject.GetComponentInChildren<InputField>(true)
                                : null;
                            if (inputField != null)
                            {
                                inputField.onEndEdit.AddListener(value => ApplyFieldValue(fieldShaderName, path, value ?? string.Empty, applySelectedPack: true, rebuildUi: false));
                            }
                        });
                }
            }

            CleanupPackUiState(viewedPack.Name, validGroups, validPackGroupKeys);
        }

        private static string BuildRouteLabel(string[]? items, ShaderRouteMode mode, string moduleName)
        {
            var label = items is null or { Length: 0 } ? "Any" : string.Join(", ", items);
            return mode == ShaderRouteMode.ExcludeListed ? $"{moduleName}: all except {label}" : $"{moduleName}: {label}";
        }

        private static (string Scenes, string Cameras) BuildPackRouteSummary(PackModel pack)
        {
            var shaders = pack?.Pack?.GetShaders();
            if (shaders == null || shaders.Count == 0)
                return ("Used Scenes: Any", "Used Cameras: Any");

            var routes = shaders
                .Where(module => module != null)
                .Select(module => (Module: module, Route: module.GetType().GetCustomAttribute<ShaderRouteAttribute>()))
                .Where(entry => entry.Route != null)
                .ToList();

            var sceneRules = routes.Select(entry => BuildRouteLabel(entry.Route.Scenes, entry.Route.SceneMode, entry.Module.Name)).ToList();
            var cameraRules = routes.Select(entry => BuildRouteLabel(entry.Route.Cameras, entry.Route.CameraMode, entry.Module.Name)).ToList();

            return (
                sceneRules.Count == 0 ? "Used Scenes: Any" : "Used Scenes: " + string.Join(" | ", sceneRules),
                cameraRules.Count == 0 ? "Used Cameras: Any" : "Used Cameras: " + string.Join(" | ", cameraRules));
        }

        private static void EnsurePackArgsLoadedForViewing(int packIndex)
        {
            if (packIndex < 0 || packIndex >= _packs.Count)
                return;

            var pack = _packs[packIndex];
            var shaders = pack.Pack?.GetShaders();
            if (shaders == null)
                return;

            foreach (var shader in shaders)
            {
                if (shader == null || string.IsNullOrWhiteSpace(shader.Name))
                    continue;

                var existing = ShaderPackManager.GetCurrentArgs(shader.Name) ?? pack.Pack.GetArgs(shader.Name);
                if (existing != null)
                    continue;

                var argsType = ResolveShaderArgsType(shader);
                if (argsType == null)
                    continue;

                Try<object>.Run(() =>
                {
                    var created = global::shaders.ShaderArgDefaults.Apply(Activator.CreateInstance(argsType));
                    if (created != null)
                        ShaderPackManager.SetCurrentArgs(shader.Name, created);

                    return null;
                }).Match(
                    _ => { },
                    ex => Debug.LogWarning($"[GeneratedUiController] Failed to initialize config args for shader '{shader.Name}': {ex.Message}"));
            }
        }

        private static System.Type? ResolveShaderArgsType(IShaderModule shader)
        {
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

        private static string GetSelectedTabForPackGroup(string packName, string groupKey, IReadOnlyList<string> tabNames)
        {
            if (string.IsNullOrWhiteSpace(packName) || string.IsNullOrWhiteSpace(groupKey) || tabNames == null || tabNames.Count == 0)
                return string.Empty;

            var key = $"{packName}|{groupKey}";
            if (_selectedTabByPackAndGroup.TryGetValue(key, out var selectedTab) && tabNames.Contains(selectedTab))
                return selectedTab;

            var fallbackTab = tabNames[0];
            _selectedTabByPackAndGroup[key] = fallbackTab;
            return fallbackTab;
        }

        private static void ToggleConfigGroupCollapsed(string packName, string groupName)
        {
            if (string.IsNullOrWhiteSpace(packName) || string.IsNullOrWhiteSpace(groupName))
                return;

            if (!_collapsedGroupsByPack.TryGetValue(packName, out var groups))
            {
                groups = new HashSet<string>(StringComparer.Ordinal);
                _collapsedGroupsByPack[packName] = groups;
            }

            if (!groups.Add(groupName))
            {
                groups.Remove(groupName);
                if (groups.Count == 0)
                    _collapsedGroupsByPack.Remove(packName);
            }
        }

        private static bool _rebuildInProgress;

        private static void RebuildGeneratedBrowser()
        {
            // Only rebuild while the expanded browser is actually the built content; this always
            // rebuilds the full expanded view, so running it while the collapsed hint panel is
            // showing would silently replace it with the browser out from under the user.
            if (_menuRoot == null || !_shaderBrowserExpanded || _rebuildInProgress)
                return;

            if (_menuParent == null && _menuRoot.transform != null)
                _menuParent = _menuRoot.transform.parent;

            if (_menuParent == null)
                return;

            _rebuildInProgress = true;
            try
            {
                CaptureScrollState(_menuRoot);
                DestroyMenuRoot();

                LoadPackModels();
                RenderGeneratedBrowser(_menuParent, true);
                _lastUiRebuildAt = Time.unscaledTime;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GeneratedUiController] UI rebuild failed: {ex}");
            }
            finally
            {
                _rebuildInProgress = false;
            }
        }

        private static void QueueUiRebuild(bool persistState, bool immediate = false)
        {
            if (persistState)
                CommitPendingInputEdits();

            if (persistState)
                _pendingUiRebuildPersistState = true;

            _pendingUiRebuild = true;

            if (immediate)
            {
                _nextUiRebuildAt = 0f;
            }
            else
            {
                var now = Time.unscaledTime;
                var nextAllowed = _lastUiRebuildAt + UiRebuildDebounceSeconds;
                var scheduled = Math.Max(now, nextAllowed);
                if (_nextUiRebuildAt <= 0f || _nextUiRebuildAt > scheduled)
                    _nextUiRebuildAt = scheduled;
            }

            TryProcessPendingUiRebuild();
        }

        private static void TryApplyPendingExpandResize()
        {
            if (_pendingExpandResizeFrames > 0)
            {
                _pendingExpandResizeFrames--;
                return;
            }
            _pendingExpandResize = false;
            var parentPath = _modWindowRect != null ? GetTransformPath(_modWindowRect.transform) : "null";
            var holderRt = _modWindowRect?.parent as RectTransform;
            var holderSize = holderRt != null ? holderRt.sizeDelta.ToString() : (_modWindowRect?.parent != null ? "not RectTransform" : "no parent");
            Debug.Log($"[GeneratedUiController] TryApplyPendingExpandResize: captured={_settingsStateCaptured} modRect={parentPath} currentSize={_modWindowRect?.sizeDelta} holderSize={holderSize}");
            if (_settingsStateCaptured)
                EnterExpandedBrowserUiMode();
            Debug.Log($"[GeneratedUiController] TryApplyPendingExpandResize after apply: size={_modWindowRect?.sizeDelta} active={_modWindowRect?.gameObject.activeInHierarchy}");
        }

        private static string GetTransformPath(Transform t)
        {
            if (t == null) return "null";
            var parts = new System.Collections.Generic.List<string>();
            while (t != null) { parts.Insert(0, t.name); t = t.parent; }
            return string.Join("/", parts);
        }

        private static void TryProcessPendingUiRebuild()
        {
            if (!_pendingUiRebuild)
                return;

            if (_menuRoot == null || !_shaderBrowserExpanded)
                return;

            if (_menuParent == null && _menuRoot.transform != null)
                _menuParent = _menuRoot.transform.parent;

            if (_menuParent == null)
                return;

            if (_nextUiRebuildAt > 0f && Time.unscaledTime < _nextUiRebuildAt)
                return;

            var persistState = _pendingUiRebuildPersistState;
            _pendingUiRebuild = false;
            _pendingUiRebuildPersistState = false;
            _nextUiRebuildAt = 0f;

            SavePersistentUiState(persistState);
            RebuildGeneratedBrowser();
        }

        private static GameObject RenderGeneratedBrowser(Transform parent, bool restoreScroll)
        {
            GeneratedLayout.ConfigureShaderBrowser(BuildGeneratedBrowserState(), false);
            _menuRoot = GeneratedLayout.RenderToParent(parent);
            _menuOpen = _menuRoot != null;
            // CloseShaderBrowser() (run at the start of BuildExpandedBrowser) resets this via
            // CleanupMenuState before we get here, so it must be restored once the expanded
            // browser is actually the thing being rendered — otherwise MenuLifecycle's
            // isExpanded-gated behaviors (save indicator, pending rebuilds, focus restore) never
            // run despite the expanded browser being live.
            if (_menuRoot != null)
                _shaderBrowserExpanded = true;
            _pendingScrollRestore = restoreScroll && _menuRoot != null && _scrollStateByPath.Count > 0;
            _scrollRestorePassesRemaining = _pendingScrollRestore ? 8 : 0;

            if (_menuRoot != null)
            {
                var lc = _menuRoot.GetComponent<MenuLifecycle>() ?? _menuRoot.AddComponent<MenuLifecycle>();
                // SFS destroys _menuRoot immediately after BuildMenuPage returns; suppress
                // CleanupMenuState() on that destroy so our expand state survives.
                lc.SuppressCleanup = true;

                var marker = _menuRoot.GetComponent<BrowserStateMarker>() ?? _menuRoot.AddComponent<BrowserStateMarker>();
                marker.Expanded = true;
            }

            return _menuRoot;
        }

        private static void PersistAndRebuild(bool persistState = true)
        {
            QueueUiRebuild(persistState);
        }

        private static void CleanupPackUiState(string packName, HashSet<string> validGroups, HashSet<string> validPackGroupKeys)
        {
            if (string.IsNullOrWhiteSpace(packName))
                return;

            if (_collapsedGroupsByPack.TryGetValue(packName, out var collapsed))
            {
                collapsed.RemoveWhere(group => validGroups == null || !validGroups.Contains(group));
                if (collapsed.Count == 0)
                    _collapsedGroupsByPack.Remove(packName);
            }

            var prefix = packName + "|";
            foreach (var staleKey in _selectedTabByPackAndGroup.Keys
                .Where(key => key.StartsWith(prefix, StringComparison.Ordinal) && (validPackGroupKeys == null || !validPackGroupKeys.Contains(key)))
                .ToList())
            {
                _selectedTabByPackAndGroup.Remove(staleKey);
            }
        }

        private static void CleanupShaderUiState(HashSet<string> availablePackNames)
        {
            var activeShaderNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var pack in _packs)
            {
                if (pack?.Pack == null || string.IsNullOrWhiteSpace(pack.Name) || availablePackNames == null || !availablePackNames.Contains(pack.Name))
                    continue;

                var shaders = pack.Pack.GetShaders();
                if (shaders == null)
                    continue;

                foreach (var shader in shaders)
                {
                    if (shader == null || string.IsNullOrWhiteSpace(shader.Name))
                        continue;

                    activeShaderNames.Add(shader.Name);
                }
            }

            foreach (var staleShader in _shaderUiState.Keys.Where(shaderName => !activeShaderNames.Contains(shaderName)).ToList())
                _shaderUiState.Remove(staleShader);
        }

        private static List<EditableField> BuildEditableFields(PackModel pack)
        {
            var list = new List<EditableField>();
            var shaders = pack.Pack?.GetShaders();
            if (shaders == null)
                return list;

            foreach (var shader in shaders)
            {
                if (shader == null || string.IsNullOrWhiteSpace(shader.Name))
                    continue;

                var args = BuildEffectiveUiArgs(pack, shader);
                if (args == null)
                    continue;

                var argsType = args.GetType();
                var flattened = FlattenObject(args, string.Empty);

                // Guard against reflection/boxing edge cases by ensuring root leaf fields are present.
                var rootFields = argsType
                    .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(field => !field.IsStatic && IsLeafType(field.FieldType));

                foreach (var rootField in rootFields)
                {
                    if (!flattened.ContainsKey(rootField.Name))
                        flattened[rootField.Name] = rootField.GetValue(args);
                }

                foreach (var entry in flattened)
                {
                    var metadata = ResolveFieldMetadata(argsType, entry.Key);
                    if (!metadata.ExposeInUi)
                        continue;

                    var valueType = entry.Value?.GetType();
                    var resolvedValueType = valueType == null ? null : (Nullable.GetUnderlyingType(valueType) ?? valueType);
                    var isBoolean = resolvedValueType == typeof(bool);

                    list.Add(new EditableField
                    {
                        ShaderName = shader.Name,
                        GroupName = metadata.Group,
                        TabName = metadata.Tab,
                        Path = entry.Key,
                        Label = BuildFieldLabel(entry.Key),
                        Value = FormatValue(entry.Value),
                        ValueType = resolvedValueType,
                        IsBoolean = isBoolean,
                    });
                }
            }

            return list.OrderBy(item => item.ShaderName, StringComparer.Ordinal)
                .ThenBy(item => item.Path, StringComparer.Ordinal)
                .ToList();
        }

        private static object? BuildEffectiveUiArgs(PackModel pack, IShaderModule shader)
        {
            if (pack == null || pack.Pack == null || shader == null || string.IsNullOrWhiteSpace(shader.Name))
                return null;

            var defaultArgs = pack.Pack.GetArgs(shader.Name);
            var currentArgs = ShaderPackManager.GetCurrentArgs(shader.Name);
            var baseline = BuildDefaultArgsBaseline(shader, defaultArgs, currentArgs);
            if (baseline == null)
                return null;

            if (currentArgs == null)
                ShaderPackManager.SetCurrentArgs(shader.Name, CloneArgs(baseline));

            var overrides = ShaderPackManager.GetUserOverrides(shader.Name);
            if (overrides == null)
                return baseline;

            foreach (var pair in overrides)
                TrySetPathValue(baseline, pair.Key, pair.Value);

            return baseline;
        }

        private static object BuildDefaultArgsBaseline(IShaderModule shader, object defaultArgs, object currentArgs)
        {
            var baseline = CloneArgs(defaultArgs) ?? CloneArgs(currentArgs);
            if (baseline == null)
            {
                var argsType = ResolveShaderArgsType(shader);
                if (argsType != null)
                {
                    try { baseline = Activator.CreateInstance(argsType); }
                    catch { baseline = null; }
                }
            }

            return ApplyShaderArgDefaults(baseline);
        }

        // Delegates to the shared implementation in shaders.ShaderArgDefaults (lib/Shaders.cs) so
        // every place that builds a fresh Args via Activator.CreateInstance — this UI, the pack
        // activation paths in ShaderPackManager, and OverlayDispatcher's render fallback — fills in
        // [ShaderArg] defaults (and CreateDefaultArgs()-seeded per-tab baselines) the same way.
        private static object? ApplyShaderArgDefaults(object target) => shaders.ShaderArgDefaults.Apply(target);

        private static object? CloneArgs(object source)
        {
            if (source == null)
                return null;

            var sourceType = source.GetType();
            try
            {
                var serialized = JsonConvert.SerializeObject(source);
                var clone = JsonConvert.DeserializeObject(serialized, sourceType);
                return clone ?? source;
            }
            catch
            {
                return source;
            }
        }

        private static (string Group, string Tab, bool ExposeInUi) ResolveFieldMetadata(System.Type argsType, string path)
        {
            if (argsType == null || string.IsNullOrWhiteSpace(path))
                return ("General", string.Empty, true);

            var currentType = argsType;
            var groupName = "General";
            var tabName = string.Empty;
            var exposeInUi = true;

            var segments = path.Split('.');
            for (var i = 0; i < segments.Length; i++)
            {
                var (memberName, _) = ParsePathSegment(segments[i]);
                if (string.IsNullOrWhiteSpace(memberName) || currentType == null)
                    break;

                var field = currentType.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null)
                    break;

                var attr = field.GetCustomAttribute<ShaderArgAttribute>();
                if (attr != null)
                {
                    if (!attr.ExposeInUi)
                        exposeInUi = false;

                    var (attrGroup, attrTab) = ResolveGroupAndTab(attr.Group, attr.Tab);
                    if (!string.IsNullOrWhiteSpace(attrGroup))
                        groupName = attrGroup;

                    if (!string.IsNullOrWhiteSpace(attrTab))
                        tabName = attrTab;
                }

                var nextType = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
                if (nextType.IsArray)
                    nextType = nextType.GetElementType();
                else if (nextType.IsGenericType && nextType.GetGenericArguments().Length == 1)
                    nextType = nextType.GetGenericArguments()[0];

                currentType = nextType;
            }

            return (groupName, tabName, exposeInUi);
        }

        private static (string Group, string Tab) ResolveGroupAndTab(string group, string tab)
        {
            if (!string.IsNullOrWhiteSpace(tab))
                return (string.IsNullOrWhiteSpace(group) ? "General" : group.Trim(), tab.Trim());

            var safeGroup = string.IsNullOrWhiteSpace(group) ? "General" : group.Trim();
            var separators = new[] { '/', '|', ':' };
            for (var i = 0; i < separators.Length; i++)
            {
                var separatorIndex = safeGroup.IndexOf(separators[i]);
                if (separatorIndex <= 0 || separatorIndex >= safeGroup.Length - 1)
                    continue;

                var parsedGroup = safeGroup.Substring(0, separatorIndex).Trim();
                var parsedTab = safeGroup.Substring(separatorIndex + 1).Trim();
                if (string.IsNullOrWhiteSpace(parsedGroup) || string.IsNullOrWhiteSpace(parsedTab))
                    continue;

                return (parsedGroup, parsedTab);
            }

            return (safeGroup, string.Empty);
        }

        private static bool ApplyFieldValue(string shaderName, string path, string valueText, bool applySelectedPack = true, bool rebuildUi = true)
        {
            if (string.IsNullOrWhiteSpace(shaderName) || string.IsNullOrWhiteSpace(path))
                return false;

            var current = ShaderPackManager.GetCurrentArgs(shaderName);
            if (current == null)
                return false;

            var fallback = ReadPathValue(current, path);
            var parsed = ParseValue(valueText, fallback?.GetType(), fallback);

            if (ValuesEqual(fallback, parsed))
            {
                RefreshBoundInputValue(shaderName, path, fallback, current);
                return false;
            }

            if (!TrySetPathValue(current, path, parsed))
                return false;

            PersistShaderOverride(shaderName, path, parsed, current);
            RefreshBoundInputValue(shaderName, path, parsed, current);

            if (applySelectedPack)
                QueuePackApply();

            if (rebuildUi)
                RequestUiRebuild();

            return true;
        }

        private static bool ReadBoolFieldValue(string shaderName, string path) => ReadPathValue(ShaderPackManager.GetCurrentArgs(shaderName), path) is bool value && value;

        private static void ToggleBoolFieldValue(string shaderName, string path)
        {
            var current = ShaderPackManager.GetCurrentArgs(shaderName);
            if (current == null)
                return;

            var currentValue = ReadBoolFieldValue(shaderName, path);
            var toggled = !currentValue;
            if (!TrySetPathValue(current, path, toggled))
                return;

            PersistShaderOverride(shaderName, path, toggled, current);
            UpdateShaderSnapshot(GetOrCreateShaderUiState(shaderName), current);

            QueuePackApply(immediate: true);

            RequestUiRebuild();
        }

        private static void QueuePackApply(bool immediate = false)
        {
            _pendingPackApply = true;

            if (immediate)
            {
                _nextPackApplyAt = 0f;
            }
            else
            {
                var now = Time.unscaledTime;
                var scheduled = now + PackApplyDebounceSeconds;
                if (_nextPackApplyAt <= 0f || _nextPackApplyAt > scheduled)
                    _nextPackApplyAt = scheduled;
            }

            if (!_menuOpen)
                TryProcessPendingPackApply();
        }

        private static void TryProcessPendingPackApply()
        {
            if (!_pendingPackApply)
                return;

            if (_nextPackApplyAt > 0f && Time.unscaledTime < _nextPackApplyAt)
                return;

            _pendingPackApply = false;
            _nextPackApplyAt = 0f;
            ApplyCurrentOrSelectedPack();
        }

        private static void PersistShaderOverride(string shaderName, string path, object value, object currentArgs)
        {
            ShaderPackManager.SetCurrentArgs(shaderName, currentArgs);
            ShowSaveIndicator("Saving", 0.5f);
            ShaderPackManager.SetUserOverride(shaderName, path, value);
            ShaderPackManager.FlushPendingSave();
            ShowSaveIndicator("Saved", 1.2f);
        }

        private static void RefreshBoundInputValue(string shaderName, string path, object value, object argsSnapshot)
        {
            if (string.IsNullOrWhiteSpace(shaderName) || string.IsNullOrWhiteSpace(path))
                return;

            var state = GetOrCreateShaderUiState(shaderName);
            PruneDeadInputs(state);

            var key = $"{shaderName}:{path}";
            if (state.Inputs.TryGetValue(key, out var input) && input != null)
            {
                var formatted = FormatValue(value);
                if (!string.Equals(input.Text, formatted, StringComparison.Ordinal))
                    input.Text = formatted;
            }

            UpdateShaderSnapshot(state, argsSnapshot);
        }

        private static void UpdateShaderSnapshot(UiShaderState state, object argsSnapshot)
        {
            if (state == null)
                return;

            if (argsSnapshot == null)
            {
                state.LastSnapshot = string.Empty;
                return;
            }

            var flat = FlattenObject(argsSnapshot, string.Empty);
            state.LastSnapshot = string.Join("|", flat.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        }

        private static bool ValuesEqual(object left, object right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left == null || right == null)
                return false;

            if (left is float leftFloat && right is float rightFloat)
                return (float.IsNaN(leftFloat) && float.IsNaN(rightFloat)) || leftFloat.Equals(rightFloat);

            if (left is double leftDouble && right is double rightDouble)
                return (double.IsNaN(leftDouble) && double.IsNaN(rightDouble)) || Math.Abs(leftDouble - rightDouble) <= 1e-9d;

            return left.Equals(right);
        }

        private static void ApplySelectedPack()
        {
            if (_selectedPackIndex < 0 || _selectedPackIndex >= _packs.Count)
                return;

            var pack = _packs[_selectedPackIndex];
            if (pack.Pack == null || string.IsNullOrWhiteSpace(pack.Name))
                return;

            var args = new Dictionary<string, object>(StringComparer.Ordinal);
            var shaders = pack.Pack.GetShaders();
            if (shaders != null)
            {
                foreach (var shader in shaders)
                {
                    if (shader == null || string.IsNullOrWhiteSpace(shader.Name))
                        continue;

                    var current = ShaderPackManager.GetCurrentArgs(shader.Name) ?? pack.Pack.GetArgs(shader.Name);
                    if (current != null)
                        args[shader.Name] = current;
                }
            }

            if (!ShaderPackManager.ActivatePack(pack.Name, args))
                Debug.LogWarning($"[GeneratedUiController] Failed to activate pack '{pack.Name}'.");

            ShaderPackManager.SetSelectedPackName(pack.Name);
            PersistCurrentPackNames();
            SavePersistentUiState();
        }

        private static void ApplyCurrentOrSelectedPack()
        {   // Re-apply whichever pack context is active so shader updates take effect immediately.
            if (_selectedPackIndex >= 0 && _selectedPackIndex < _packs.Count)
            {
                ApplySelectedPack();
                return;
            }

            var activePack = ShaderPackManager.GetActivePack();
            if (activePack == null || string.IsNullOrWhiteSpace(activePack.Name))
                return;

            var activeIndex = _packs.FindIndex(p => p != null && p.Pack != null && string.Equals(p.Name, activePack.Name, StringComparison.Ordinal));
            if (activeIndex < 0)
                return;

            var previousSelectedIndex = _selectedPackIndex;
            _selectedPackIndex = activeIndex;
            try
            {
                ApplySelectedPack();
            }
            finally
            {
                _selectedPackIndex = previousSelectedIndex;
            }
        }

        private static List<int> FilterPacks()
        {
            var needle = (_searchText ?? string.Empty).Trim();

            return Enumerable.Range(0, _packs.Count)
                .Where(i =>
                {
                    var pack = _packs[i];
                    return _packFilterMode switch
                    {
                        PackFilterMode.Ready when !pack.CanActivate => false,
                        PackFilterMode.Failed when pack.CanActivate => false,
                        _ => needle.Length == 0
                            || pack.Name.Contains(needle, StringComparison.OrdinalIgnoreCase)
                            || pack.Description.Contains(needle, StringComparison.OrdinalIgnoreCase)
                            || pack.Author.Contains(needle, StringComparison.OrdinalIgnoreCase),
                    };
                })
                .ToList();
        }

        private static UiShaderState GetOrCreateShaderUiState(string shaderName)
        {
            if (_shaderUiState.TryGetValue(shaderName, out var state) && state != null)
                return state;
            return _shaderUiState[shaderName] = new UiShaderState();
        }

        private static void PruneDeadInputs(UiShaderState state)
        {
            if (state == null || state.Inputs.Count == 0)
                return;

            var deadKeys = new List<string>();
            foreach (var pair in state.Inputs)
            {
                var input = pair.Value;
                if (input == null || input.gameObject == null)
                    deadKeys.Add(pair.Key);
            }

            for (var i = 0; i < deadKeys.Count; i++)
                state.Inputs.Remove(deadKeys[i]);
        }

        private static void PersistCurrentPackNames()
        {
            _persistedSelectedPackName = _selectedPackIndex >= 0 && _selectedPackIndex < _packs.Count ? _packs[_selectedPackIndex].Name : string.Empty;
            _persistedViewedPackName = _viewedPackIndex >= 0 && _viewedPackIndex < _packs.Count ? _packs[_viewedPackIndex].Name : _persistedSelectedPackName;
        }

        private static void LoadPersistentUiState()
        {
            if (_persistentUiStateLoaded)
                return;

            _persistentUiStateLoaded = true;
            if (!PlayerPrefs.HasKey(PersistentUiStateKey))
                return;

            Try<object>.Run(() =>
            {
                var raw = PlayerPrefs.GetString(PersistentUiStateKey, string.Empty);
                if (string.IsNullOrWhiteSpace(raw))
                    return null;

                var state = JsonConvert.DeserializeObject<PersistentUiState>(raw);
                if (state == null)
                    return null;

                _searchText = state.SearchText ?? string.Empty;
                if (!Enum.TryParse(state.PackFilterMode, true, out _packFilterMode))
                    _packFilterMode = state.ShowReadyOnly ? PackFilterMode.Ready : PackFilterMode.All;

                // Deliberately not restoring ConfigPopoutMode across sessions: it silently skips
                // hiding/resizing the native settings panel on every future expand once left true
                // (that branch exists specifically for the detached popout window, which needs to
                // coexist with the settings screen). Popout should be an explicit action each
                // session, not something that can get permanently stuck on.
                _configPopoutMode = false;

                _persistedSelectedPackName = state.SelectedPackName ?? string.Empty;
                _persistedViewedPackName = state.ViewedPackName ?? string.Empty;

                _collapsedGroupsByPack.Clear();
                if (state.CollapsedGroups != null)
                {
                    for (var i = 0; i < state.CollapsedGroups.Count; i++)
                    {
                        var item = state.CollapsedGroups[i];
                        if (item == null || string.IsNullOrWhiteSpace(item.PackName) || string.IsNullOrWhiteSpace(item.GroupKey))
                            continue;

                        if (!_collapsedGroupsByPack.TryGetValue(item.PackName, out var groups))
                        {
                            groups = new HashSet<string>(StringComparer.Ordinal);
                            _collapsedGroupsByPack[item.PackName] = groups;
                        }

                        groups.Add(item.GroupKey);
                    }
                }

                _selectedTabByPackAndGroup.Clear();
                if (state.SelectedTabs != null)
                {
                    for (var i = 0; i < state.SelectedTabs.Count; i++)
                    {
                        var item = state.SelectedTabs[i];
                        if (item == null || string.IsNullOrWhiteSpace(item.PackName) || string.IsNullOrWhiteSpace(item.GroupKey) || string.IsNullOrWhiteSpace(item.TabName))
                            continue;

                        _selectedTabByPackAndGroup[$"{item.PackName}|{item.GroupKey}"] = item.TabName;
                    }
                }

                return null;
            }).Match(
                _ => { },
                ex => Debug.LogWarning($"[GeneratedUiController] Failed to load persistent UI state: {ex.Message}"));
        }

        private static void SavePersistentUiState(bool showIndicator = true)
        {
            Try<object>.Run(() =>
            {
                PersistCurrentPackNames();

                var state = new PersistentUiState
                {
                    SearchText = _searchText ?? string.Empty,
                    ShowReadyOnly = _packFilterMode == PackFilterMode.Ready,
                    PackFilterMode = _packFilterMode.ToString(),
                    ShaderBrowserExpanded = _shaderBrowserExpanded,
                    ConfigPopoutMode = _configPopoutMode,
                    SelectedPackName = _persistedSelectedPackName ?? string.Empty,
                    ViewedPackName = _persistedViewedPackName ?? string.Empty,
                };

                foreach (var pair in _collapsedGroupsByPack)
                {
                    foreach (var groupKey in pair.Value)
                    {
                        state.CollapsedGroups.Add(new PersistentCollapsedGroup
                        {
                            PackName = pair.Key,
                            GroupKey = groupKey,
                        });
                    }
                }

                foreach (var pair in _selectedTabByPackAndGroup)
                {
                    var separator = pair.Key.IndexOf('|');
                    if (separator <= 0 || separator >= pair.Key.Length - 1)
                        continue;

                    var packName = pair.Key.Substring(0, separator);
                    var groupKey = pair.Key.Substring(separator + 1);
                    if (string.IsNullOrWhiteSpace(packName) || string.IsNullOrWhiteSpace(groupKey) || string.IsNullOrWhiteSpace(pair.Value))
                        continue;

                    state.SelectedTabs.Add(new PersistentTabSelection
                    {
                        PackName = packName,
                        GroupKey = groupKey,
                        TabName = pair.Value,
                    });
                }

                PlayerPrefs.SetString(PersistentUiStateKey, JsonConvert.SerializeObject(state));
                PlayerPrefs.Save();
                if (showIndicator)
                    ShowSaveIndicator("Saved", 1.2f);

                return null;
            }).Match(
                _ => { },
                ex =>
                {
                    Debug.LogWarning($"[GeneratedUiController] Failed to save persistent UI state: {ex.Message}");
                    if (showIndicator)
                        ShowSaveIndicator("Save failed", 2f);
                });
        }

        private static void ShowSaveIndicator(string message, float seconds)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            _saveIndicatorText = message;
            _saveIndicatorUntil = Time.unscaledTime + Math.Max(0.3f, seconds);
            ApplyWindowTitle();
        }

        private static void TickSaveIndicator()
        {
            if (string.IsNullOrWhiteSpace(_saveIndicatorText))
                return;

            if (Time.unscaledTime < _saveIndicatorUntil)
                return;

            _saveIndicatorText = string.Empty;
            _saveIndicatorUntil = 0f;
            ApplyWindowTitle();
        }

        // The save-indicator suffix changes far more often than the browser's actual content, so
        // it is pushed onto the live window directly instead of going through QueueUiRebuild — a
        // full tree rebuild (destroy + recreate every card/field, re-run scroll/focus restore) just
        // to change a few characters of title text was the main source of visible UI jitter.
        private static void ApplyWindowTitle()
        {
            if (_menuRoot == null || !_shaderBrowserExpanded)
                return;

            var suffix = string.IsNullOrWhiteSpace(_saveIndicatorText) ? string.Empty : $" - {_saveIndicatorText}";
            GeneratedLayout.SetWindowTitle("Main Window" + suffix);
        }

        private static object ParseValue(string text, System.Type targetType, object fallback)
        {
            if (targetType == null)
                return text;

            var nonNullType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            var input = text ?? string.Empty;

            return nonNullType switch
            {
                _ when nonNullType == typeof(string) => input,
                _ when nonNullType == typeof(bool) => bool.TryParse(input, out var b) ? b : fallback,
                _ when nonNullType == typeof(int) => int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : fallback,
                _ when nonNullType == typeof(float) => float.TryParse(input, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var f) ? f : fallback,
                _ when nonNullType == typeof(double) => double.TryParse(input, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var d) ? d : fallback,
                { IsEnum: true } => TryParseEnum(nonNullType, input, fallback),
                _ => TryConvertType(input, nonNullType, fallback),
            };
        }

        private static object TryParseEnum(System.Type enumType, string input, object fallback)
        {
            try { return Enum.Parse(enumType, input, true); }
            catch { return fallback; }
        }

        private static object TryConvertType(string input, System.Type targetType, object fallback)
        {
            try { return Convert.ChangeType(input, targetType, CultureInfo.InvariantCulture); }
            catch { return fallback; }
        }

        private static string FormatValue(object value)
        {
            if (value == null)
                return string.Empty;

            return value switch
            {
                float f => f.ToString("R", CultureInfo.InvariantCulture),
                double d => d.ToString("R", CultureInfo.InvariantCulture),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
            };
        }

        private static string BuildFieldLabel(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "Config";

            var clean = path;
            var dotIndex = clean.LastIndexOf('.');
            if (dotIndex >= 0 && dotIndex < clean.Length - 1)
                clean = clean.Substring(dotIndex + 1);

            var bracketIndex = clean.IndexOf('[');
            if (bracketIndex > 0)
                clean = clean.Substring(0, bracketIndex);

            return clean.Replace("_", " ");
        }

        private const BindingFlags InstanceFieldFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly Dictionary<System.Type, FieldInfo[]> _typeFieldsCache = new();
        private static readonly Dictionary<(System.Type Type, string Name), FieldInfo> _namedFieldCache = new();

        // Reflecting a struct's field list/attributes is the same every call for a given Type;
        // only the boxed values change per-call, so the metadata lookup is cached once per Type.
        private static FieldInfo[] GetInstanceFields(System.Type type)
        {
            if (_typeFieldsCache.TryGetValue(type, out var cached))
                return cached;

            var fields = type.GetFields(InstanceFieldFlags);
            _typeFieldsCache[type] = fields;
            return fields;
        }

        private static FieldInfo GetNamedField(System.Type type, string name)
        {
            var key = (type, name);
            if (_namedFieldCache.TryGetValue(key, out var cached))
                return cached;

            var field = type.GetField(name, InstanceFieldFlags);
            _namedFieldCache[key] = field;
            return field;
        }

        private static Dictionary<string, object> FlattenObject(object source, string prefix)
        {
            var result = new Dictionary<string, object>(StringComparer.Ordinal);
            if (source == null)
                return result;

            var type = source.GetType();
            if (IsLeafType(type))
            {
                result[prefix] = source;
                return result;
            }

            var fields = GetInstanceFields(type);

            foreach (var field in fields)
            {
                var value = field.GetValue(source);
                var key = string.IsNullOrWhiteSpace(prefix) ? field.Name : $"{prefix}.{field.Name}";

                if (value == null || IsLeafType(field.FieldType))
                {
                    result[key] = value;
                    continue;
                }

                if (value is System.Collections.IList list)
                {
                    for (var i = 0; i < list.Count; i++)
                    {
                        var item = list[i];
                        var itemKey = $"{key}[{i}]";
                        if (item == null || IsLeafType(item.GetType()))
                        {
                            result[itemKey] = item;
                            continue;
                        }

                        foreach (var nested in FlattenObject(item, itemKey))
                            result[nested.Key] = nested.Value;
                    }

                    continue;
                }

                foreach (var nested in FlattenObject(value, key))
                    result[nested.Key] = nested.Value;
            }

            return result;
        }

        private static object? ReadPathValue(object root, string path)
        {
            if (root == null || string.IsNullOrWhiteSpace(path))
                return null;

            object current = root;
            var parts = path.Split('.');
            for (var i = 0; i < parts.Length; i++)
            {
                var (memberName, listIndex) = ParsePathSegment(parts[i]);
                if (current == null)
                    return null;

                var field = GetNamedField(current.GetType(), memberName);
                if (field == null)
                    return null;

                current = field.GetValue(current);
                if (listIndex >= 0)
                {
                    if (!(current is System.Collections.IList list) || listIndex >= list.Count)
                        return null;

                    current = list[listIndex];
                }
            }

            return current;
        }

        private static bool TrySetPathValue(object root, string path, object value)
        {
            if (root == null || string.IsNullOrWhiteSpace(path))
                return false;

            var parts = path.Split('.');
            if (parts.Length == 0)
                return false;

            return TrySetPathValueRecursive(root, parts, 0, value, out _);
        }

        private static bool TrySetPathValueRecursive(object current, string[] parts, int depth, object value, out object updated)
        {
            updated = current;
            if (current == null)
                return false;

            var (memberName, listIndex) = ParsePathSegment(parts[depth]);
            var field = GetNamedField(current.GetType(), memberName);
            if (field == null)
                return false;

            var isLeaf = depth == parts.Length - 1;
            if (isLeaf)
            {
                if (listIndex >= 0)
                {
                    if (!(field.GetValue(current) is System.Collections.IList list) || listIndex < 0 || listIndex >= list.Count)
                        return false;

                    var elementType = list.GetType().IsArray ? list.GetType().GetElementType() : null;
                    list[listIndex] = ConvertValueForField(elementType ?? value?.GetType(), value);
                    updated = current;
                    return true;
                }

                field.SetValue(current, ConvertValueForField(field.FieldType, value));
                updated = current;
                return true;
            }

            if (listIndex >= 0)
            {
                if (!(field.GetValue(current) is System.Collections.IList list) || listIndex < 0 || listIndex >= list.Count)
                    return false;

                var child = list[listIndex];
                if (!TrySetPathValueRecursive(child, parts, depth + 1, value, out var updatedChild))
                    return false;

                list[listIndex] = updatedChild;
                updated = current;
                return true;
            }

            var next = field.GetValue(current);
            if (next == null)
                return false;

            if (!TrySetPathValueRecursive(next, parts, depth + 1, value, out var updatedChildValue))
                return false;

            field.SetValue(current, updatedChildValue);
            updated = current;
            return true;
        }

        private static object ConvertValueForField(System.Type fieldType, object value)
        {   // UI edit fields box numeric input as double regardless of the target field's
            // actual numeric type (float, int, etc.), so FieldInfo.SetValue throws unless we
            // convert to the exact type first.
            if (fieldType == null || value == null || fieldType.IsInstanceOfType(value))
                return value;

            if (fieldType.IsEnum && value is string enumText)
                return System.Enum.Parse(fieldType, enumText, true);

            if (typeof(IConvertible).IsAssignableFrom(fieldType) && value is IConvertible)
            {
                try { return System.Convert.ChangeType(value, fieldType); }
                catch { return value; }
            }

            return value;
        }

        private static (string Name, int Index) ParsePathSegment(string segment)
        {
            if (string.IsNullOrWhiteSpace(segment))
                return (string.Empty, -1);

            var open = segment.IndexOf('[');
            var close = segment.IndexOf(']');
            if (open > 0 && close > open)
            {
                var name = segment.Substring(0, open);
                var idxText = segment.Substring(open + 1, close - open - 1);
                return int.TryParse(idxText, out var idx) ? (name, idx) : (name, -1);
            }

            return (segment, -1);
        }

        private static bool IsLeafType(System.Type type)
        {
            var target = Nullable.GetUnderlyingType(type) ?? type;
            return target.IsPrimitive
                || target.IsEnum
                || target == typeof(string)
                || target == typeof(decimal)
                || target == typeof(Vector2)
                || target == typeof(Vector3)
                || target == typeof(Color)
                || target == typeof(Color32);
        }

        private static void CaptureScrollState(GameObject root)
        {
            _scrollStateByPath.Clear();
            if (root == null)
                return;

            var rootTransform = root.transform;
            foreach (var scrollRect in root.GetComponentsInChildren<ScrollRect>(true))
            {
                if (scrollRect == null || scrollRect.transform == null)
                    continue;

                var position = new Vector2(scrollRect.horizontalNormalizedPosition, scrollRect.verticalNormalizedPosition);
                var strictKey = BuildRelativeTransformKey(rootTransform, scrollRect.transform, includeSiblingOrdinal: true);
                if (!string.IsNullOrWhiteSpace(strictKey))
                    _scrollStateByPath[strictKey] = position;

                var fallbackKey = BuildRelativeTransformKey(rootTransform, scrollRect.transform, includeSiblingOrdinal: false);
                if (!string.IsNullOrWhiteSpace(fallbackKey))
                    _scrollStateByPath[fallbackKey] = position;
            }
        }

        private static void TryRestoreScrollState()
        {
            if (_menuRoot == null || _scrollStateByPath.Count == 0)
            {
                _pendingScrollRestore = false;
                _scrollRestorePassesRemaining = 0;
                return;
            }

            Canvas.ForceUpdateCanvases();

            var rootTransform = _menuRoot.transform;
            foreach (var scrollRect in _menuRoot.GetComponentsInChildren<ScrollRect>(true))
            {
                if (scrollRect == null || scrollRect.transform == null)
                    continue;

                var strictKey = BuildRelativeTransformKey(rootTransform, scrollRect.transform, includeSiblingOrdinal: true);
                if (!string.IsNullOrWhiteSpace(strictKey) && _scrollStateByPath.TryGetValue(strictKey, out var strictPosition))
                {
                    scrollRect.horizontalNormalizedPosition = strictPosition.x;
                    scrollRect.verticalNormalizedPosition = strictPosition.y;
                    continue;
                }

                var fallbackKey = BuildRelativeTransformKey(rootTransform, scrollRect.transform, includeSiblingOrdinal: false);
                if (string.IsNullOrWhiteSpace(fallbackKey) || !_scrollStateByPath.TryGetValue(fallbackKey, out var position))
                    continue;

                scrollRect.horizontalNormalizedPosition = position.x;
                scrollRect.verticalNormalizedPosition = position.y;
            }

            Canvas.ForceUpdateCanvases();
            _scrollRestorePassesRemaining = Math.Max(0, _scrollRestorePassesRemaining - 1);
            _pendingScrollRestore = _scrollRestorePassesRemaining > 0;
        }

        private static void TryRestoreSearchInputFocus()
        {
            if (!_pendingSearchFocusRestore || _menuRoot == null)
                return;

            InputField inputField = null;

            var searchTransform = _menuRoot
                .GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t != null && string.Equals(t.name, "Search_Input", StringComparison.Ordinal));

            if (searchTransform != null)
                inputField = searchTransform.GetComponent<InputField>() ?? searchTransform.GetComponentInChildren<InputField>(true);

            if (inputField == null)
                inputField = _menuRoot.GetComponentsInChildren<InputField>(true).FirstOrDefault();

            if (inputField == null)
                return;

            inputField.Select();
            inputField.ActivateInputField();
            var caret = string.IsNullOrEmpty(inputField.text) ? 0 : inputField.text.Length;
            inputField.caretPosition = caret;
            inputField.selectionAnchorPosition = caret;
            inputField.selectionFocusPosition = caret;
            _pendingSearchFocusRestore = false;
        }

        private static string BuildRelativeTransformKey(Transform root, Transform target, bool includeSiblingOrdinal)
        {
            if (root == null || target == null)
                return string.Empty;
            var segments = new List<string>();
            for (var current = target; current != null && current != root; current = current.parent)
            {
                if (!includeSiblingOrdinal)
                    segments.Add(current.name);
                else
                {
                    var parent = current.parent;
                    var ordinal = 0;
                    if (parent != null)
                        for (var i = 0; i < parent.childCount; i++)
                        {
                            var s = parent.GetChild(i);
                            if (s == null || !string.Equals(s.name, current.name, StringComparison.Ordinal))
                                continue;
                            if (ReferenceEquals(s, current))
                                break;
                            ordinal += 1;
                        }
                    segments.Add($"{current.name}[{ordinal}]");
                }
            }
            if (target == null)
                return string.Empty;
            var cursor = target;
            while (cursor != null && cursor != root)
                cursor = cursor.parent;
            if (cursor != root)
                return string.Empty;
            segments.Reverse();
            return string.Join("/", segments);
        }

        private static void CloseShaderBrowser()
        {
            CommitPendingInputEdits();
            DestroyMenuRoot();
            _scrollStateByPath.Clear();
            _pendingScrollRestore = false;
            _scrollRestorePassesRemaining = 0;

            CleanupMenuState();
        }

        private static void DestroyMenuRoot()
        {
            CommitPendingInputEdits();

            if (_menuRoot != null)
            {
                var lifecycle = _menuRoot.GetComponent<MenuLifecycle>();
                if (lifecycle != null)
                    lifecycle.SuppressCleanup = true;

                _menuRoot.SetActive(false);
            }

            _menuRoot = null;
            GeneratedLayout.Remove();
            foreach (var state in _shaderUiState.Values)
                state.Inputs.Clear();
        }

        private static void CommitPendingInputEdits()
        {
            if (_committingPendingInputEdits)
                return;

            _committingPendingInputEdits = true;
            try
            {
                var anyApplied = false;
                foreach (var state in _shaderUiState.Values)
                {
                    if (state == null)
                        continue;

                    PruneDeadInputs(state);
                    foreach (var kvp in state.Inputs.ToArray())
                    {
                        var input = kvp.Value;
                        if (input == null)
                            continue;

                        var separator = kvp.Key.IndexOf(':');
                        if (separator <= 0 || separator >= kvp.Key.Length - 1)
                            continue;

                        var shaderName = kvp.Key.Substring(0, separator);
                        var path = kvp.Key.Substring(separator + 1);
                        if (string.IsNullOrWhiteSpace(shaderName) || string.IsNullOrWhiteSpace(path))
                            continue;

                        if (ApplyFieldValue(shaderName, path, input.Text ?? string.Empty, applySelectedPack: false, rebuildUi: false))
                            anyApplied = true;
                    }
                }

                if (anyApplied)
                    QueuePackApply(immediate: true);
            }
            finally
            {
                _committingPendingInputEdits = false;
            }
        }

        private static void CleanupMenuState()
        {
            // Every path that ends the menu's lifecycle must restore the native settings panel
            // that EnterExpandedBrowserUiMode() hides — not just the explicit CloseShaderBrowser
            // path. Otherwise an unexpected teardown (host closing the screen, a missed collapse
            // click, etc.) leaves the game's own settings panel permanently disabled with nothing
            // left watching to re-enable it. RestoreSettingsUiState() is a safe no-op if nothing
            // was ever hidden.
            RestoreSettingsUiState();

            _menuOpen = false;
            _menuRoot = null;
            _shaderBrowserExpanded = false;
            _lastConfigClickPackIndex = -1;
            _lastConfigClickTime = 0f;
            _pendingPackApply = false;
            _nextPackApplyAt = 0f;
            _pendingScrollRestore = false;
            _scrollRestorePassesRemaining = 0;
            _saveIndicatorText = string.Empty;
            _saveIndicatorUntil = 0f;
            _pendingSearchFocusRestore = false;
            _pendingExpandResize = false;
            _pendingExpandResizeFrames = 0;
            _pendingUiRebuild = false;
            _pendingUiRebuildPersistState = false;
            _nextUiRebuildAt = 0f;
            _lastUiRebuildAt = 0f;
            IsTyping = false;
            SavePersistentUiState();

            foreach (var state in _shaderUiState.Values)
                state.Inputs.Clear();

            ShaderPackManager.FlushPendingSave();
        }

        // Fixed-depth Transform.Find("A/B/C") paths break the moment the game nests one extra
        // holder or renames a level in between, and silently returning null looks identical to
        // "nothing to hide" — a BFS-by-name search survives structural changes at any depth.
        private static Transform? FindDescendantByName(Transform root, string name, bool exact = true)
        {
            if (root == null || string.IsNullOrEmpty(name))
                return null;

            var queue = new Queue<Transform>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                for (var i = 0; i < current.childCount; i++)
                {
                    var child = current.GetChild(i);
                    if (exact ? child.name == name : child.name.StartsWith(name, StringComparison.Ordinal))
                        return child;
                    queue.Enqueue(child);
                }
            }

            return null;
        }

        private static bool TryGetUiWindows(out RectTransform settingsWindow, out RectTransform modWindow, out RectTransform hostRect)
        {
            settingsWindow = null;
            modWindow = null;
            hostRect = null;

            var uiElement = GameObject.Find("UI");
            if (uiElement == null)
            {
                Debug.LogWarning("[GeneratedUiController] TryGetUiWindows: scene root \"UI\" not found.");
                return false;
            }

            settingsWindow = uiElement.transform.Find("Settings Menu/Holder + Background/Window") as RectTransform;
            modWindow = uiElement.transform.Find("CategoriesWindow Holder/Window(Clone)") as RectTransform;
            if (settingsWindow == null || modWindow == null)
            {
                Debug.LogWarning($"[GeneratedUiController] TryGetUiWindows: settingsWindow found={settingsWindow != null}, modWindow found={modWindow != null}.");
                return false;
            }

            hostRect = settingsWindow.parent?.parent?.parent as RectTransform;
            Debug.Log($"[GeneratedUiController] TryGetUiWindows: settingsWindow={settingsWindow.name} modWindow={modWindow.name} modSize={modWindow.sizeDelta} hostRect={(hostRect != null ? hostRect.name : "null")}");
            return hostRect != null;
        }

        private static void SaveExpandedUiState()
        {
            if (!TryGetUiWindows(out var settingsWindow, out var modWindow, out var hostRect))
                return;

            _settingsWindow = settingsWindow;
            _modWindowRect = modWindow;
            _hostRect = hostRect;

            if (_settingsStateCaptured)
                return;

            _settingsWindowOriginalPos = settingsWindow.anchoredPosition;
            _settingsWindowOriginalAnchorMin = settingsWindow.anchorMin;
            _settingsWindowOriginalAnchorMax = settingsWindow.anchorMax;
            _settingsWindowOriginalPivot = settingsWindow.pivot;
            _settingsWindowOriginalSize = settingsWindow.sizeDelta;

            _modWindowOriginalAnchoredPosition = modWindow.anchoredPosition;
            _modWindowOriginalSizeDelta = modWindow.sizeDelta;
            _modWindowOriginalAnchorMin = modWindow.anchorMin;
            _modWindowOriginalAnchorMax = modWindow.anchorMax;
            _modWindowOriginalPivot = modWindow.pivot;

            _hostRectOriginalSize = hostRect.sizeDelta;

            var panel = settingsWindow.parent != null ? settingsWindow.parent.gameObject : settingsWindow.gameObject;
            // Always refresh the panel reference (it may point to a destroyed object from a
            // previous settings session if SuppressCleanup prevented the usual state reset).
            _nativeSettingsMenuHolder = panel;
            if (!_settingsStateCaptured)
                _nativeSettingsMenuHolderWasActive = panel != null && panel.activeSelf;
            _settingsStateCaptured = true;
        }

        private static void EnterExpandedBrowserUiMode()
        {
            if (!_settingsStateCaptured)
                return;

            if (_nativeSettingsMenuHolder != null && _nativeSettingsMenuHolder.activeSelf)
                _nativeSettingsMenuHolder.SetActive(false);

            if (_modWindowRect == null)
                return;

            const float scale = 1f;
            var fallbackW = Mathf.RoundToInt(UIUtility.CanvasPixelSize.x * 0.68f / Mathf.Max(0.01f, scale));
            var fallbackH = Mathf.RoundToInt(UIUtility.CanvasPixelSize.y * 0.68f / Mathf.Max(0.01f, scale));
            var width = Mathf.RoundToInt(_settingsWindow != null && _settingsWindow.rect.width > 1f ? _settingsWindow.rect.width : (_settingsWindowOriginalSize.x > 1f ? _settingsWindowOriginalSize.x : fallbackW));
            var height = Mathf.RoundToInt(_settingsWindow != null && _settingsWindow.rect.height > 1f ? _settingsWindow.rect.height : (_settingsWindowOriginalSize.y > 1f ? _settingsWindowOriginalSize.y : fallbackH));

            var center = new Vector2(0.5f, 0.5f);
            var expandedSize = new Vector2(Mathf.Max(2200, width), Mathf.Max(1320, height));

            _modWindowRect.anchorMin = center;
            _modWindowRect.anchorMax = center;
            _modWindowRect.pivot = center;
            _modWindowRect.sizeDelta = expandedSize;
            _modWindowRect.anchoredPosition = Vector2.zero;

            Debug.Log($"[GeneratedUiController] EnterExpandedBrowserUiMode: size={_modWindowRect.sizeDelta} panelHidden={_nativeSettingsMenuHolder?.activeSelf == false}");

            // Attach ExpandResizeHelper to Window(Clone) itself — not to _menuRoot, which SFS
            // destroys immediately after BuildMenuPage returns. The helper re-applies the expand
            // every frame so SFS's own layout passes can't silently undo it.
            var existing = _modWindowRect.GetComponent<ExpandResizeHelper>();
            if (existing != null)
                UnityEngine.Object.Destroy(existing);

            var helper = _modWindowRect.gameObject.AddComponent<ExpandResizeHelper>();
            helper.Window = _modWindowRect;
            helper.ExpandedSize = expandedSize;
            helper.PanelToHide = _nativeSettingsMenuHolder;
            helper.OriginalSize = _modWindowOriginalSizeDelta;
            helper.OriginalAnchorMin = _modWindowOriginalAnchorMin;
            helper.OriginalAnchorMax = _modWindowOriginalAnchorMax;
            helper.OriginalPivot = _modWindowOriginalPivot;
            helper.OriginalAnchoredPosition = _modWindowOriginalAnchoredPosition;
            helper.PanelOriginallyActive = _nativeSettingsMenuHolderWasActive;
        }

        private static void RestoreSettingsUiState()
        {
            if (!_settingsStateCaptured)
                return;

            if ((_settingsWindow == null || _modWindowRect == null || _hostRect == null)
                && !TryGetUiWindows(out _settingsWindow, out _modWindowRect, out _hostRect))
            {
                _settingsStateCaptured = false;
                return;
            }

            // ExpandResizeHelper owns the Window(Clone) and panel restore; delegate to it.
            // It holds the exact original values captured at setup time.
            var helper = _modWindowRect != null ? _modWindowRect.GetComponent<ExpandResizeHelper>() : null;
            if (helper != null)
            {
                helper.RestoreAndDestroy();
            }
            else
            {
                // Fallback: restore manually (helper may have been destroyed with the window).
                if (_modWindowRect != null)
                {
                    _modWindowRect.anchorMin = _modWindowOriginalAnchorMin;
                    _modWindowRect.anchorMax = _modWindowOriginalAnchorMax;
                    _modWindowRect.pivot = _modWindowOriginalPivot;
                    _modWindowRect.sizeDelta = _modWindowOriginalSizeDelta;
                    _modWindowRect.anchoredPosition = _modWindowOriginalAnchoredPosition;
                }
                if (_nativeSettingsMenuHolder != null)
                    _nativeSettingsMenuHolder.SetActive(_nativeSettingsMenuHolderWasActive);
            }

            if (_settingsWindow != null)
            {
                _settingsWindow.anchorMin = _settingsWindowOriginalAnchorMin;
                _settingsWindow.anchorMax = _settingsWindowOriginalAnchorMax;
                _settingsWindow.pivot = _settingsWindowOriginalPivot;
                _settingsWindow.sizeDelta = _settingsWindowOriginalSize;
                _settingsWindow.anchoredPosition = _settingsWindowOriginalPos;
            }

            if (_hostRect != null)
                _hostRect.sizeDelta = _hostRectOriginalSize;

            _settingsWindow = null;
            _modWindowRect = null;
            _hostRect = null;
            _nativeSettingsMenuHolder = null;
            _nativeSettingsMenuHolderWasActive = false;
            _settingsStateCaptured = false;
        }

    }
}

