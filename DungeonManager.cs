using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System; // Для работы с DateTime
using UnityEngine.UI;

public class DungeonManager : MonoBehaviour
{
    public static DungeonManager Instance { get; private set; }

    [Header("UI References - Dungeon")]
    public TextMeshProUGUI threatLevelText;
    public TextMeshProUGUI expeditionStatusText;
    public TextMeshProUGUI dungeonMessageText;
    public TextMeshProUGUI totalExpeditionPowerDisplay; // Этот UI элемент, вероятно, нужно будет перенести, если он показывается в ExpeditionPreparationUI
    public TextMeshProUGUI currentEnemyPowerDisplay;

    [Header("Expedition Outcome UI")]
    public GameObject expeditionOutcomePanel;
    public TextMeshProUGUI outcomeTitleText;
    public TextMeshProUGUI rewardGoldText;
    public TextMeshProUGUI rewardXPText;
    public TextMeshProUGUI rewardFragmentsText;
    public TextMeshProUGUI soldiersLostText;
    public TextMeshProUGUI missionsUntilBossOutcomeText; // Текст "До босса осталось..." в окне наград
    public Button closeOutcomeButton;

    [Header("Threat Warning UI")]
    public GameObject threatWarningObject; // Твой текст-объект, который будет активен/неактивен
    public float threatWarningThreshold = 80f;

    [Header("Raid Outcome UI")]
    public GameObject raidOutcomePanel;
    public TextMeshProUGUI raidTitleText;
    public TextMeshProUGUI raidLostGoldText;
    public TextMeshProUGUI raidLostPeopleText;
    public Button closeRaidOutcomeButton;

    [Header("Boss Visuals Settings")]
    public GameObject bossButton; // Весь объект кнопки, чтобы его активировать/деактивировать
    public Image bossProgressOverlay; // Серое перекрытие (Image Type: Filled, Method: Vertical, Origin: Top)
    public TextMeshProUGUI bossProgressText; // Текст "4/10" на кнопке
    public Button bossActualButton; // Компонент Button на самой кнопке (для interactable)

    [Header("UI Managers")]
    public ExpeditionPreparationUI expeditionPreparationUI;
    public GameObject expeditionPreparationPanel;

    [Header("Dungeon Message Settings")]
    public float dungeonMessageDisplayDuration = 2.0f;
    private Coroutine currentDungeonMessageRoutine;

    [Header("Dungeon Settings")]
    public float baseThreatIncreasePerSecond = 0.5f;
    public float threatLevel = 0f;
    public float maxThreat = 100f;
    public float threatReductionPerExpedition = 20f;

    [Header("Negative Event Settings (100% Threat)")]
    public float goldLossPercentage = 0.1f;
    public int peopleLossAmount = 2;

    [Header("Enemy Power Settings")]
    public float enemyPowerPerThreatPercent = 5f;
    public long baseDungeonEnemyPower = 100;

    [Header("Expedition Outcome Multipliers")]
    public float winRatioThreshold = 1.2f;
    public float partialWinRatioThreshold = 0.8f;
    public float goldRewardMultiplierWin = 1f;
    public float goldRewardMultiplierPartialWin = 0.5f; public float goldRewardMultiplierLoss = 0.1f;
    public float xpRewardMultiplierWin = 1f;
    public float xpRewardMultiplierPartialWin = 0.5f;
    public float xpRewardMultiplierLoss = 0.1f;

    [Header("Soldier Loss Settings")]
    public float baseSoldierLossRate = 0.1f;
    public float soldierLossRatePerThreatPercent = 0.005f;
    public float soldierLossMultiplierPartialWin = 2f;
    public float soldierLossMultiplierLoss = 5f;

    [Header("Boss Mission Progress")]
    public int missionsToBoss = 10;
    public int completedMissionsCounter = 0;

    [HideInInspector] public bool isExpeditionActive = false;
    private float expeditionTimer = 0f;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }

        if (expeditionOutcomePanel != null) expeditionOutcomePanel.SetActive(false);
        if (threatWarningObject != null) threatWarningObject.SetActive(false);
        if (raidOutcomePanel != null) raidOutcomePanel.SetActive(false);
    }

    void Start()
    {
        UpdateThreatUI();
        UpdateExpeditionStatusUI();
        UpdateBossButtonVisibility(); // Обновление визуалов босса при старте
        StartCoroutine(ThreatGrowthRoutine());

        if (expeditionPreparationPanel != null) expeditionPreparationPanel.SetActive(false);

        if (closeOutcomeButton != null)
        {
            closeOutcomeButton.onClick.RemoveAllListeners();
            closeOutcomeButton.onClick.AddListener(HideExpeditionOutcomePanel);
        }
        if (closeRaidOutcomeButton != null)
        {
            closeRaidOutcomeButton.onClick.RemoveAllListeners();
            closeRaidOutcomeButton.onClick.AddListener(HideRaidOutcomePanel);
        }
    }

    void Update()
    {
        if (isExpeditionActive)
        {
            expeditionTimer -= Time.deltaTime;
            if (expeditionTimer < 0) expeditionTimer = 0;
            UpdateExpeditionStatusUI();
        }
    }

    // --- ВОССТАНОВЛЕНИЕ ЭКСПЕДИЦИИ ПОСЛЕ ЗАГРУЗКИ ---
    public void ResumeExpeditionAfterLoad(ExpeditionSaveData data)
    {
        if (data == null || !data.isActive) return;

        MissionDefinition mission = MissionManager.Instance.allMissions[data.missionIndex];
        Hero hero = null;
        if (!string.IsNullOrEmpty(data.heroTypeName))
        {
            hero = HeroManager.Instance.hiredHeroes.Find(h => h.Definition.heroTypeName == data.heroTypeName);
        }

        DateTime startTime = DateTime.Parse(data.startTime);
        TimeSpan elapsed = DateTime.Now - startTime;
        float remainingTime = data.duration - (float)elapsed.TotalSeconds;

        isExpeditionActive = true;

        if (remainingTime <= 0)
        {
            expeditionTimer = 0;
            EndExpedition(hero, data.playerPower, data.swordsmen, data.archers, data.shieldbearers, data.enemyPower, mission);
        }
        else
        {
            expeditionTimer = remainingTime;
            StartCoroutine(ExpeditionTimerRoutine(hero, data.playerPower, data.swordsmen, data.archers, data.shieldbearers, data.enemyPower, mission));
        }
    }

    // --- ЛОГИКА БОССА (ВИЗУАЛ) ---
    public void UpdateBossButtonVisibility()
    {
        if (bossButton == null) return;
        bossButton.SetActive(true); // Всегда видна

        if (bossProgressOverlay != null && bossProgressText != null)
        {
            float progress = (float)completedMissionsCounter / missionsToBoss;
            bossProgressOverlay.fillAmount = Mathf.Clamp01(1f - progress);

            if (completedMissionsCounter >= missionsToBoss)
            {
                bossProgressText.text = "READY!";
                if (bossActualButton != null) bossActualButton.interactable = true;
            }
            else
            {
                bossProgressText.text = $"{completedMissionsCounter}/{missionsToBoss}";
                if (bossActualButton != null) bossActualButton.interactable = false;
            }
        }
    }

    // --- УГРОЗА ---
    IEnumerator ThreatGrowthRoutine()
    {
        while (true)
        {
            threatLevel = Mathf.Min(threatLevel + baseThreatIncreasePerSecond * Time.deltaTime, maxThreat);
            UpdateThreatUI();

            if (threatLevel >= maxThreat)
            {
                TriggerNegativeEvent();
                threatLevel = 0; // Сбрасываем угрозу после события
            }
            yield return null;
        }
    }

    private void TriggerNegativeEvent()
    {
        if (GameManager.Instance != null)
        {
            long goldToLose = (long)(GameManager.Instance.Gold * goldLossPercentage);
            GameManager.Instance.Gold -= goldToLose;

            int peopleLost = 0;
            if (GameManager.Instance.People >= peopleLossAmount)
            {
                GameManager.Instance.People -= peopleLossAmount;
                peopleLost = peopleLossAmount;
            }

            ShowDungeonMessage($"Threat Max! Resources lost!"); // Маленькое уведомление

            ShowRaidOutcome(goldToLose, peopleLost); // Большая панель
            if (CloudSaveManager.Instance != null) CloudSaveManager.Instance.SaveToCloud();
        }
    }

    private void ShowRaidOutcome(long lostGold, int lostPeople)
    {
        if (raidOutcomePanel == null) return;
        raidTitleText.text = "VILLAGE RAIDED!";
        raidLostGoldText.text = $"Gold Lost: -{lostGold:N0}";
        raidLostPeopleText.text = $"People Lost: -{lostPeople}";
        raidOutcomePanel.SetActive(true);
    }

    public void UpdateThreatUI()
    {
        if (threatLevelText != null) threatLevelText.text = $"Threat: {threatLevel:F0}%";
        if (currentEnemyPowerDisplay != null)
        {
            float calculatedEnemyPower = baseDungeonEnemyPower + (threatLevel * enemyPowerPerThreatPercent);
            currentEnemyPowerDisplay.text = $"Enemy Power: {calculatedEnemyPower:N0}";
        }

        if (threatWarningObject != null)
        {
            if (threatLevel >= threatWarningThreshold && !isExpeditionActive)
            {
                threatWarningObject.SetActive(true);
                // ИСПРАВЛЕНО: используем TextMeshProUGUI
                var textComp = threatWarningObject.GetComponent<TextMeshProUGUI>();
                if (textComp != null)
                {
                    textComp.text = $"Threat Level Critical! ({threatLevel:F0}%) Send an Expedition!";
                }
            }
            else threatWarningObject.SetActive(false);
        }
    }

    // --- ЭКСПЕДИЦИИ ---
    public void OpenExpeditionPreparationPanel()
    {
        if (isExpeditionActive) { ShowDungeonMessage("Expedition active!"); return; }
        if (expeditionPreparationPanel != null)
        {
            expeditionPreparationPanel.SetActive(true);
            if (expeditionPreparationUI != null) expeditionPreparationUI.RefreshUI();
        }
    }

    public void StartExpedition(Hero selectedHero, int sw, int arch, int sh, MissionDefinition mission)
    {
        if (isExpeditionActive || mission == null) return;

        long totalExpeditionPower = 0;
        if (selectedHero != null)
        {
            totalExpeditionPower += selectedHero.CurrentAttack + selectedHero.CurrentHealth + selectedHero.CurrentDefense;
            selectedHero.SetExpeditionStatus(true);
        }

        if (BarracksManager.Instance != null)
        {
            totalExpeditionPower += (long)(sw * (BarracksManager.Instance.swordsmanDef.attack + BarracksManager.Instance.swordsmanDef.health + BarracksManager.Instance.swordsmanDef.defense));
            totalExpeditionPower += (long)(arch * (BarracksManager.Instance.archerDef.attack + BarracksManager.Instance.archerDef.health + BarracksManager.Instance.archerDef.defense));
            totalExpeditionPower += (long)(sh * (BarracksManager.Instance.shieldbearerDef.attack + BarracksManager.Instance.shieldbearerDef.health + BarracksManager.Instance.shieldbearerDef.defense));
        }

        float enemyPowerForMission = mission.enemyPowerBase + (enemyPowerPerThreatPercent * threatLevel * mission.threatMultiplier);

        GameManager.Instance.SwordsmenCount -= sw;
        GameManager.Instance.ArchersCount -= arch;
        GameManager.Instance.ShieldbearersCount -= sh;
        GameManager.Instance.UpdateTroopUI();

        isExpeditionActive = true;
        expeditionTimer = mission.durationSeconds;

        if (CloudSaveManager.Instance != null)
        {
            var data = CloudSaveManager.Instance._gameData.activeExpedition;
            data.isActive = true;
            data.startTime = DateTime.Now.ToString("o");
            data.duration = mission.durationSeconds;
            data.heroTypeName = (selectedHero != null) ? selectedHero.Definition.heroTypeName : "";
            data.swordsmen = sw; data.archers = arch; data.shieldbearers = sh;
            data.playerPower = totalExpeditionPower; data.enemyPower = enemyPowerForMission;
            data.missionIndex = MissionManager.Instance.allMissions.IndexOf(mission);
            CloudSaveManager.Instance.SaveToCloud();
        }

        UpdateExpeditionStatusUI();
        StartCoroutine(ExpeditionTimerRoutine(selectedHero, totalExpeditionPower, sw, arch, sh, enemyPowerForMission, mission));
    }

    IEnumerator ExpeditionTimerRoutine(Hero hero, float playerPowerSent, int swSent, int archSent, int shSent, float enemyPowerForMission, MissionDefinition mission)
    {
        yield return new WaitForSeconds(expeditionTimer);
        EndExpedition(hero, playerPowerSent, swSent, archSent, shSent, enemyPowerForMission, mission);
    }

    private void EndExpedition(Hero hero, float playerPowerSent, int swSent, int archSent, int shSent, float enemyPowerForMission, MissionDefinition mission)
    {
        isExpeditionActive = false;

        if (CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance._gameData.activeExpedition.isActive = false;
            CloudSaveManager.Instance.SaveToCloud();
        }

        float ratio = playerPowerSent / enemyPowerForMission;

        string outcome = "";
        float goldRewardMult = 0f;
        float xpRewardMult = 0f;
        int fragmentsGained = 0;
        long finalGold = 0;
        long finalXP = 0;

        if (ratio >= winRatioThreshold)
        {
            outcome = "VICTORY";
            goldRewardMult = goldRewardMultiplierWin;
            xpRewardMult = xpRewardMultiplierWin;
            fragmentsGained = mission.fragmentReward;
            completedMissionsCounter++; // УВЕЛИЧЕНИЕ ПРОГРЕССА К БОССУ
        }
        else if (ratio >= partialWinRatioThreshold)
        {
            outcome = "PARTIAL VICTORY";
            goldRewardMult = goldRewardMultiplierPartialWin;
            xpRewardMult = xpRewardMultiplierPartialWin;
        }
        else
        {
            outcome = "DEFEAT";
            goldRewardMult = goldRewardMultiplierLoss;
            xpRewardMult = xpRewardMultiplierLoss;
        }

        if (GameManager.Instance != null)
        {
            finalGold = (long)(mission.goldReward * goldRewardMult);
            GameManager.Instance.Gold += finalGold;
        }

        if (hero != null)
        {
            finalXP = (long)(mission.xpReward * xpRewardMult);
            hero.GainXP(finalXP);
            if (outcome == "VICTORY")
            {
                hero.GainFragments(fragmentsGained);
                if (DailyMissionManager.Instance != null)
                    DailyMissionManager.Instance.ProgressMission(DailyMissionType.FinishExpedition, 1);
            }
            HeroManager.Instance.UpdateHeroUI();
        }

        string lostSoldiersInfo = HandleSoldierLossesAndGetInfo(swSent, archSent, shSent, ratio);

        if (ratio >= partialWinRatioThreshold)
        {
            threatLevel = Mathf.Max(0, threatLevel - threatReductionPerExpedition);
        }

        UpdateThreatUI();
        UpdateExpeditionStatusUI();
        UpdateBossButtonVisibility(); // ОБНОВЛЕНИЕ КНОПКИ БОССА ПОСЛЕ МИССИИ

        ShowExpeditionOutcome(outcome, finalGold, finalXP, fragmentsGained, lostSoldiersInfo); // Показываем большое окно результатов
    }

    // --- НОВЫЙ МЕТОД: Отображение результатов экспедиции ---
    private void ShowExpeditionOutcome(string outcome, long gold, long xp, int frags, string lost)
    {
        if (expeditionOutcomePanel == null) return;

        // Убедимся, что все TextMeshProUGUI UI-элементы назначены
        if (outcomeTitleText != null) outcomeTitleText.text = outcome;
        if (rewardGoldText != null) rewardGoldText.text = $"Gold: +{gold:N0}";
        if (rewardXPText != null) rewardXPText.text = xp > 0 ? $"Hero XP: +{xp:N0}" : "Hero XP: -";
        if (rewardFragmentsText != null) rewardFragmentsText.text = frags > 0 ? $"Fragments: +{frags}" : "Fragments: -";

        if (soldiersLostText != null)
        {
            soldiersLostText.gameObject.SetActive(!string.IsNullOrEmpty(lost));
            soldiersLostText.text = $"Lost: {lost}";
        }

        if (missionsUntilBossOutcomeText != null)
        {
            int left = missionsToBoss - completedMissionsCounter;
            missionsUntilBossOutcomeText.text = left > 0 ? $"Missions until Boss: {left}" : "BOSS IS READY!";
        }

        expeditionOutcomePanel.SetActive(true);
    }

    // --- МЕТОДЫ ОТОБРАЖЕНИЯ ПАНЕЛЕЙ И МЕССЕЙДЖЕЙ ---
    private void HideExpeditionOutcomePanel() => expeditionOutcomePanel.SetActive(false);
    private void HideRaidOutcomePanel() => raidOutcomePanel.SetActive(false);

    public void ShowDungeonMessage(string message) // СНОВА PUBLIC
    {
        if (dungeonMessageText == null) return;
        if (currentDungeonMessageRoutine != null) StopCoroutine(currentDungeonMessageRoutine);
        dungeonMessageText.text = message;
        dungeonMessageText.gameObject.SetActive(true);
        currentDungeonMessageRoutine = StartCoroutine(HideDungeonMessageRoutine(dungeonMessageDisplayDuration));
    }

    private IEnumerator HideDungeonMessageRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (dungeonMessageText != null) dungeonMessageText.gameObject.SetActive(false);
        currentDungeonMessageRoutine = null;
    }

    // --- ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ (ОСТАВЛЕНЫ ДЛЯ ПОЛНОТЫ) ---
    private string HandleSoldierLossesAndGetInfo(int sw, int ar, int sh, float ratio)
    {
        int total = sw + ar + sh;
        if (total <= 0) return "";
        float m = (ratio >= winRatioThreshold) ? 1f : (ratio >= partialWinRatioThreshold) ? soldierLossMultiplierPartialWin : soldierLossMultiplierLoss;
        float rate = Mathf.Min(baseSoldierLossRate + (soldierLossRatePerThreatPercent * threatLevel), 1f) * m;
        int lost = Mathf.CeilToInt(total * Mathf.Min(rate, 1f));
        if (lost == 0)
        {
            GameManager.Instance.SwordsmenCount += sw; GameManager.Instance.ArchersCount += ar; GameManager.Instance.ShieldbearersCount += sh;
            return "";
        }
        float surv = 1f - (lost / (float)total);
        int swL = sw - Mathf.FloorToInt(sw * surv);
        int arL = ar - Mathf.FloorToInt(ar * surv);
        int shL = sh - Mathf.FloorToInt(sh * surv);

        GameManager.Instance.SwordsmenCount += Mathf.FloorToInt(sw * surv);
        GameManager.Instance.ArchersCount += Mathf.FloorToInt(ar * surv);
        GameManager.Instance.ShieldbearersCount += Mathf.FloorToInt(sh * surv);
        GameManager.Instance.UpdateTroopUI();

        List<string> p = new List<string>();
        if (swL > 0) p.Add($"{swL} Sw."); if (arL > 0) p.Add($"{arL} Ar."); if (shL > 0) p.Add($"{shL} Sh.");
        return string.Join(", ", p);
    }

    public void UpdateExpeditionStatusUI() => expeditionStatusText.text = isExpeditionActive ? $"Time: {expeditionTimer:F0}s" : "Idle";
    public void StartBossBattle()
    {
        if (completedMissionsCounter >= missionsToBoss)
        {
            ShowDungeonMessage("Boss Battle Commencing! Good Luck!");
            Debug.Log("Starting Boss Battle!");
            BossBattleManager.Instance.StartBossBattle();
        }
        else
        {
            ShowDungeonMessage($"Need to complete {missionsToBoss - completedMissionsCounter} more missions to fight the Boss!");
        }
    }
}