# Building a part pack for SFS Shaders

This walks through turning a folder of Unity assets (and optionally C# shader
code) into a `.pack` file that Spaceflight Simulator can load.

## 1. Set up the tool

```
pip install sfspack/dist/sfspack-*.whl
```

Check it's on your PATH:

```
sfspack --help
```

## 2. Start a project

Either copy `example-pack/` from this SDK (it's a real, working shader pack —
the same one shipped with the SFS Shaders mod) and rename things, or start
fresh:

```
sfspack init MyPartsPack
cd MyPartsPack
```

Either way you end up with:

```
MyPartsPack/
  pack.config.json
  assets/
    my_parts_pack/
      pack_meta.json
      icon.png            (optional)
      ... your Unity assets ...
  Editor/
    BundleBuilder.cs       (copy into your Unity project's Assets/Editor/)
  README.md
```

Every top-level folder under `assets/` becomes one asset bundle, named after
the folder. Drop a `pack_meta.json` in a bundle folder to set its display
name/author/version/description, and an `icon.png` for its icon.

## 3. Wire up `pack.config.json`

Copy the DLLs from this SDK's `dependencies/` folder next to your project (or
point at them directly), then edit:

```jsonc
{
  "project_name": "MyPartsPack",
  "assets_folder": "assets",
  "output_folder": "<path to Mods/Custom_Assets/Parts>",
  "asset_bundle_label": "my_parts_pack",

  "unity_project_path": "<path to your SFS Modding Toolkit Unity project>",
  "unity_path": "<path to Unity.exe for that project's Unity version>",

  "platforms": ["windows"],
  "code_assembly": {
    "dependencies_path": "<path to this SDK's dependencies/ folder>",
    "mod_assembly_path": "<path to dependencies/sfs-shaders.dll>",
    "target_framework": "netstandard2.1"
  },

  "pack": {
    "display_name": "My Parts Pack",
    "author": "you",
    "version": "1.0.0",
    "description": "What this pack does.",
    "show_icon": false,
    "icon": "icon.png"
  }
}
```

`code_assembly` is only needed if your pack ships C# (custom shader modules,
part behaviour, etc.) — see `example-pack/assets/*/*.cs` for a real example of
a shader module that references the mod's `shaders.Lib` types. If your pack is
assets-only, you can drop the `code_assembly` block entirely.

## 4. Copy the Unity-side helper once per project

```
cp Editor/BundleBuilder.cs <your Unity project>/Assets/Editor/BundleBuilder.cs
```

This is the only piece that actually talks to Unity — everything else
(scaffolding, config, discovery, `.pack` assembly) is pure Python.

## 5. Build

```
sfspack build
```

This shells out to Unity in batch mode to compile the AssetBundle(s), compiles
any `.cs` files into a CodeAssembly against the mod DLL, and writes one
`<bundle_label>.pack` file per discovered bundle into `output_folder`.

## Reference: the example pack in this SDK

`example-pack/` is the actual shader pack bundled with the SFS Shaders mod. It
has two bundles under `assets/` (`Atmo`, `oldFilter`), each with both Unity
assets (`.shader` files) and a `.cs` module (`AtmoShader.cs`, `oldFilter.cs`)
that hooks into the mod's shader registry — a good template for a pack that
mixes assets and code.
