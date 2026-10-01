using System;
using System.IO;
using System.Linq;
using GodTower.Events;
using GodTower.Levels;
using UnityEditor;
using UnityEngine;

namespace GodTower.Editor
{
    /// <summary>
    /// Writes every level's seeded <see cref="EventTimeline"/> (villain impacts and lanes, hero boosts) to
    /// <c>Tools/Video/level_timelines.json</c>, so the PC-side recording script can dodge villains on a real device.
    /// Same factory as the game (<see cref="EventDirector.BuildTimeline"/>), so the file always matches the build.
    /// </summary>
    public static class LevelTimelineExporter
    {
        public const string OutputPath = "Tools/Video/level_timelines.json";

        public static void Export()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(ProjectPaths.LevelCatalog);
            var events = AssetDatabase.LoadAssetAtPath<EventsConfig>(ProjectPaths.EventsConfig);
            if (catalog == null || events == null)
                throw new InvalidOperationException("Level catalog or events config missing; run SetupProject first.");

            var file = new TimelineFile
            {
                levels = catalog.Levels.Select(level =>
                {
                    EventTimeline timeline = EventDirector.BuildTimeline(level, events);
                    return new LevelEntry
                    {
                        number = level.Number,
                        timeLimit = level.TimeLimit,
                        towerHeight = level.TowerHeight,
                        telegraph = level.TelegraphDuration,
                        strikes = timeline.Strikes.Select(strike => new StrikeEntry
                        {
                            kind = strike.Kind.ToString(),
                            telegraphTime = strike.TelegraphTime,
                            impact = strike.ImpactTime,
                            laneMask = strike.LaneMask
                        }).ToArray(),
                        boosts = timeline.Boosts.Select(boost => new BoostEntry
                        {
                            kind = boost.Kind.ToString(),
                            time = boost.Time,
                            duration = boost.Duration
                        }).ToArray()
                    };
                }).ToArray()
            };

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            File.WriteAllText(OutputPath, JsonUtility.ToJson(file, prettyPrint: true) + "\n");
            Debug.Log($"[LevelTimelineExporter] Wrote {OutputPath} ({file.levels.Length} levels).");
        }

        // JsonUtility field names = JSON keys (camelCase on purpose).
#pragma warning disable IDE1006
        [Serializable]
        private sealed class TimelineFile
        {
            public LevelEntry[] levels;
        }

        [Serializable]
        private sealed class LevelEntry
        {
            public int number;
            public float timeLimit;
            public float towerHeight;
            public float telegraph;
            public StrikeEntry[] strikes;
            public BoostEntry[] boosts;
        }

        [Serializable]
        private sealed class StrikeEntry
        {
            public string kind;
            public float telegraphTime;
            public float impact;
            public int laneMask;
        }

        [Serializable]
        private sealed class BoostEntry
        {
            public string kind;
            public float time;
            public float duration;
        }
#pragma warning restore IDE1006
    }
}
