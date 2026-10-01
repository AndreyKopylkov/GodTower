using UnityEngine;

namespace GodTower.Gameplay
{
    /// <summary>
    /// Animator state names of <c>Hero.controller</c> (one state per hero clip) and the mapping from
    /// <see cref="ClimberState"/>. The editor builds the controller from the same names.
    /// </summary>
    public static class ClimberAnimation
    {
        public const string ClimbUp = "ClimbUp";
        public const string HangIdle = "HangIdle";
        public const string ShiftLeft = "ShiftLeft";
        public const string ShiftRight = "ShiftRight";
        public const string Hit = "Hit";
        public const string Fall = "Fall";
        public const string Carried = "Carried";
        public const string Win = "Win";
        public const string Lose = "Lose";

        /// <summary>All states, in the order of the hero clips.</summary>
        public static readonly string[] States = { ClimbUp, HangIdle, ShiftLeft, ShiftRight, Hit, Fall, Carried, Win, Lose };

        public static string StateFor(ClimberState state, int shiftDirection) => state switch
        {
            ClimberState.Climb => ClimbUp,
            ClimberState.Shift => shiftDirection < 0 ? ShiftLeft : ShiftRight,
            ClimberState.Hit => Hit,
            ClimberState.Fall => Fall,
            ClimberState.Carried => Carried,
            ClimberState.Win => Win,
            ClimberState.Lose => Lose,
            _ => HangIdle
        };

        public static int HashFor(ClimberState state, int shiftDirection) => Animator.StringToHash(StateFor(state, shiftDirection));
    }
}
