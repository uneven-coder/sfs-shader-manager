# SFS Shaders — Part Pack SDK

Everything needed to build a part pack that targets the SFS Shaders mod, without
having to dig the pieces out of the mod repo yourself.

## What's in here

```
dependencies/       Game/mod DLLs your pack's C# code compiles against
sfspack/             The pack-builder CLI tool (wheel + source)
example-pack/       A working shader pack you can copy as a starting point
GUIDE.md            Walkthrough for building a part pack
```

## Quick start

1. Install the tool:
   ```
   pip install sfspack/dist/sfspack-*.whl
   ```
   (or `pip install -e sfspack/source` if you want to hack on the tool itself)

2. Copy `example-pack/` to a new folder for your own pack, or run
   `sfspack init MyPack` and merge in the `dependencies/` folder from this SDK.

3. Open `pack.config.json` in your new pack folder and edit the paths marked
   below — see [GUIDE.md](GUIDE.md) for the full walkthrough.

## Paths you need to edit in `pack.config.json`

| Key | What to set it to |
| --- | --- |
| `unity_project_path` | Your local clone of the SFS Modding Toolkit Unity project |
| `unity_path` | Your local Unity Editor executable (must match the Modding Toolkit's Unity version) |
| `output_folder` | Your SFS `Mods/Custom_Assets/Parts` folder |
| `code_assembly.dependencies_path` | Path to this SDK's `dependencies/` folder |
| `code_assembly.mod_assembly_path` | Path to `dependencies/sfs-shaders.dll` (or wherever you installed the mod) |

## Dependencies you need to get yourself

These aren't bundled in this SDK (too large / license-restricted to redistribute):

- **Unity Editor** — same version the SFS Modding Toolkit project targets.
- **SFS Modding Toolkit** Unity project — used only for the one step that needs
  Unity: compiling raw assets into an AssetBundle.
- **Python 3.8+** — to run `sfspack`.
- **Spaceflight Simulator** itself, with the SFS Shaders mod installed, if you
  want your pack's shader config UI to actually show up in-game.

Everything else (the DLLs your C# references and the pack-builder tool) is
already in this SDK.
