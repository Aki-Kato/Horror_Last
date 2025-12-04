using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Experimental.Rendering; // RTHandles

[ExecuteAlways]
[DisallowMultipleComponent]
public class AKI_HDRP_PixelateCameraPass : MonoBehaviour
{
    [Range(1, 128)]
    public int pixelSize = 6;

    // Материал на шейдере "AKI/HDRP/PixelateFullScreen"
    public Material pixelateMaterial;

    CustomPassVolume _volume;
    PixelatePass _pass;

    void OnEnable()
    {
        // добавляем/находим CustomPassVolume на ЭТОМ объекте
        _volume = GetComponent<CustomPassVolume>();
        if (_volume == null)
            _volume = gameObject.AddComponent<CustomPassVolume>();

        _volume.isGlobal = true;
        _volume.injectionPoint = CustomPassInjectionPoint.AfterPostProcess;

        // создаём наш пасс
        _pass = null;
        if (_volume.customPasses.Count > 0)
            _pass = _volume.customPasses[0] as PixelatePass;

        if (_pass == null)
        {
            _pass = new PixelatePass();
            if (_volume.customPasses.Count == 0)
                _volume.customPasses.Add(_pass);
            else
                _volume.customPasses[0] = _pass;
        }

        // передаём стартовые значения
        _pass.pixelSize = pixelSize;
        _pass.runtimeMaterial = pixelateMaterial;
    }

    void OnDisable()
    {
        if (_pass != null)
            _pass.CleanupPass();
    }

    void Update()
    {
        if (_pass == null) return;

        _pass.pixelSize = pixelSize;

        // если материал подцепили позже
        if (_pass.runtimeMaterial == null && pixelateMaterial != null)
            _pass.runtimeMaterial = pixelateMaterial;
    }

    // ================== сам кастом-пасс ==================
    class PixelatePass : CustomPass
    {
        public int pixelSize = 6;
        public Material runtimeMaterial;

        RTHandle tempRT;

        static readonly int _InputTextureID = Shader.PropertyToID("_InputTexture");
        static readonly int _InputTexelID   = Shader.PropertyToID("_InputTexture_TexelSize");
        static readonly int _PixelSizeID    = Shader.PropertyToID("_PixelSize");

        protected override void Execute(CustomPassContext ctx)
        {
            if (runtimeMaterial == null)
                return;

            // текущий цветовой буфер камеры
            var src = ctx.cameraColorBuffer;
            EnsureTempRT(ctx, src);

            // 1) копируем камеру в наш временный RT
            HDUtils.BlitCameraTexture(ctx.cmd, src, tempRT);

            // 2) настраиваем материал
            var rt = tempRT.rt;
            runtimeMaterial.SetTexture(_InputTextureID, tempRT);
            runtimeMaterial.SetVector(_InputTexelID, new Vector4(
                1f / rt.width,
                1f / rt.height,
                rt.width,
                rt.height
            ));
            runtimeMaterial.SetFloat(_PixelSizeID, pixelSize);

            // 3) рисуем обратно в буфер камеры
            CoreUtils.DrawFullScreen(ctx.cmd, runtimeMaterial, src);
        }

        void EnsureTempRT(CustomPassContext ctx, RTHandle source)
        {
            int w = source.rt.width;
            int h = source.rt.height;

            if (tempRT == null || tempRT.rt.width != w || tempRT.rt.height != h)
            {
                if (tempRT != null)
                    RTHandles.Release(tempRT);

                tempRT = RTHandles.Alloc(
                    w, h,
                    colorFormat: source.rt.graphicsFormat,
                    name: "AKI_Pixelate_Temp"
                );
            }
        }

        protected override void Cleanup()
        {
            CleanupPass();
        }

        public void CleanupPass()
        {
            if (tempRT != null)
            {
                RTHandles.Release(tempRT);
                tempRT = null;
            }
        }
    }
}
