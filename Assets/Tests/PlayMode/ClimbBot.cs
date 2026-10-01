using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using GodTower.Events;
using GodTower.Gameplay;
using GodTower.Levels;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GodTower.Tests.PlayMode
{
    /// <summary>
    /// Simulated player for autoplay tests: holds a virtual mouse to climb and swipes away from telegraphed lanes
    /// (it reads the telegraphs from <see cref="EventDirector"/> — it is a test). Drive it with <see cref="StepAsync"/>
    /// once per frame.
    /// </summary>
    public sealed class ClimbBot
    {
        private readonly InputTestFixture _input;
        private readonly Mouse _mouse;
        private readonly LevelRunner _runner;
        private readonly List<VillainStrike> _telegraphed = new();

        public ClimbBot(InputTestFixture input, Mouse mouse, LevelRunner runner, EventDirector director)
        {
            _input = input;
            _mouse = mouse;
            _runner = runner;
            director.Telegraphed += _telegraphed.Add;
        }

        public void Hold() => PressAt(0.5f);

        public void Release() => _input.Release(_mouse.leftButton);

        /// <summary>Swipes towards the nearest safe lane if needed; call every frame while holding.</summary>
        public async UniTask StepAsync()
        {
            ClimberMotor climber = _runner.Climber;
            int target = SafeLane(climber, _telegraphed.Where(strike => strike.ImpactTime > _runner.Elapsed));
            if (target != climber.Lanes.Index && climber.State is ClimberState.Idle or ClimberState.Climb)
                await SwipeAsync(Math.Sign(target - climber.Lanes.Index));
        }

        /// <summary>A quick horizontal drag, then a new press at the centre (re-centring would read as a swipe back).</summary>
        public async UniTask SwipeAsync(int direction)
        {
            _input.Set(_mouse.position, new Vector2(Screen.width * (0.5f + 0.3f * direction), Screen.height * 0.5f));
            await UniTask.Yield();
            await UniTask.Yield();
            Release();
            await UniTask.Yield();
            PressAt(0.5f);
            await UniTask.Yield();
        }

        /// <summary>Nearest lane not hit by any pending strike (current lane if it is safe).</summary>
        public static int SafeLane(ClimberMotor climber, IEnumerable<VillainStrike> pending)
        {
            int mask = pending.Aggregate(0, (current, strike) => current | strike.LaneMask);
            int current = climber.Lanes.Index;
            return Enumerable.Range(0, climber.Lanes.Count)
                .Where(lane => (mask & (1 << lane)) == 0)
                .OrderBy(lane => Math.Abs(lane - current))
                .DefaultIfEmpty(current)
                .First();
        }

        private void PressAt(float normalizedX)
        {
            _input.Set(_mouse.position, new Vector2(Screen.width * normalizedX, Screen.height * 0.5f));
            _input.Press(_mouse.leftButton);
        }
    }
}
