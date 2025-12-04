using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[System.Serializable, VolumeComponentMenu("Post-processing/AKI/PixelateRT")]
public sealed class AKI_HDRP_PixelateRT : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    public ClampedIntParameter pixelSize = new ClampedIntParameter(4, 1, 128);
    public ColorParameter tint = new ColorParameter(Color.white);

    static readonly int _InputTextureID   = Shader.PropertyToID("_InputTexture");
    static readonly int _PixelSizeID      = Shader.PropertyToID("_PixelSize");
    static readonly int _TintID           = Shader.PropertyToID("_Tint");
    static readonly int _TexelSizeID      = Shader.PropertyToID("_InputTexture_TexelSize");

    Material _material;

    public override CustomPostProcessInjectionPoint injectionPoint =>
        CustomPostProcessInjectionPoint.AfterPostProcess;

    public bool IsActive() => _material != null && pixelSize.value > 1;

    public override void Setup()
    {
        var shader = Shader.Find("AKI/HDRP/PixelateRT");
        if (shader != null)
            _material = new Material(shader);
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        if (_material == null)
        {
            HDUtils.BlitCameraTexture(cmd, source, destination);
            return;
        }

        _material.SetTexture(_InputTextureID, source);
        _material.SetFloat(_PixelSizeID, pixelSize.value);
        _material.SetColor(_TintID, tint.value);

        var rt = source.rt;
        _material.SetVector(_TexelSizeID, new Vector4(
            1f / rt.width,
            1f / rt.height,
            rt.width,
            rt.height
        ));

        HDUtils.DrawFullScreen(cmd, _material, destination);
    }

    public override void Cleanup()
    {
        CoreUtils.Destroy(_material);
    }
}
