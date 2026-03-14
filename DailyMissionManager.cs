using UnityEngine;
using System.Collections.Generic;
using GamePush; // Обязательно: проверь поле DailyResetTrigger в панели GamePush!
using UnityEngine.UI;
using TMPro;

public class DailyMissionManager : MonoBehaviour
{
    [Header("Daily Mission UI")]
    public GameObject dailyMissionNotificationDot;

    [Header("Daily Reward Definition")]
    public List<DailyRewardDefinition> allDailyRewards; // Ссылки на ScriptableObject DailyRewardDefinition
    [Header("Gift UI")]
    public GameObject dailyGiftPanel;
    public TextMeshProUGUI giftDescriptionText;
    public Image giftIconImage;

    public static DailyMissionManager Instance { get; private set; }

    [Header("Base Settings")]
    public List<DailyMissionDefinition> allPossibleMissions; // Список всех ассетов заданий
    public int missionsPerDay = 3;

    private List<DailyMissionSaveData> _currentMissions;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void InitMissions(List<DailyMissionSaveData> loadedMissions)
    {
        Debug.Log("<color=green>!!! [DailyMissionManager] InitMissions Started !!!</color>");
        _currentMissions = loadedMissions;

        // --- ИСПРАВЛЕНИЕ: GP_Player.GetInt() без второго аргумента ---
        int resetTrigger = GP_Player.GetInt("DailyResetTrigger");
        Debug.Log("[DailyMissionManager] ResetTrigger Value: " + resetTrigger);

        if (resetTrigger == 0)
        {
            Debug.Log("[DailyMissionManager] New day detected! Generating fresh missions and giving daily gift...");
            GenerateNewMissions();
            GiveDailyGift();

            GP_Player.Set("DailyResetTrigger", 1);
            GP_Player.Sync();
        }
        else
        {
            Debug.Log("[DailyMissionManager] Continuing today's missions.");
        }

        UpdateMissionUI();
    }

    private void GiveDailyGift()
    {
        if (allDailyRewards == null || allDailyRewards.Count == 0)
        {
            Debug.LogWarning("[DailyMissionManager] No DailyRewardDefinitions assigned. Cannot give daily gift.");
            return;
        }

        DailyRewardDefinition gift = allDailyRewards[UnityEngine.Random.Range(0, allDailyRewards.Count)];
        string finalMessage = "";

        switch (gift.type)
        {
            case RewardType.Gold:
                GameManager.Instance.Gold += gift.amount;
                finalMessage = $"You got {gift.amount:N0} Gold!";
                break;

            case RewardType.Wood:
                GameManager.Instance.Wood += gift.amount;
                finalMessage = $"You got {gift.amount:N0} Wood!";
                break;

            case RewardType.Iron:
                GameManager.Instance.Iron += gift.amount;
                finalMessage = $"You got {gift.amount:N0} Iron!";
                break;

            case RewardType.Stone:
                GameManager.Instance.Stone += gift.amount;
                finalMessage = $"You got {gift.amount:N0} Stone!";
                break;

            case RewardType.FullPopulation:
                GameManager.Instance.People = GameManager.Instance.MaxPeople;
                finalMessage = "Village is full! Population restored.";
                break;

            case RewardType.HeroFragment:
                if (HeroManager.Instance != null && HeroManager.Instance.hiredHeroes.Count > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, HeroManager.Instance.hiredHeroes.Count);
                    Hero luckyHero = HeroManager.Instance.hiredHeroes[randomIndex];
                    luckyHero.GainFragments(gift.amount);
                    HeroManager.Instance.UpdateHeroUI();
                    finalMessage = $"{luckyHero.Name} received {gift.amount} fragments!";
                }
                else
                {
                    long compensation = 5000;
                    GameManager.Instance.Gold += compensation;
                    finalMessage = $"No heroes found! Received {compensation:N0} Gold instead.";
                }
                break;
        }

        ShowGiftUI(gift);
    }

    private void ShowGiftUI(DailyRewardDefinition gift)
    {
        if (dailyGiftPanel == null)
        {
            Debug.LogWarning("[DailyMissionManager] Daily Gift Panel UI not assigned. Cannot show gift UI.");
            return;
        }

        dailyGiftPanel.SetActive(true);
        if (giftDescriptionText != null) giftDescriptionText.text = $"Daily Gift: {gift.rewardName} x{gift.amount}";
        if (giftIconImage != null && gift.icon != null) giftIconImage.sprite = gift.icon;
    }

    private void GenerateNewMissions()
    {
        if (_currentMissions == null)
        {
            _currentMissions = new List<DailyMissionSaveData>();
        }
        _currentMissions.Clear();

        List<int> pool = new List<int>();
        for (int i = 0; i < allPossibleMissions.Count; i++) pool.Add(i);

        for (int i = 0; i < missionsPerDay; i++)
        {
            if (pool.Count == 0) break;

            int randomIndex = UnityEngine.Random.Range(0, pool.Count);
            int selectedIndex = pool[randomIndex];

            _currentMissions.Add(new DailyMissionSaveData
            {
                missionIndex = selectedIndex,
                progress = 0,
                isClaimed = false
            });

            pool.RemoveAt(randomIndex);
        }
    }

    public void ProgressMission(DailyMissionType type, int amount)
    {
        if (_currentMissions == null) return;

        bool hasProgressed = false;
        for (int i = 0; i < _currentMissions.Count; i++)
        {
            var data = _currentMissions[i];
            if (data.missionIndex >= allPossibleMissions.Count) continue;
            var def = allPossibleMissions[data.missionIndex];

            if (def.missionType == type && !data.isClaimed && data.progress < def.requiredAmount)
            {
                data.progress = Mathf.Min(data.progress + amount, def.requiredAmount);
                hasProgressed = true;
                Debug.Log($"[Daily] Progress: {def.missionType} -> {data.progress}/{def.requiredAmount}");
            }
        }

        if (hasProgressed)
        {
            UpdateMissionUI();
            if (CloudSaveManager.Instance != null) CloudSaveManager.Instance.SaveToCloud();
        }
    }

    public bool HasPendingRewards()
    {
        if (_currentMissions == null) return false;

        foreach (var data in _currentMissions)
        {
            if (data.missionIndex >= allPossibleMissions.Count) continue;
            var def = allPossibleMissions[data.missionIndex];

            if (data.progress >= def.requiredAmount && !data.isClaimed)
            {
                return true;
            }
        }
        return false;
    }

    public void UpdateDailyMissionNotification()
    {
        if (dailyMissionNotificationDot == null) return;
        if (DailyMissionManager.Instance != null)
        {
            bool showDot = DailyMissionManager.Instance.HasPendingRewards();
            dailyMissionNotificationDot.SetActive(showDot);
        }
    }

    public void ClaimReward(int listIndex)
    {
        if (_currentMissions == null || listIndex >= _currentMissions.Count) return;

        var data = _currentMissions[listIndex];
        var def = allPossibleMissions[data.missionIndex];

        if (!data.isClaimed && data.progress >= def.requiredAmount)
        {
            data.isClaimed = true;
            GameManager.Instance.Gold += def.goldReward;

            if (CloudSaveManager.Instance != null)
            {
                CloudSaveManager.Instance.SaveToCloud();
            }

            UpdateMissionUI();
            Debug.Log($"[Daily] Reward Claimed: {def.goldReward} Gold!");
        }
    }

    // --- RerollMissionsWithAds() ---
    // Метод раскомментирован и готов к использованию с GamePush Ads
    public void RerollMissionsWithAds()
    {
        GP_Ads.ShowRewarded("REROLL_MISSIONS", (value) => {
            if (value == "REROLL_MISSIONS")
            {
                Debug.Log("[Daily] Rerolling missions for ads!");
                GenerateNewMissions();
                if (CloudSaveManager.Instance != null) CloudSaveManager.Instance.SaveToCloud();
                UpdateMissionUI();
            }
        });
    }

    public List<DailyMissionSaveData> GetCurrentMissionsData()
    {
        return _currentMissions;
    }

    public void UpdateMissionUI()
    {
        UpdateDailyMissionNotification();
        DailyMissionPanelUI ui = Object.FindAnyObjectByType<DailyMissionPanelUI>();
        if (ui != null && ui.gameObject.activeInHierarchy)
        {
            ui.RefreshUI();
        }
    }

    public void CloseDailyGiftPanel()
    {
        if (dailyGiftPanel != null)
        {
            dailyGiftPanel.SetActive(false);
        }
    }
}