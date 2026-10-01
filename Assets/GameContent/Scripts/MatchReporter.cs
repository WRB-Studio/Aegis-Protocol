using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

// One recording per loaded session: saves can roll back to the start of a wave.
public sealed class MatchReporter : IDisposable
{
    public static MatchReporter Current { get; private set; }
    public static string LastReportDirectory { get; private set; }
    public MatchReport Report { get; private set; }
    StreamWriter writer;
    MatchWaveReport activeWave;
    float nextSnapshot, nextFlush;
    int sequence;
    bool finished, capturingLog;
    readonly string directory;

    public MatchReporter(string rootDirectory, bool resumed)
    {
        Current?.Dispose();
        Current = this;
        Report = new MatchReport
        {
            id = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N"),
            startedUtc = DateTime.UtcNow.ToString("O"),
            resumed = resumed,
            gameVersion = Application.version,
            unityVersion = Application.unityVersion,
            platform = Application.platform.ToString(),
            initial = CaptureState()
        };
        directory = Path.Combine(rootDirectory, Report.id);
        try
        {
            Directory.CreateDirectory(directory);
            writer = new StreamWriter(Path.Combine(directory, "events.jsonl"), false, new UTF8Encoding(false));
            LastReportDirectory = directory;
        }
        catch (Exception exception) when (IsFileError(exception))
        {
            Debug.LogWarning("Match reporter could not open recording: " + exception.Message);
        }
        Application.logMessageReceived += CaptureLog;
        Record("session_started", detail: resumed ? "Loaded save; previous events unavailable; wave may restart." : "New match");
        Snapshot();
        Flush();
        Debug.Log("Match report: " + directory);
    }

    public static void Event(string kind, string actor = "", string target = "", float value = 0,
        float remaining = 0, string detail = "", Vector3 position = default)
    {
        Current?.Record(kind, actor, target, value, remaining, detail, position);
    }

    void Record(string kind, string actor = "", string target = "", float value = 0,
        float remaining = 0, string detail = "", Vector3 position = default, MatchState state = null)
    {
        if (finished || writer == null) return;
        var entry = new MatchEvent
        {
            sequence = ++sequence, utc = DateTime.UtcNow.ToString("O"),
            playTime = Stats.Instance ? Stats.Instance.playTime : 0,
            wave = activeWave != null ? activeWave.wave : EnemySpawner.Instance ? EnemySpawner.Instance.DisplayWave : 0,
            kind = kind, actor = actor, target = target, value = value, remaining = remaining,
            detail = detail, position = position, state = state
        };
        if (kind == "module_built" || kind == "module_repaired" || kind == "module_destroyed" ||
            kind == "upgrade_bought" || kind == "shield_depleted" || kind == "shield_activated" ||
            kind == "self_destruct" || kind == "app_paused" || kind == "app_resumed")
            Report.timeline.Add(entry);
        try { writer.WriteLine(JsonUtility.ToJson(entry)); }
        catch (Exception exception) when (IsFileError(exception)) { DisableWriter(exception); }
    }

    public void Tick()
    {
        if (finished) return;
        float time = Stats.Instance ? Stats.Instance.playTime : 0;
        if (time >= nextSnapshot)
        {
            Snapshot();
            nextSnapshot = time + 5f;
        }
        if (Time.realtimeSinceStartup >= nextFlush)
        {
            Flush();
            nextFlush = Time.realtimeSinceStartup + 5f;
        }
    }

    public void BeginWave(int wave, bool authored)
    {
        if (finished) return;
        activeWave = new MatchWaveReport { wave = wave, initial = CaptureState() };
        Report.waves.Add(activeWave);
        Record("wave_started", detail: authored ? "authored" : "procedural", state: activeWave.initial);
        Flush();
    }

    public void CompleteWave()
    {
        if (finished || activeWave == null) return;
        activeWave.completed = true;
        activeWave.final = CaptureState();
        Record("wave_completed", state: activeWave.final);
        activeWave = null;
        Checkpoint();
    }

    public void Checkpoint()
    {
        if (finished) return;
        Snapshot();
        Flush();
        WriteReports("in_progress");
    }

    public void Finish(string outcome)
    {
        if (finished) return;
        if (activeWave != null) activeWave.final = CaptureState();
        Record("session_ended", detail: outcome);
        Snapshot();
        Flush();
        WriteReports(outcome);
        finished = true;
        Application.logMessageReceived -= CaptureLog;
        CloseWriter();
    }

    public void Dispose()
    {
        Finish(GameManager.gameOver ? "game_over" : "session_closed");
        if (Current == this) Current = null;
    }

    void Snapshot() => Record("snapshot", state: CaptureState());

    void CaptureLog(string message, string stackTrace, LogType type)
    {
        if (capturingLog || finished || type == LogType.Log) return;
        capturingLog = true;
        try
        {
            Report.warnings += type == LogType.Warning ? 1 : 0;
            Report.errors += type == LogType.Warning ? 0 : 1;
            Record("runtime_log", actor: type.ToString(), detail: message + "\n" + stackTrace);
        }
        finally { capturingLog = false; }
    }

    void WriteReports(string outcome)
    {
        Report.outcome = outcome;
        Report.endedUtc = DateTime.UtcNow.ToString("O");
        Report.final = CaptureState();
        Report.events = sequence;
        // Logging is best effort and must never prevent gameplay or saving.
        try
        {
            File.WriteAllText(Path.Combine(directory, "summary.json"), JsonUtility.ToJson(Report, true), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(directory, "analysis.md"), BuildAnalysis(Report), new UTF8Encoding(false));
        }
        catch (Exception exception) when (IsFileError(exception))
        {
            Debug.LogWarning("Match reporter could not write summary: " + exception.Message);
        }
    }

    void Flush()
    {
        if (writer == null) return;
        try { writer.Flush(); }
        catch (Exception exception) when (IsFileError(exception)) { DisableWriter(exception); }
    }

    void DisableWriter(Exception exception)
    {
        CloseWriter();
        Debug.LogWarning("Match reporter stopped recording: " + exception.Message);
    }

    void CloseWriter()
    {
        var old = writer;
        writer = null;
        try { old?.Dispose(); }
        catch (Exception exception) when (IsFileError(exception)) { }
    }

    static bool IsFileError(Exception exception) => exception is IOException || exception is UnauthorizedAccessException;

    public static MatchState CaptureState()
    {
        var state = new MatchState
        {
            stats = Stats.Instance ? Stats.Instance.GetStatsData() : new StatsData(),
            materials = ResourceManager.Instance ? ResourceManager.Instance.curMaterials : 0,
            activeEnemies = EnemySpawner.Instance ? EnemySpawner.Instance.instantiatedEnemies.Count : 0,
            activeDrones = DroneManager.Instance ? DroneManager.Instance.allDrones.Count : 0,
            timeScale = Time.timeScale,
            shield = Shield.Instance ? Shield.Instance.currentShieldPoints : 0,
            shieldMax = Shield.Instance ? Shield.Instance.maxShieldPoints : 0,
            shieldActive = Shield.Instance && Shield.Instance.shieldIsActive,
            shieldRecharge = Shield.Instance ? Shield.Instance.rechargeCountdown : 0
        };
        foreach (var module in StationModule.allModules)
            if (module) state.modules.Add(new MatchModuleState
            {
                type = module.moduleType.ToString(), built = module.isBuilt,
                hp = module.currentHP, maxHP = module.maxHP
            });
        foreach (var upgrade in UpgradeAttribute.allUpgradeAttributes)
            state.upgrades.Add(new MatchUpgradeState
            {
                name = upgrade.upgradeName.ToString(), owner = upgrade.ownerModule ? upgrade.ownerModule.moduleType.ToString() : "",
                level = upgrade.level, value = upgrade.currentValue, cost = upgrade.cost
            });
        return state;
    }

    public static string BuildAnalysis(MatchReport report)
    {
        var start = report.initial.stats;
        var end = report.final.stats;
        var sb = new StringBuilder();
        sb.AppendLine("# Matchanalyse");
        sb.AppendLine($"\nSession: {report.id}\n\nStart (UTC): {report.startedUtc} | Stand (UTC): {report.endedUtc}");
        sb.AppendLine($"\nStatus: {report.outcome} | Version: {report.gameVersion} | Unity: {report.unityVersion} | Plattform: {report.platform}");
        sb.AppendLine(report.resumed
            ? "\nFortsetzung eines Spielstands. Alle folgenden Differenzen gelten nur fuer diese Session. Fruehere Ereignisse fehlen; die gespeicherte Welle kann neu beginnen."
            : "\nAufzeichnung eines neuen Matches.");
        sb.AppendLine($"\nAufgezeichnete Spielzeit: {Math.Max(0, end.playTime - start.playTime):F1} s | Gesamte Spielzeit: {end.playTime:F1} s");
        sb.AppendLine($"\nAbgeschlossene Wellen: {end.wavesCompleted - start.wavesCompleted} | Aktive Gegner am Ende: {report.final.activeEnemies}");
        sb.AppendLine($"\nModulschaden (angefordert, inkl. Overkill): {end.modulesDamageTaken - start.modulesDamageTaken} | Schildschaden (angefordert): {end.shieldDamageTaken - start.shieldDamageTaken}");
        sb.AppendLine($"\nModulverluste: {end.modulesDestroyed - start.modulesDestroyed} | Drohnenverluste: {end.dronesDestroyed - start.dronesDestroyed}");
        sb.AppendLine($"\nMaterial: {report.initial.materials} -> {report.final.materials} | Gesammelt: {end.resourcesCollectedManually + end.resourcesCollectedAutomatically - start.resourcesCollectedManually - start.resourcesCollectedAutomatically} | Baukosten: {end.modulesCost - start.modulesCost} | Upgradekosten: {end.totalUpgradeCosts - start.totalUpgradeCosts}");
        sb.AppendLine("\n## Waffenleistung");
        AppendAccuracy(sb, "Turm", end.towerProjectilesFired - start.towerProjectilesFired, end.towerProjectilesHit - start.towerProjectilesHit);
        AppendAccuracy(sb, "Drohnen", end.droneProjectilesFired - start.droneProjectilesFired, end.droneProjectilesHit - start.droneProjectilesHit);
        AppendAccuracy(sb, "Reflexion", end.deflectedProjectilesFired - start.deflectedProjectilesFired, end.deflectedProjectilesHit - start.deflectedProjectilesHit);
        sb.AppendLine("\nTrefferquoten enthalten noch fliegende Projektile. Bei einer Fortsetzung koennen Treffer aus vor der Session abgefeuerten Projektilen die Quote verzerren.");
        sb.AppendLine("\n## Gegner nach Typ und Entfernungursache");
        sb.AppendLine("\nStationskollisionen sind Durchbrueche, keine erfolgreichen Abschuesse.");
        foreach (var kill in end.kills)
        {
            int before = 0;
            foreach (var previous in start.kills)
                if (previous.enemyType == kill.enemyType && previous.deadBy == kill.deadBy) before += previous.count;
            if (kill.count > before) sb.AppendLine($"- {kill.enemyType} / {kill.deadBy}: {kill.count - before}");
        }
        sb.AppendLine("\n## Wellenverlauf");
        sb.AppendLine("\n| Welle | Status | Dauer (s) | Spawns | Modulschaden | Verluste | Material Start/Ende |");
        sb.AppendLine("|---|---|---:|---:|---:|---:|---|");
        MatchWaveReport worst = null;
        int worstDamage = 0;
        foreach (var wave in report.waves)
        {
            var final = wave.final ?? report.final;
            int damage = final.stats.modulesDamageTaken - wave.initial.stats.modulesDamageTaken;
            sb.AppendLine(FormattableString.Invariant($"| {wave.wave} | {(wave.completed ? "abgeschlossen" : "unvollstaendig")} | {final.stats.playTime - wave.initial.stats.playTime:F1} | {final.stats.enemiesSpawned - wave.initial.stats.enemiesSpawned} | {damage} | {final.stats.modulesDestroyed - wave.initial.stats.modulesDestroyed} | {wave.initial.materials}/{final.materials} |"));
            if (damage > worstDamage) { worst = wave; worstDamage = damage; }
        }
        sb.AppendLine("\n## Analysehinweise");
        if (worst != null) sb.AppendLine($"- Hoechster Modulschaden in Welle {worst.wave}: {worstDamage}. Schadenquellen und Kaufzeitpunkte im Ereignisprotokoll pruefen.");
        if (end.modulesDamageTaken == start.modulesDamageTaken && end.enemiesSpawned > start.enemiesSpawned)
            sb.AppendLine("- Keine Modulschaeden in der aufgezeichneten Session: Die Station blieb gegen die aufgezeichneten Angriffe geschuetzt.");
        if (report.final.materials > 0 && (report.outcome == "game_over" || report.outcome == "self_destruct"))
            sb.AppendLine($"- {report.final.materials} Material blieb am Ende uebrig. Ob Reparatur oder Ausbau moeglich war, anhand der damaligen Kosten und Modulzustaende pruefen.");
        sb.AppendLine($"- Laufzeitwarnungen: {report.warnings}; Fehler/Exceptions: {report.errors}. Details und Stacktraces stehen in events.jsonl.");
        sb.AppendLine("\nDiese Hinweise beschreiben Messwerte und moegliche Untersuchungen, keine bewiesenen Ursachen oder Balanceurteile.");
        sb.AppendLine("\n## Entscheidungen und kritische Ereignisse");
        sb.AppendLine("\n| Spielzeit (s) | Welle | Ereignis | Akteur/Ziel | Wert | Restwert/Level | Details |");
        sb.AppendLine("|---:|---:|---|---|---:|---:|---|");
        foreach (var entry in report.timeline)
            sb.AppendLine(FormattableString.Invariant($"| {entry.playTime:F1} | {entry.wave} | {entry.kind} | {entry.actor}/{entry.target} | {entry.value} | {entry.remaining} | {entry.detail} |"));
        sb.AppendLine("\n## Detailprotokoll");
        sb.AppendLine("\nevents.jsonl enthaelt ein JSON-Objekt pro Ereignis: Sequenz, UTC, Spielzeit, Welle, Akteur, Ziel, Wert, Restwert, Position und Details. Snapshots enthalten Statistiken, Module, Upgrades, Material und Schildzustand. summary.json enthaelt Anfang, Ende und Wellenzustaende.");
        return sb.ToString();
    }

    static void AppendAccuracy(StringBuilder sb, string name, int fired, int hits)
    {
        sb.AppendLine(fired > 0
            ? FormattableString.Invariant($"\n- {name}: {hits}/{fired} Treffer ({100f * hits / fired:F1} %)")
            : $"\n- {name}: {hits} Treffer, keine Schuesse aufgezeichnet; Quote nicht bestimmbar.");
    }
}

[Serializable]
public class MatchReport
{
    public int schemaVersion = 1;
    public string id, startedUtc, endedUtc, gameVersion, unityVersion, platform, outcome;
    public bool resumed;
    public int events, warnings, errors;
    public MatchState initial, final;
    public List<MatchWaveReport> waves = new();
    public List<MatchEvent> timeline = new();
}

[Serializable]
public class MatchWaveReport
{
    public int wave;
    public bool completed;
    public MatchState initial, final;
}

[Serializable]
public class MatchState
{
    public StatsData stats;
    public int materials, activeEnemies, activeDrones;
    public float timeScale, shield, shieldMax, shieldRecharge;
    public bool shieldActive;
    public List<MatchModuleState> modules = new();
    public List<MatchUpgradeState> upgrades = new();
}

[Serializable]
public class MatchModuleState
{
    public string type;
    public bool built;
    public int hp, maxHP;
}

[Serializable]
public class MatchUpgradeState
{
    public string name, owner;
    public int level;
    public float value, cost;
}

[Serializable]
public class MatchEvent
{
    public int sequence, wave;
    public string utc, kind, actor, target, detail;
    public float playTime, value, remaining;
    public Vector3 position;
    public MatchState state;
}
