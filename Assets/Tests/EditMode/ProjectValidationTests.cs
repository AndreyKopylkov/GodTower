using System.Linq;
using GodTower.Editor;
using GodTower.Levels;
using NUnit.Framework;
using UnityEditor;

namespace GodTower.Tests.EditMode
{
    /// <summary>Generated content checks: asset hygiene (U-68) and level authoring (U-60/U-61).</summary>
    public sealed class ProjectValidationTests
    {
        [Test]
        public void Project_HasNoMissingReferences_AndNoDemoContent()
        {
            var issues = ProjectValidator.Run();
            Assert.That(issues, Is.Empty, string.Join("\n", issues));
        }

        [Test]
        public void Levels_HaveDistinctSkyPresets_DayToDusk()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(ProjectPaths.LevelCatalog);
            Assert.That(catalog.Count, Is.EqualTo(5));
            Assert.That(catalog.Levels.Select(level => level.Sky), Is.All.Not.Null);
            Assert.That(catalog.Levels.Select(level => level.Sky.DisplayName),
                Is.EqualTo(new[] { "Day", "Bright afternoon", "Golden hour", "Sunset", "Dusk" }));
        }

        [Test]
        public void Levels_GetHarderInOrder()
        {
            var levels = AssetDatabase.LoadAssetAtPath<LevelCatalog>(ProjectPaths.LevelCatalog).Levels;
            for (int i = 1; i < levels.Count; i++)
            {
                Assert.That(levels[i].Number, Is.EqualTo(i + 1));
                Assert.That(levels[i].TowerHeight, Is.GreaterThan(levels[i - 1].TowerHeight));
                Assert.That(levels[i].VillainInterval, Is.LessThan(levels[i - 1].VillainInterval));
                Assert.That(levels[i].TelegraphDuration, Is.LessThan(levels[i - 1].TelegraphDuration));
                Assert.That(levels[i].Villains.Count, Is.GreaterThanOrEqualTo(levels[i - 1].Villains.Count));
            }
        }
    }
}
