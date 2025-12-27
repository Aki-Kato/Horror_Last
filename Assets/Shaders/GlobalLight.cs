using UnityEngine;

[ExecuteAlways]
public class GlobalLightUploader : MonoBehaviour
{
    const int MAX_LIGHTS = 256;
    const float MAX_DISTANCE = 200f;

    static readonly int LightCountID = Shader.PropertyToID("_GlobalLightCount");
    static readonly int LightPosID   = Shader.PropertyToID("_GlobalLightPos");
    static readonly int LightDirID   = Shader.PropertyToID("_GlobalLightDir");
    static readonly int LightColorID = Shader.PropertyToID("_GlobalLightColor");
    static readonly int LightParamID = Shader.PropertyToID("_GlobalLightParam");
    // param: x = range, y = type (0=dir,1=point,2=spot), z = spotCos

    Vector4[] pos   = new Vector4[MAX_LIGHTS];
    Vector4[] dir   = new Vector4[MAX_LIGHTS];
    Vector4[] color = new Vector4[MAX_LIGHTS];
    Vector4[] param = new Vector4[MAX_LIGHTS];

    void LateUpdate()
    {
        var lights = FindObjectsOfType<Light>(false);
        var cam = Camera.main;
        Vector3 camPos = cam ? cam.transform.position : Vector3.zero;

        int count = 0;

        foreach (var l in lights)
        {
            if (!l.enabled) continue;
            if (count >= MAX_LIGHTS) break;

            // Directional — всегда берём
            if (l.type != LightType.Directional)
            {
                float d = Vector3.Distance(l.transform.position, camPos);
                if (d > MAX_DISTANCE) continue;
            }

            color[count] = l.color * l.intensity;

            if (l.type == LightType.Directional)
            {
                pos[count]   = Vector4.zero;
                dir[count]   = -l.transform.forward;
                param[count] = new Vector4(0, 0, 0, 0);
            }
            else
            {
                pos[count] = l.transform.position;
                dir[count] = -l.transform.forward;

                float type = (l.type == LightType.Point) ? 1 : 2;
                float spotCos = (l.type == LightType.Spot)
                    ? Mathf.Cos(l.spotAngle * Mathf.Deg2Rad * 0.5f)
                    : 0f;

                param[count] = new Vector4(l.range, type, spotCos, 0);
            }

            count++;
        }

        Shader.SetGlobalInt(LightCountID, count);
        Shader.SetGlobalVectorArray(LightPosID, pos);
        Shader.SetGlobalVectorArray(LightDirID, dir);
        Shader.SetGlobalVectorArray(LightColorID, color);
        Shader.SetGlobalVectorArray(LightParamID, param);
    }
}
