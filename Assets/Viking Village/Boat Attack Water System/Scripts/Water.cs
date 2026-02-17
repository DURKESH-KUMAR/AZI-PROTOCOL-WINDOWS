using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;
using WaterSystem.Data;
using Random = UnityEngine.Random;

namespace WaterSystem
{
    [ExecuteAlways]
    public class Water : MonoBehaviour
    {
        #region Singleton

        private static Water _instance;
        public static Water Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<Water>();
                return _instance;
            }
        }

        #endregion

        #region Fields

        private PlanarReflections _planarReflections;
        private bool _useComputeBuffer;

        [SerializeField] private bool computeOverride = false;
        [SerializeField] private RenderTexture _depthTex;
        [SerializeField] private Texture bakedDepthTex;

        private Camera _depthCam;
        private Texture2D _rampTexture;

        public Wave[] _waves;
        private ComputeBuffer waveBuffer;

        private float _maxWaveHeight;
        private float _waveHeight;

        public WaterSettingsData settingsData;
        public WaterSurfaceData surfaceData;
        [SerializeField] private WaterResources resources;

        #endregion

        #region Shader IDs

        private static readonly int WaterDepthMap = Shader.PropertyToID("_WaterDepthMap");
        private static readonly int WaveHeight = Shader.PropertyToID("_WaveHeight");
        private static readonly int MaxWaveHeight = Shader.PropertyToID("_MaxWaveHeight");
        private static readonly int MaxDepth = Shader.PropertyToID("_MaxDepth");
        private static readonly int WaveCount = Shader.PropertyToID("_WaveCount");
        private static readonly int WaveDataBuffer = Shader.PropertyToID("_WaveDataBuffer");
        private static readonly int WaveData = Shader.PropertyToID("waveData");

        #endregion

        #region Unity Events

        private void OnEnable()
        {
            _useComputeBuffer = !computeOverride &&
                                SystemInfo.supportsComputeShaders &&
                                Application.platform != RuntimePlatform.WebGLPlayer;

            Init();

            RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
        }

        private void OnDisable()
        {
            Cleanup();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Init & Cleanup

        private void Init()
        {
            if (resources == null)
                resources = Resources.Load<WaterResources>("WaterResources");

            SetWaves();
            GenerateColorRamp();

            if (bakedDepthTex != null)
                Shader.SetGlobalTexture(WaterDepthMap, bakedDepthTex);

            SetupPlanarReflection();
        }

        private void Cleanup()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;

            if (waveBuffer != null)
            {
                waveBuffer.Release();
                waveBuffer = null;
            }

            if (_depthCam != null)
            {
                _depthCam.targetTexture = null;
                DestroyImmediate(_depthCam.gameObject);
            }

            if (_depthTex != null)
            {
                _depthTex.Release();
                DestroyImmediate(_depthTex);
            }
        }

        #endregion

        #region Rendering

        private void BeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if (cam.cameraType == CameraType.Preview)
                return;

            if (!cam.TryGetComponent(out UniversalAdditionalCameraData _))
                return;

            if (resources == null || resources.defaultSeaMaterial == null)
                return;

            Matrix4x4 matrix = transform.localToWorldMatrix;

            foreach (var mesh in resources.defaultWaterMeshes)
            {
                Graphics.DrawMesh(
                    mesh,
                    matrix,
                    resources.defaultSeaMaterial,
                    gameObject.layer,
                    cam,
                    0,
                    null,
                    ShadowCastingMode.Off,
                    true,
                    null,
                    LightProbeUsage.Off
                );
            }
        }

        #endregion

        #region Waves

        private void SetWaves()
        {
            SetupWaves(surfaceData._customWaves);

            _maxWaveHeight = 0f;
            foreach (var w in _waves)
                _maxWaveHeight += w.amplitude;

            _maxWaveHeight /= Mathf.Max(_waves.Length, 1);
            _waveHeight = transform.position.y;

            Shader.SetGlobalFloat(WaveHeight, _waveHeight);
            Shader.SetGlobalFloat(MaxWaveHeight, _maxWaveHeight);
            Shader.SetGlobalFloat(MaxDepth, surfaceData._waterMaxVisibility);
            Shader.SetGlobalInt(WaveCount, _waves.Length);

            SetupWaveBuffer();
        }

        private void SetupWaveBuffer()
        {
            if (_useComputeBuffer)
            {
                Shader.EnableKeyword("USE_STRUCTURED_BUFFER");

                waveBuffer?.Release();
                waveBuffer = new ComputeBuffer(_waves.Length, sizeof(float) * 6);
                waveBuffer.SetData(_waves);

                Shader.SetGlobalBuffer(WaveDataBuffer, waveBuffer);
            }
            else
            {
                Shader.DisableKeyword("USE_STRUCTURED_BUFFER");
                Shader.SetGlobalVectorArray(WaveData, GetWaveData());
            }
        }

        private Vector4[] GetWaveData()
        {
            Vector4[] data = new Vector4[_waves.Length];
            for (int i = 0; i < _waves.Length; i++)
            {
                data[i] = new Vector4(
                    _waves[i].amplitude,
                    _waves[i].direction,
                    _waves[i].wavelength,
                    _waves[i].onmiDir
                );
            }
            return data;
        }

        private void SetupWaves(bool custom)
        {
            if (custom)
            {
                _waves = surfaceData._waves.ToArray();
                return;
            }

            var basic = surfaceData._basicWaveSettings;
            _waves = new Wave[basic.numWaves];

            Random.InitState(surfaceData.randomSeed);

            for (int i = 0; i < basic.numWaves; i++)
            {
                float amp = basic.amplitude * Random.Range(0.8f, 1.2f);
                float dir = basic.direction + Random.Range(-90f, 90f);
                float len = basic.wavelength * Random.Range(0.6f, 1.4f);

                _waves[i] = new Wave(amp, dir, len, Vector2.zero, false);
            }
        }

        #endregion

        #region Planar Reflection

        private void SetupPlanarReflection()
        {
            if (!TryGetComponent(out _planarReflections))
                _planarReflections = gameObject.AddComponent<PlanarReflections>();

            _planarReflections.enabled =
                settingsData.refType == ReflectionType.PlanarReflection;
        }

        #endregion

        #region Depth Capture

        [ContextMenu("Capture Depth")]
        public void CaptureDepthMap()
        {
            if (_depthCam == null)
            {
                GameObject go = new GameObject("WaterDepthCam");
                _depthCam = go.AddComponent<Camera>();
                go.hideFlags = HideFlags.HideAndDontSave;
            }

            _depthCam.orthographic = true;
            _depthCam.orthographicSize = 250;
            _depthCam.nearClipPlane = 0.1f;
            _depthCam.farClipPlane = surfaceData._waterMaxVisibility;

            if (_depthTex == null)
            {
                _depthTex = new RenderTexture(1024, 1024, 24, RenderTextureFormat.Depth);
                _depthTex.name = "WaterDepthMap";
            }

            _depthCam.targetTexture = _depthTex;
            _depthCam.Render();

            Shader.SetGlobalTexture(WaterDepthMap, _depthTex);
        }

        #endregion

        #region Ramp Texture

        private void GenerateColorRamp()
        {
            if (_rampTexture == null)
                _rampTexture = new Texture2D(128, 1, TextureFormat.RGBA32, false);

            _rampTexture.wrapMode = TextureWrapMode.Clamp;

            for (int i = 0; i < 128; i++)
            {
                _rampTexture.SetPixel(i, 0,
                    surfaceData._absorptionRamp.Evaluate(i / 128f));
            }

            _rampTexture.Apply();
        }

        #endregion
    }
}
