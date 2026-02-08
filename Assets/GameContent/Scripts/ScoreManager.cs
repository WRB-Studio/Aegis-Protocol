using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("Points per stat")]
    [SerializeField] int waves = 150;
    [SerializeField] int kills = 10;
    [SerializeField] int upgradesBought = 30;
    [SerializeField] int resourcesTotal = 1;
    [SerializeField] int modulesLost = 50;

    [Header("Time (logarithmic)")]
    [SerializeField] int logTime = 100; // log10(seconds+1) * logTime

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public ScoreBreakdown GetBreakdown()
    {
        var s = Stats.Instance;
        if (!s) return default;

        int killsCount = Mathf.Max(0, s.GetTotalKills());
        int upgradesCount = Mathf.Max(0, s.boughtUpgrades);
        int wavesCount = Mathf.Max(0, s.wavesCompleted);
        int resTotal = Mathf.Max(0, s.resourcesCollectedManually + s.resourcesCollectedAutomatically);
        int modulesLostCount = Mathf.Max(0, s.modulesDestroyed);

        int timeScore = Mathf.RoundToInt(Mathf.Log10(Mathf.Max(0f, s.playTime) + 1f) * logTime);

        int wavesScore = wavesCount * waves;
        int killsScore = killsCount * kills;
        int upgradesScore = upgradesCount * upgradesBought;
        int resourcesScore = resTotal * resourcesTotal;
        int modulesPenalty = modulesLostCount * modulesLost;

        int total = wavesScore + killsScore + upgradesScore + resourcesScore + timeScore - modulesPenalty;
        total = Mathf.Max(0, total);

        return new ScoreBreakdown
        {
            playTimeSeconds = s.playTime,
            timeScore = timeScore,

            wavesCount = wavesCount,
            wavesScore = wavesScore,

            killsCount = killsCount,
            killsScore = killsScore,

            upgradesCount = upgradesCount,
            upgradesScore = upgradesScore,

            resourcesTotal = resTotal,
            resourcesScore = resourcesScore,

            modulesLostCount = modulesLostCount,
            modulesPenalty = modulesPenalty,

            totalScore = total
        };
    }

    [Serializable]
    public struct ScoreBreakdown
    {
        public float playTimeSeconds;
        public int timeScore;

        public int wavesCount;
        public int wavesScore;

        public int killsCount;
        public int killsScore;

        public int upgradesCount;
        public int upgradesScore;

        public int resourcesTotal;
        public int resourcesScore;

        public int modulesLostCount;
        public int modulesPenalty; // positive number, subtract in UI

        public int totalScore;
    }
}
