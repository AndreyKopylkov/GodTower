using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace GodTower.Environment
{
    /// <summary>
    /// Soft cartoon clouds behind the tower in a few depth layers. Clouds are world-space sprites, so the climbing camera
    /// gives natural parallax (near layers scroll faster than far ones); every layer wraps around the camera height
    /// (an endless sky for any tower height) and drifts slowly sideways. Farther layers fade into the horizon colour.
    /// </summary>
    public sealed class CloudField : MonoBehaviour
    {
        [SerializeField] private Sprite[] _sprites = Array.Empty<Sprite>();
        [Tooltip("Unlit sprite material (no fog: depth haze is done with the layer tint).")]
        [SerializeField] private Material _material;
        [SerializeField] private CloudLayer[] _layers = DefaultLayers();
        [Tooltip("Extra view height the wrap band covers, for the hero-boost zoom-out and camera shakes.")]
        [SerializeField, Min(1f)] private float _bandHeadroom = 1.4f;
        [SerializeField] private int _seed = 11;

        private readonly List<Cloud> _clouds = new();
        private Camera _camera;
        private Color _nearColor = Color.white;
        private Color _horizonColor = Color.white;
        private float _opacity = 1f;

        /// <summary>Clouds spawned (diagnostics and tests).</summary>
        public int Count => _clouds.Count;

        /// <summary>Tints every layer: near clouds take <paramref name="nearColor"/>, far ones blend towards the horizon.</summary>
        public void SetColors(Color nearColor, Color horizonColor, float opacity)
        {
            _nearColor = nearColor;
            _horizonColor = horizonColor;
            _opacity = opacity;
            foreach (Cloud cloud in _clouds)
                cloud.Renderer.color = LayerColor(cloud.Layer);
        }

        private void Start()
        {
            _camera = Camera.main;
            if (_camera == null || _sprites.Length == 0)
                return;

            var random = new Random(_seed);
            foreach (CloudLayer layer in _layers)
            {
                Vector2 band = Band(layer);
                for (int i = 0; i < layer.Count; i++)
                    _clouds.Add(CreateCloud(layer, band, random, i));
            }
        }

        private void LateUpdate()
        {
            if (_camera == null)
                return;

            Vector3 view = _camera.transform.position;
            float deltaTime = Time.deltaTime;
            foreach (Cloud cloud in _clouds)
            {
                Vector3 position = cloud.Transform.position;
                cloud.X += cloud.Layer.Drift * deltaTime;
                position.x = Wrap(cloud.X, view.x, cloud.Band.x);
                position.y = Wrap(position.y, view.y, cloud.Band.y);
                cloud.Transform.position = position;
            }
        }

        private Cloud CreateCloud(CloudLayer layer, Vector2 band, Random random, int index)
        {
            var cloudObject = new GameObject($"Cloud_{layer.Depth:0}_{index:00}");
            cloudObject.transform.SetParent(transform, false);

            var spriteRenderer = cloudObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = _sprites[random.Next(_sprites.Length)];
            if (_material != null)
                spriteRenderer.sharedMaterial = _material;
            spriteRenderer.flipX = random.NextDouble() < 0.5;
            spriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            spriteRenderer.receiveShadows = false;

            float width = Mathf.Lerp(layer.Width.x, layer.Width.y, (float)random.NextDouble());
            float spriteWidth = Mathf.Max(0.01f, spriteRenderer.sprite.bounds.size.x);
            cloudObject.transform.localScale = new Vector3(1f, 0.8f + 0.3f * (float)random.NextDouble(), 1f) * (width / spriteWidth);

            Vector3 view = _camera.transform.position;
            // Stratified heights keep a layer evenly spread over its band instead of clumping.
            float y = view.y + ((index + (float)random.NextDouble()) / layer.Count - 0.5f) * band.y;
            float x = view.x + ((float)random.NextDouble() - 0.5f) * band.x;
            cloudObject.transform.position = new Vector3(x, y, layer.Depth);

            var cloud = new Cloud(cloudObject.transform, spriteRenderer, layer, band) { X = x };
            spriteRenderer.color = LayerColor(layer);
            return cloud;
        }

        /// <summary>Width and height of the area a layer wraps in: the camera view at the layer's depth plus headroom.</summary>
        private Vector2 Band(CloudLayer layer)
        {
            float distance = Mathf.Max(1f, layer.Depth - _camera.transform.position.z);
            float viewHeight = 2f * distance * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            // Portrait phones and tablets; the clamp also keeps the density right in the landscape batch-mode window.
            float viewWidth = viewHeight * Mathf.Clamp(_camera.aspect, 0.5f, 0.8f);
            return new Vector2(viewWidth * _bandHeadroom + layer.Width.y, viewHeight * _bandHeadroom + layer.Width.y);
        }

        private Color LayerColor(CloudLayer layer)
        {
            Color color = Color.Lerp(_nearColor, _horizonColor, layer.Haze);
            color.a = _opacity * layer.Opacity;
            return color;
        }

        private static float Wrap(float value, float center, float size) =>
            center + Mathf.Repeat(value - center + size * 0.5f, size) - size * 0.5f;

        private static CloudLayer[] DefaultLayers() => new[]
        {
            new CloudLayer(260f, 40, new Vector2(18f, 32f), 0.5f, 0.45f, 0.85f),
            new CloudLayer(110f, 26, new Vector2(8f, 15f), 0.35f, 0.3f, 0.9f),
            new CloudLayer(30f, 12, new Vector2(5f, 8f), 0.25f, 0.08f, 0.95f)
        };

        private sealed class Cloud
        {
            public readonly Transform Transform;
            public readonly SpriteRenderer Renderer;
            public readonly CloudLayer Layer;
            public readonly Vector2 Band;
            public float X;

            public Cloud(Transform transform, SpriteRenderer renderer, CloudLayer layer, Vector2 band)
            {
                Transform = transform;
                Renderer = renderer;
                Layer = layer;
                Band = band;
            }
        }
    }

    /// <summary>One depth layer of <see cref="CloudField"/>.</summary>
    [Serializable]
    public sealed class CloudLayer
    {
        [Tooltip("World Z of the layer (the tower axis is at 0, the camera looks along +Z).")]
        [SerializeField] private float _depth = 100f;
        [SerializeField, Min(0)] private int _count = 6;
        [Tooltip("Cloud width range in meters.")]
        [SerializeField] private Vector2 _width = new(10f, 20f);
        [Tooltip("Sideways drift in meters per second.")]
        [SerializeField] private float _drift = 0.3f;
        [Tooltip("0 = cloud colour, 1 = horizon colour (aerial perspective).")]
        [SerializeField, Range(0f, 1f)] private float _haze;
        [SerializeField, Range(0f, 1f)] private float _opacity = 1f;

        public CloudLayer(float depth, int count, Vector2 width, float drift, float haze, float opacity)
        {
            _depth = depth;
            _count = count;
            _width = width;
            _drift = drift;
            _haze = haze;
            _opacity = opacity;
        }

        public float Depth => _depth;
        public int Count => _count;
        public Vector2 Width => _width;
        public float Drift => _drift;
        public float Haze => _haze;
        public float Opacity => _opacity;
    }
}
