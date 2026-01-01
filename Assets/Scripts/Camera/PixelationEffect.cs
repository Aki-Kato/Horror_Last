using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class PixelationEffect : MonoBehaviour
{
    public Material effectMaterial;

    [Range(1, 1024)]
    public int pixelSize = 256;

    [Range(2, 256)]
    public int colorLevels = 16;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (effectMaterial != null)
        {
            // Передаем параметры в шейдер
            effectMaterial.SetFloat("_PixelSize", pixelSize);
            effectMaterial.SetFloat("_ColorLevels", colorLevels);
            
            // Отрисовываем эффект через виртуальную текстуру (Blit)
            Graphics.Blit(source, destination, effectMaterial);
        }
        else
        {
            // Если материала нет, просто выводим картинку как есть
            Graphics.Blit(source, destination);
        }
    }
}