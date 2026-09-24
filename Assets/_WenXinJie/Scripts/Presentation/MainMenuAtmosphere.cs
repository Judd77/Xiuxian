using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace WenXinJie.Presentation
{
    internal sealed class MainMenuAtmosphere : MonoBehaviour
    {
        private const int ParticleLayer = 31;
        private const float RigPositionX = 1000f;
        private const string TransparentSurfaceKeyword = "_SURFACE_TYPE_TRANSPARENT";
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
        private static readonly int BlendId = Shader.PropertyToID("_Blend");
        private static readonly int SourceBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DestinationBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int SourceBlendAlphaId = Shader.PropertyToID("_SrcBlendAlpha");
        private static readonly int DestinationBlendAlphaId = Shader.PropertyToID("_DstBlendAlpha");
        private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");

        private GameObject _rig;
        private RenderTexture _renderTexture;
        private Texture2D _particleTexture;
        private Material _mistMaterial;
        private Material _goldenLightMaterial;

        public static void Create(Transform parent)
        {
            RectTransform root = StoryUiFactory.Box(parent, "MainMenuAtmosphere", 0f, 0f, 1920f, 1080f);
            var image = root.gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            var atmosphere = root.gameObject.AddComponent<MainMenuAtmosphere>();
            atmosphere.Initialize(image, parent.GetComponentInParent<StoryApplication>().transform);
        }

        private void OnEnable()
        {
            if (_rig != null)
            {
                _rig.SetActive(true);
            }
        }

        private void OnDisable()
        {
            if (_rig != null)
            {
                _rig.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_rig != null)
            {
                Destroy(_rig);
            }

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }

            if (_mistMaterial != null)
            {
                Destroy(_mistMaterial);
            }

            if (_goldenLightMaterial != null)
            {
                Destroy(_goldenLightMaterial);
            }

            if (_particleTexture != null)
            {
                Destroy(_particleTexture);
            }
        }

        private void Initialize(RawImage target, Transform owner)
        {
            _renderTexture = new(960, 540, 24, RenderTextureFormat.ARGB32)
            {
                name = "MainMenuParticles",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _renderTexture.Create();
            target.texture = _renderTexture;

            _particleTexture = CreateParticleTexture();
            _mistMaterial = CreateParticleMaterial(_particleTexture, false);
            _goldenLightMaterial = CreateParticleMaterial(_particleTexture, true);
            _rig = new GameObject("MainMenuParticleRig");
            _rig.transform.SetParent(owner, false);
            _rig.transform.position = new Vector3(RigPositionX, 0f, 0f);
            SetLayer(_rig, ParticleLayer);
            CreateCamera(_rig.transform);
            CreateMist(_rig.transform);
            CreateGoldenLights(_rig.transform);
        }

        private void CreateCamera(Transform parent)
        {
            var cameraObject = new GameObject("ParticleCamera", typeof(Camera));
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);
            cameraObject.layer = ParticleLayer;
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.cullingMask = 1 << ParticleLayer;
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 30f;
            camera.targetTexture = _renderTexture;
            camera.allowHDR = false;
            camera.allowMSAA = false;
        }

        private void CreateMist(Transform parent)
        {
            var particles = CreateSystem(parent, "MistParticles", _mistMaterial);
            ParticleSystem.MainModule main = particles.main;
            main.maxParticles = 48;
            main.startLifetime = new(16f, 25f);
            main.startSpeed = 0f;
            main.startSize3D = true;
            main.startSizeX = new(6f, 12f);
            main.startSizeY = new(1.4f, 3f);
            main.startSizeZ = 1f;
            var minimumMistColor = new Color(0.76f, 0.88f, 0.84f, 0.22f);
            var maximumMistColor = new Color(0.94f, 0.97f, 0.92f, 0.42f);
            main.startColor = new(minimumMistColor, maximumMistColor);

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 1.8f;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(20f, 8f, 0.1f);
            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            velocity.y = new(0f, 0f);
            velocity.z = new(0f, 0f);
            var noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.16f;
            noise.frequency = 0.12f;
            noise.scrollSpeed = 0.08f;
            ApplyFade(particles, 0.12f, 0.28f);
            particles.Play();
        }

        private void CreateGoldenLights(Transform parent)
        {
            var particles = CreateSystem(parent, "GoldenLightParticles", _goldenLightMaterial);
            ParticleSystem.MainModule main = particles.main;
            main.maxParticles = 120;
            main.startLifetime = new(6f, 11f);
            main.startSpeed = 0f;
            main.startSize = new(0.14f, 0.42f);
            var minimumLightColor = new Color(1f, 0.68f, 0.22f, 0.78f);
            var maximumLightColor = new Color(1f, 0.94f, 0.60f, 1f);
            main.startColor = new(minimumLightColor, maximumLightColor);

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 8f;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(18f, 9f, 0.1f);
            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.04f, 0.05f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.10f, 0.26f);
            velocity.z = new(0f, 0f);
            ParticleSystem.NoiseModule noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.08f;
            noise.frequency = 0.25f;
            noise.scrollSpeed = 0.18f;
            ApplyFade(particles, 0.18f, 0.35f);
            particles.Play();
        }

        private static ParticleSystem CreateSystem(Transform parent, string systemName, Material material)
        {
            var particleObject = new GameObject(systemName, typeof(ParticleSystem));
            particleObject.transform.SetParent(parent, false);
            particleObject.layer = ParticleLayer;
            ParticleSystem particles = particleObject.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Local;
            ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            return particles;
        }

        private static void ApplyFade(ParticleSystem particles, float fadeIn, float fadeOut)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, fadeIn),
                    new GradientAlphaKey(1f, 1f - fadeOut),
                    new GradientAlphaKey(0f, 1f)
                });
            ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
            color.enabled = true;
            color.color = gradient;
        }

        private static Material CreateParticleMaterial(Texture texture, bool additive)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Particles/Standard Unlit");
            }

            Material material = new(shader)
            {
                name = additive ? "MainMenuGoldenLightMaterial" : "MainMenuMistMaterial",
                hideFlags = HideFlags.HideAndDontSave
            };
            material.SetTexture(BaseMapId, texture);
            material.SetColor(BaseColorId, Color.white);
            material.SetFloat(SurfaceId, 1f);
            material.SetFloat(BlendId, additive ? 2f : 0f);
            material.SetFloat(SourceBlendId, (float)BlendMode.SrcAlpha);
            material.SetFloat(DestinationBlendId,
                additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat(SourceBlendAlphaId, (float)BlendMode.One);
            material.SetFloat(DestinationBlendAlphaId, (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat(ZWriteId, 0f);
            material.EnableKeyword(TransparentSurfaceKeyword);
            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }

        private static Texture2D CreateParticleTexture()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "MainMenuParticleTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float horizontal = (x + 0.5f) / size * 2f - 1f;
                    float vertical = (y + 0.5f) / size * 2f - 1f;
                    float distance = Mathf.Sqrt(horizontal * horizontal + vertical * vertical);
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 2.2f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static void SetLayer(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform)
            {
                SetLayer(child.gameObject, layer);
            }
        }
    }
}