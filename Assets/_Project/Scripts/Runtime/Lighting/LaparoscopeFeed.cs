using SurgicalFoundations.Audio;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SurgicalFoundations.Lighting
{
    /// <summary>
    /// Renders the laparoscope camera into a RenderTexture shown on the tower monitor. The feed fades in when the
    /// Operate stage loads. Resolution and refresh rate are the main performance levers for the second camera
    /// (see ExecutionPlan decision D7).
    /// </summary>
    public class LaparoscopeFeed : MonoBehaviour
    {
        [SerializeField] Camera scopeCamera;
        [SerializeField] Vector2Int resolution = new Vector2Int(960, 540);
        [SerializeField, Tooltip("Render every Nth frame. 1 = every frame, 2 = 36 fps on a 72 Hz headset.")]
        [Range(1, 4)] int renderEveryNthFrame = 1;
        [SerializeField] float powerOnTime = 1.2f;

        Renderer monitorScreen;
        RenderTexture target;
        float power;
        MaterialPropertyBlock mpb;
        static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        void Start()
        {
            if (scopeCamera == null) scopeCamera = GetComponentInChildren<Camera>();
            var screenGo = GameObject.Find("LapTower_Screen");
            if (screenGo != null) monitorScreen = screenGo.GetComponent<Renderer>();

            target = new RenderTexture(resolution.x, resolution.y, 24, RenderTextureFormat.ARGB32) { name = "RT_Laparoscope", antiAliasing = 2 };
            scopeCamera.targetTexture = target;
            scopeCamera.GetUniversalAdditionalCameraData().allowXRRendering = false;
            mpb = new MaterialPropertyBlock();
            AudioManager.Instance?.PlayAt(SoundId.ENV_MonitorPowerOn, monitorScreen != null ? monitorScreen.transform.position : transform.position);
        }

        void Update()
        {
            scopeCamera.enabled = Time.frameCount % renderEveryNthFrame == 0;
            if (monitorScreen == null || power >= 1f) return;
            power = Mathf.Clamp01(power + Time.deltaTime / powerOnTime);
            monitorScreen.GetPropertyBlock(mpb);
            mpb.SetTexture(BaseMap, target);
            mpb.SetTexture(EmissionMap, target);
            mpb.SetColor(BaseColor, Color.white * power);
            mpb.SetColor(EmissionColor, Color.white * (1.2f * power));
            monitorScreen.SetPropertyBlock(mpb);
        }

        void OnDestroy()
        {
            if (monitorScreen != null) monitorScreen.SetPropertyBlock(null);
            if (target != null) { target.Release(); Destroy(target); }
        }
    }
}
