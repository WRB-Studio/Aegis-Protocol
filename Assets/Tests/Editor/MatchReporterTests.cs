using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MatchReporterTests
{
    string root;
    GameObject statsObject;
    Stats previousStats;
    MatchReporter reporter;

    [SetUp]
    public void SetUp()
    {
        root = Path.Combine(Path.GetTempPath(), "Aegis-MatchReporter-" + Guid.NewGuid().ToString("N"));
        previousStats = Stats.Instance;
        statsObject = new GameObject("Reporter test stats");
        Stats.Instance = statsObject.AddComponent<Stats>();
    }

    [TearDown]
    public void TearDown()
    {
        reporter?.Dispose();
        UnityEngine.Object.DestroyImmediate(statsObject);
        Stats.Instance = previousStats;
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }

    [Test]
    public void RecordingPreservesChronologyStateAndFinalCollisionCounts()
    {
        reporter = new MatchReporter(root, false);
        reporter.BeginWave(1, true);
        Stats.Instance.enemiesSpawned = 2;
        Stats.Instance.playTime = 12;
        MatchReporter.Event("module_damage", "Normal:123", "Core", 3, 7, "detail\nwith newline", new Vector3(1, 2));
        Stats.Instance.modulesDamageTaken = 3;
        Stats.Instance.RegisterKill(Enemy.eEnemyType.Normal, Stats.eDeadBy.stationCollision);
        Stats.Instance.enemyProjectilesHit = 1;
        reporter.Finish("game_over");
        string directory = MatchReporter.LastReportDirectory;
        var entries = File.ReadAllLines(Path.Combine(directory, "events.jsonl"))
            .Select(JsonUtility.FromJson<MatchEvent>).ToArray();
        Assert.That(entries.Select(e => e.sequence), Is.EqualTo(Enumerable.Range(1, entries.Length)));
        var damage = entries.Single(e => e.kind == "module_damage");
        Assert.That(damage.wave, Is.EqualTo(1));
        Assert.That(damage.playTime, Is.EqualTo(12));
        Assert.That(damage.detail, Is.EqualTo("detail\nwith newline"));
        Assert.That(damage.position.y, Is.EqualTo(2));
        var summary = JsonUtility.FromJson<MatchReport>(File.ReadAllText(Path.Combine(directory, "summary.json")));
        Assert.That(summary.final.stats.enemyProjectilesHit, Is.EqualTo(1));
        Assert.That(summary.waves.Single().completed, Is.False);
        Assert.That(summary.waves.Single().final.stats.modulesDamageTaken, Is.EqualTo(3));
        Assert.That(File.ReadAllText(Path.Combine(directory, "analysis.md")), Does.Contain("Durchbrueche"));
        reporter.Finish("restarted");
        MatchReporter.Event("after_end");
        Assert.That(File.ReadAllLines(Path.Combine(directory, "events.jsonl")), Has.Length.EqualTo(entries.Length));
        Assert.That(reporter.Report.outcome, Is.EqualTo("game_over"));
    }

    [Test]
    public void ResumedAnalysisUsesSessionDifferencesAndNeverDividesByZero()
    {
        Stats.Instance.playTime = 120;
        Stats.Instance.modulesDamageTaken = 20;
        Stats.Instance.towerProjectilesFired = 100;
        Stats.Instance.towerProjectilesHit = 75;
        reporter = new MatchReporter(root, true);
        Stats.Instance.playTime = 125;
        Stats.Instance.modulesDamageTaken = 22;
        reporter.Finish("session_closed");
        string analysis = MatchReporter.BuildAnalysis(reporter.Report);
        Assert.That(analysis, Does.Contain("Fortsetzung"));
        Assert.That(analysis, Does.Contain("inkl. Overkill): 2"));
        Assert.That(analysis, Does.Contain("Quote nicht bestimmbar"));
        Assert.That(analysis, Does.Not.Contain("NaN"));
        Assert.That(analysis, Does.Not.Contain("Infinity"));
        Assert.That(reporter.Report.initial.stats.modulesDamageTaken, Is.EqualTo(20));
    }

    [Test]
    public void WaveCheckpointAndCriticalTimelineRemainAvailableBeforeFinish()
    {
        reporter = new MatchReporter(root, false);
        reporter.BeginWave(3, false);
        Stats.Instance.playTime = 10;
        Stats.Instance.wavesCompleted = 1;
        MatchReporter.Event("upgrade_bought", "AmmoFabricator", "Damage", 20, 2);
        reporter.CompleteWave();
        var summary = JsonUtility.FromJson<MatchReport>(File.ReadAllText(
            Path.Combine(MatchReporter.LastReportDirectory, "summary.json")));
        Assert.That(summary.outcome, Is.EqualTo("in_progress"));
        Assert.That(summary.waves.Single().completed, Is.True);
        Assert.That(summary.timeline.Single().target, Is.EqualTo("Damage"));
        Assert.That(summary.waves.Single().final.stats.playTime, Is.EqualTo(10));
        LogAssert.Expect(LogType.Warning, "Reporter test warning");
        Debug.LogWarning("Reporter test warning");
        reporter.Finish("app_quit");
        Assert.That(reporter.Report.warnings, Is.EqualTo(1));
        Assert.That(File.ReadAllText(Path.Combine(MatchReporter.LastReportDirectory, "events.jsonl")),
            Does.Contain("Reporter test warning"));
    }

    [Test]
    public void ReplayCreatesIndependentRecordingAndDisposalReleasesCurrent()
    {
        reporter = new MatchReporter(root, false);
        string first = MatchReporter.LastReportDirectory;
        MatchReporter.Event("module_built", target: "Shield", value: 10);
        reporter.Finish("restarted");
        reporter = new MatchReporter(root, false);
        Assert.That(MatchReporter.LastReportDirectory, Is.Not.EqualTo(first));
        Assert.That(reporter.Report.timeline, Is.Empty);
        Assert.That(File.Exists(Path.Combine(first, "analysis.md")), Is.True);
        reporter.Dispose();
        Assert.That(MatchReporter.Current, Is.Null);
    }
}
