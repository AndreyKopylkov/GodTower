using System;

namespace GodTower.Levels
{
    /// <summary>
    /// Session-wide choice of the level to play (0-based index into the <see cref="LevelCatalog"/>).
    /// Written by the menu, read by the Game scene. Defaults to the first level so the Game scene can start on its own.
    /// </summary>
    public sealed class SelectedLevel
    {
        public int Index { get; private set; }

        public void Select(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Level index must not be negative.");

            Index = index;
        }
    }
}
