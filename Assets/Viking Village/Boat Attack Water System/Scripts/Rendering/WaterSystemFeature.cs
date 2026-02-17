using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace WaterSystem
{
    public class WaterSystemFeature : ScriptableRendererFeature
    {
        #region Water Effects Pass

        class WaterFxPass : ScriptableRenderPass
        {
            private const string k_RenderWaterFXTag = "Render Water FX";
            private readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_RenderWaterFXTag);
            private readonly ShaderTagId m_ShaderTag = new ShaderTagId("WaterFX");
            private readonly Color m_ClearColor = new Color(0f, 0.5f, 0.5f, 0.5f);

            private FilteringSettings m_FilteringSettings;
            private RTHandle m_WaterFXHandle;

            public WaterFxPass()
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
                m_FilteringSettings = new FilteringSettings(RenderQueueRange.transparent);
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                cameraTextureDescriptor.depthBufferBits = 0;
                cameraTextureDescriptor.width /= 2;
                cameraTextureDescriptor.height /= 2;
                cameraTextureDescriptor.colorFormat = RenderTextureFormat.Default;

                RenderingUtils.ReAllocateIfNeeded(
                    ref m_WaterFXHandle,
                    cameraTextureDescriptor,
                    FilterMode.Bilinear,
                    TextureWrapMode.Clamp,
                    name: "_WaterFXMap"
                );

                ConfigureTarget(m_WaterFXHandle);
                ConfigureClear(ClearFlag.Color, m_ClearColor);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get(k_RenderWaterFXTag);

                using (new ProfilingScope(cmd, m_ProfilingSampler))
                {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    var drawSettings = CreateDrawingSettings(
                        m_ShaderTag,
                        ref renderingData,
                        SortingCriteria.CommonTransparent
                    );

                    context.DrawRenderers(
                        renderingData.cullResults,
                        ref drawSettings,
                        ref m_FilteringSettings
                    );
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                if (m_WaterFXHandle != null)
                {
                    m_WaterFXHandle.Release();
                }
            }
        }

        #endregion

        #region Caustics Pass

        class WaterCausticsPass : ScriptableRenderPass
        {
            private const string k_RenderWaterCausticsTag = "Render Water Caustics";
            private readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_RenderWaterCausticsTag);

            public Material WaterCausticMaterial;
            private static Mesh m_Mesh;

            public WaterCausticsPass()
            {
                renderPassEvent = RenderPassEvent.AfterRenderingSkybox + 1;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cam = renderingData.cameraData.camera;

                if (cam.cameraType == CameraType.Preview || WaterCausticMaterial == null)
                    return;

                CommandBuffer cmd = CommandBufferPool.Get(k_RenderWaterCausticsTag);

                using (new ProfilingScope(cmd, m_ProfilingSampler))
                {
                    var sunMatrix = RenderSettings.sun != null
                        ? RenderSettings.sun.transform.localToWorldMatrix
                        : Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(-45f, 45f, 0f), Vector3.one);

                    WaterCausticMaterial.SetMatrix("_MainLightDir", sunMatrix);

                    if (m_Mesh == null)
                        m_Mesh = GenerateCausticsMesh(1000f);

                    var position = cam.transform.position;
                    position.y = 0;

                    var matrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);

                    cmd.DrawMesh(m_Mesh, matrix, WaterCausticMaterial, 0, 0);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        #endregion

        private WaterFxPass m_WaterFxPass;
        private WaterCausticsPass m_CausticsPass;

        public WaterSystemSettings settings = new WaterSystemSettings();

        [SerializeField] private Shader causticShader;
        [SerializeField] private Texture2D causticTexture;

        private Material m_CausticMaterial;

        private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int Size = Shader.PropertyToID("_Size");
        private static readonly int CausticTextureID = Shader.PropertyToID("_CausticMap");

        public override void Create()
        {
            m_WaterFxPass = new WaterFxPass();
            m_CausticsPass = new WaterCausticsPass();

            if (causticShader == null)
                causticShader = Shader.Find("Hidden/BoatAttack/Caustics");

            if (causticShader == null)
                return;

            if (m_CausticMaterial != null)
                DestroyImmediate(m_CausticMaterial);

            m_CausticMaterial = CoreUtils.CreateEngineMaterial(causticShader);

            m_CausticMaterial.SetFloat("_BlendDistance", settings.causticBlendDistance);
            m_CausticMaterial.SetFloat(Size, settings.causticScale);
            m_CausticMaterial.SetTexture(CausticTextureID, causticTexture);

            switch (settings.debug)
            {
                case WaterSystemSettings.DebugMode.Caustics:
                    m_CausticMaterial.SetFloat(SrcBlend, 1f);
                    m_CausticMaterial.SetFloat(DstBlend, 0f);
                    m_CausticMaterial.EnableKeyword("_DEBUG");
                    m_CausticsPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
                    break;

                case WaterSystemSettings.DebugMode.Disabled:
                    m_CausticMaterial.SetFloat(SrcBlend, 2f);
                    m_CausticMaterial.SetFloat(DstBlend, 0f);
                    m_CausticMaterial.DisableKeyword("_DEBUG");
                    break;
            }

            m_CausticsPass.WaterCausticMaterial = m_CausticMaterial;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_WaterFxPass);
            renderer.EnqueuePass(m_CausticsPass);
        }

        private static Mesh GenerateCausticsMesh(float size)
        {
            Mesh mesh = new Mesh();
            size *= 0.5f;

            mesh.vertices = new[]
            {
                new Vector3(-size, 0f, -size),
                new Vector3(size, 0f, -size),
                new Vector3(-size, 0f, size),
                new Vector3(size, 0f, size)
            };

            mesh.triangles = new[]
            {
                0, 2, 1,
                2, 3, 1
            };

            mesh.RecalculateNormals();
            return mesh;
        }

        [System.Serializable]
        public class WaterSystemSettings
        {
            [Range(0.1f, 1f)]
            public float causticScale = 0.25f;

            public float causticBlendDistance = 3f;

            public DebugMode debug = DebugMode.Disabled;

            public enum DebugMode
            {
                Disabled,
                WaterEffects,
                Caustics
            }
        }
    }
}
