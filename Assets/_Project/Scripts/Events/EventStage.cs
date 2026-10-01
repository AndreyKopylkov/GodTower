using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GodTower.Gameplay;
using GodTower.Levels;
using PrimeTween;
using UnityEngine;
using VContainer;

namespace GodTower.Events
{
    /// <summary>
    /// World-space presentation of level events: lane telegraph markers, villain props flying/falling into lanes,
    /// impact effects and hero props carrying the climber. Motion is driven by the level clock, so it freezes on pause
    /// and stays in sync with the gameplay decision taken by <see cref="EventDirector"/>.
    /// </summary>
    public sealed class EventStage : MonoBehaviour
    {
        private const float CenterHeight = 1.1f;
        private const float AfterImpactDuration = 0.8f;

        [SerializeField] private Material _markerMaterial;
        [SerializeField] private Material _placeholderMaterial;
        [SerializeField] private Vector2 _markerSize = new(1.5f, 6f);

        [Tooltip("How far props travel during their approach (missile, axes: sideways; truck: drop height).")]
        [SerializeField, Min(1f)] private float _approachDistance = 14f;

        private readonly Dictionary<int, StrikeVisual> _strikes = new();
        private LevelRunner _runner;
        private ClimberView _climber;
        private CameraRig _camera;
        private float _hangRadius;

        [Inject]
        public void Construct(LevelRunner runner, ClimberView climber, CameraRig cameraRig, GameplayConfig gameplay)
        {
            _runner = runner;
            _climber = climber;
            _camera = cameraRig;
            _hangRadius = gameplay.Climber.HangRadius;
        }

        private float Clock => _runner.Elapsed;

        private float ClimberHeight => _runner.Climber.Height;

        /// <summary>Starts the telegraph of <paramref name="strike"/> and the approach of its prop.</summary>
        public void BeginStrike(VillainStrike strike, VillainEventConfig config)
        {
            var visual = new StrikeVisual(strike, config);
            _strikes[strike.Id] = visual;
            RunStrikeAsync(visual, destroyCancellationToken).Forget();
        }

        /// <summary>The villain landed: impact effect, shake, and the prop flies on (dodge) or bursts (hit).</summary>
        public void LandStrike(VillainStrike strike, bool hit)
        {
            if (!_strikes.TryGetValue(strike.Id, out StrikeVisual visual))
                return;

            visual.Landed = true;
            visual.Hit = hit;
            VillainEventConfig config = visual.Config;

            foreach (int lane in Lanes(strike))
            {
                Vector3 point = hit && lane == _runner.Climber.Lanes.Index ? _climber.Center : LanePoint(lane);
                if (hit || strike.Kind == VillainKind.Truck)
                    SpawnEffect(config.ImpactEffect, point, config.ImpactEffectScale, 3f);
            }

            _camera.Shake(hit ? config.ShakeOnHit : config.ShakeOnImpact);
        }

        /// <summary>Attaches the hero prop and effects to the climber for the boost.</summary>
        public void PlayBoost(HeroBoost boost, HeroEventConfig config) =>
            RunBoostAsync(boost, config, destroyCancellationToken).Forget();

        private async UniTaskVoid RunStrikeAsync(StrikeVisual visual, CancellationToken token)
        {
            VillainStrike strike = visual.Strike;
            var markers = new List<Transform>();
            foreach (int lane in Lanes(strike))
                markers.Add(CreateMarker(lane));

            var props = new List<(Transform Prop, int Lane, int Side)>();
            try
            {
                while (_runner.Outcome == null && (!visual.Landed || Clock < strike.ImpactTime + AfterImpactDuration))
                {
                    float toImpact = strike.ImpactTime - Clock;

                    for (int i = 0; i < markers.Count; i++)
                        markers[i].gameObject.SetActive(!visual.Landed);
                    if (!visual.Landed)
                        PlaceMarkers(markers, strike);

                    if (props.Count == 0 && toImpact <= visual.Config.ApproachDuration)
                        foreach (int lane in Lanes(strike))
                            props.Add((CreateProp(visual.Config), lane, SideOf(lane, strike.Id)));

                    bool burst = visual.Landed && visual.Hit && strike.Kind != VillainKind.Truck;
                    foreach ((Transform prop, int lane, int side) in props)
                    {
                        prop.gameObject.SetActive(!burst);
                        PlaceProp(prop, strike.Kind, LanePoint(lane), side, toImpact / visual.Config.ApproachDuration);
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            finally
            {
                foreach (Transform marker in markers)
                    DestroySafe(marker);
                foreach ((Transform prop, int _, int _) in props)
                    DestroySafe(prop);
                _strikes.Remove(strike.Id);
            }
        }

        /// <summary>
        /// Prop pose for a normalized time to impact <paramref name="u"/> (1 = approach start, 0 = impact, negative = after).
        /// </summary>
        private void PlaceProp(Transform prop, VillainKind kind, Vector3 lanePoint, int side, float u)
        {
            float travel = u * _approachDistance;
            switch (kind)
            {
                case VillainKind.Truck:
                    prop.position = lanePoint + Vector3.up * travel;
                    prop.rotation = Quaternion.Euler(u * 220f, side * 25f, u * 60f * side);
                    break;
                case VillainKind.Axes:
                    prop.position = lanePoint + Vector3.left * (side * travel) + Vector3.back * 0.4f;
                    prop.rotation = Quaternion.Euler(0f, 0f, u * 1440f * side);
                    break;
                default:
                    var direction = new Vector3(side, 0f, 0f);
                    prop.position = lanePoint - direction * travel;
                    prop.rotation = Quaternion.LookRotation(direction);
                    break;
            }
        }

        private async UniTaskVoid RunBoostAsync(HeroBoost boost, HeroEventConfig config, CancellationToken token)
        {
            Transform climber = _climber.transform;
            Transform prop = config.Prop != null
                ? Instantiate(config.Prop, climber).transform
                : CreatePlaceholder(PrimitiveType.Cube, climber, new Vector3(0.6f, 0.8f, 0.4f));
            prop.SetLocalPositionAndRotation(config.PropOffset, config.PropRotation);
            Vector3 scale = Vector3.one * config.PropScale;
            prop.localScale = Vector3.zero;
            _ = Tween.Scale(prop, scale, 0.25f, Ease.OutBack);

            Transform trail = SpawnEffect(config.TrailEffect, climber.TransformPoint(config.TrailOffset), config.TrailEffectScale, 0f);
            if (trail != null)
            {
                trail.SetParent(climber, true);
                trail.rotation = Quaternion.Euler(180f, 0f, 0f); // flames point down
            }

            SpawnEffect(config.StartEffect, _climber.Center, config.StartEffectScale, 4f);
            if (config.ZoomOutCamera)
                _camera.SetZoomedOut(true);

            try
            {
                await UniTask.WaitUntil(() => Clock >= boost.EndTime || _runner.Outcome != null, cancellationToken: token);
                await Tween.Scale(prop, Vector3.zero, 0.2f, Ease.InBack);
            }
            finally
            {
                DestroySafe(prop);
                DestroySafe(trail);
                if (config.ZoomOutCamera && _camera != null)
                    _camera.SetZoomedOut(false);
            }
        }

        private Transform CreateMarker(int lane)
        {
            Transform marker = CreatePlaceholder(PrimitiveType.Quad, transform, new Vector3(_markerSize.x, _markerSize.y, 1f));
            marker.name = $"Telegraph_Lane{lane}";
            marker.GetComponent<MeshRenderer>().sharedMaterial = _markerMaterial;
            marker.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Tween.Scale(marker, new Vector3(_markerSize.x * 1.25f, _markerSize.y, 1f), 0.2f, Ease.InOutSine, -1, CycleMode.Yoyo);
            return marker;
        }

        private void PlaceMarkers(List<Transform> markers, VillainStrike strike)
        {
            int index = 0;
            foreach (int lane in Lanes(strike))
            {
                Transform marker = markers[index++];
                float angle = _runner.Climber.Lanes.AngleOf(lane);
                marker.position = ClimberView.PositionOnColumn(angle, ClimberHeight + CenterHeight, _hangRadius - 0.2f); // behind the climber
                marker.rotation = ClimberView.FacingColumn(angle);
            }
        }

        private Transform CreateProp(VillainEventConfig config)
        {
            Transform prop = config.Prop != null
                ? Instantiate(config.Prop, transform).transform
                : CreatePlaceholder(PrimitiveType.Cube, transform, Vector3.one);
            prop.localScale *= config.PropScale;
            return prop;
        }

        private Transform CreatePlaceholder(PrimitiveType type, Transform parent, Vector3 scale)
        {
            GameObject primitive = GameObject.CreatePrimitive(type);
            Destroy(primitive.GetComponent<Collider>());
            primitive.transform.SetParent(parent, false);
            primitive.transform.localScale = scale;
            primitive.GetComponent<MeshRenderer>().sharedMaterial = _placeholderMaterial;
            return primitive.transform;
        }

        private Transform SpawnEffect(GameObject effect, Vector3 position, float scale, float lifetime)
        {
            if (effect == null)
                return null;

            GameObject instance = Instantiate(effect, position, Quaternion.identity, transform);
            instance.transform.localScale *= scale;
            if (lifetime > 0f)
                Destroy(instance, lifetime);
            return instance.transform;
        }

        private Vector3 LanePoint(int lane) =>
            _climber.LanePosition(_runner.Climber.Lanes.AngleOf(lane), ClimberHeight + CenterHeight);

        private IEnumerable<int> Lanes(VillainStrike strike)
        {
            for (int lane = 0; lane < _runner.Climber.Lanes.Count; lane++)
                if (strike.Hits(lane))
                    yield return lane;
        }

        /// <summary>Screen side a prop comes from: outer lanes from their own side, the centre alternates.</summary>
        private int SideOf(int lane, int strikeId)
        {
            int center = _runner.Climber.Lanes.CenterIndex;
            if (lane == center)
                return strikeId % 2 == 0 ? 1 : -1;
            return lane < center ? 1 : -1;
        }

        private static void DestroySafe(Component component)
        {
            if (component == null)
                return;

            Tween.StopAll(component.transform);
            Destroy(component.gameObject);
        }

        private sealed class StrikeVisual
        {
            public readonly VillainStrike Strike;
            public readonly VillainEventConfig Config;
            public bool Landed;
            public bool Hit;

            public StrikeVisual(VillainStrike strike, VillainEventConfig config)
            {
                Strike = strike;
                Config = config;
            }
        }
    }
}
