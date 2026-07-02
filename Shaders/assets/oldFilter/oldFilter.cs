using shaders.Lib;
using shaders.Lib.ShaderModules.ShaderPack;
using shaders.Lib.Renders;
using static shaders.Lib.ShaderModules.ShaderPack.ShaderRequirementAttribute;

namespace shaders.Effects
{
    [ShaderPack("Old Filter", "Vintage film filter effects", author: "shaders", version: "2.0")]
    public sealed class OldFilterShaderPack : ShaderPackBase
    {
        [ShaderRequirement(typeof(OldFilterShaderModule), FailCondition.DisablePack, "Old Filter")]
        public object? OldFilterRequirement;
    }

    public struct OldFilterShaderArgs
    {
        [ShaderArg(property: "_Contrast", defaultValue: 1.25f)] public float Contrast;
        [ShaderArg(property: "_Exposure", defaultValue: 1.05f)] public float Exposure;
        [ShaderArg(property: "_Gamma", defaultValue: 0.95f)] public float Gamma;
        [ShaderArg(property: "_GrainAmount", defaultValue: 0.18f)] public float GrainAmount;
        [ShaderArg(property: "_GrainSpeed", defaultValue: 24f)] public float GrainSpeed;
        [ShaderArg(property: "_FlickerAmt", defaultValue: 0.06f)] public float FlickerAmt;
        [ShaderArg(property: "_VignetteAmt", defaultValue: 0.35f)] public float VignetteAmt;
        [ShaderArg(property: "_ScanlineAmt", defaultValue: 0.06f)] public float ScanlineAmt;
        [ShaderArg(property: "_DustChance", defaultValue: 0.0022f)] public float DustChance;
        [ShaderArg(property: "_ScratchAmt", defaultValue: 0.22f)] public float ScratchAmt;
        [ShaderArg(property: "_ScratchWidth", defaultValue: 1)] public int ScratchWidth;
        [ShaderArg(property: "_TimeSpeed", defaultValue: 0.1f)] public float TimeSpeed;
    }

    [ShaderModule("OldFilter", ShaderType.Shader, "Hidden/shaders/OldFilter", OverlayRenderMode.Inclusive)]
    [ShaderRoute(ShaderRouteMode.ExcludeListed, new[] { "home", "mainmenu" })]
    public sealed class OldFilterShaderModule : ShaderModule<OldFilterShaderArgs, object>
    {
        public OldFilterShaderModule() { }

        public override object Run(in OldFilterShaderArgs args)
        {
            return null;
        }
    }
}