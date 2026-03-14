using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using GamePush;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    [Header("UI References")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI woodText;
    public TextMeshProUGUI ironText;
    public TextMeshProUGUI stoneText;
    public TextMeshProUGUI peopleText;
    public TextMeshProUGUI clickUpgradeMessageText;

    [Header("AFK Rewards UI")]
    public GameObject afkPanel; // Перетащи сюда панель наград
    public TextMeshProUGUI afkReportText; // Перетащи сюда текст внутри панели
    public float maxAfkHours = 6f; // Лимит 6 часов

    [Header("Message Settings")]
    public float clickMessageDisplayDuration = 2.0f;
    private Coroutine currentClickMessageRoutine;

    [Header("Screen Panels")]
    public GameObject villageScreenPanel;
    public GameObject resourceScreenPanel;
    public GameObject heroScreenPanel;
    public GameObject barracksScreenPanel;
    public GameObject dungeonScreenPanel;
    private List<GameObject> allScreenPanels;

    [Header("Clicker Settings")]
    public int baseGoldPerClick = 1;
    public int baseWoodPerClick = 1;
    public int baseIronPerClick = 1;
    public int baseStonePerClick = 1;

    [Header("Passive Income Settings")]
    public float passiveGoldPerSecond = 0f;
    public float passiveWoodPerSecond = 0f;
    public float passiveIronPerSecond = 0f;
    public float passiveStonePerSecond = 0f;
    public float basePeoplePerSecondRate = 0.01f;
    private float _currentPassivePeoplePerSecond;

    public float goldPerPersonPerSecond = 0.05f;

    [Header("Upgrade Settings - Click Gold")]
    public int goldClickUpgradeLevel = 0;
    public long goldClickUpgradeBaseCost = 10;
    public float goldClickUpgradeCostMultiplier = 1.2f;
    public int goldClickUpgradeAmountPerLevel = 1;

    [Header("Upgrade Settings - Click Wood")]
    public int woodClickUpgradeLevel = 0;
    public long woodClickUpgradeBaseCost = 15;
    public float woodClickUpgradeCostMultiplier = 1.25f;
    public int woodClickUpgradeAmountPerLevel = 1;
    private int _currentWoodPerClick = 1;

    [Header("Upgrade Settings - Click Iron")]
    public int ironClickUpgradeLevel = 0;
    public long ironClickUpgradeBaseCost = 25;
    public float ironClickUpgradeCostMultiplier = 1.4f;
    public int ironClickUpgradeAmountPerLevel = 1;
    private int _currentIronPerClick = 1;

    [Header("Upgrade Settings - Click Stone")]
    public int stoneClickUpgradeLevel = 0;
    public long stoneClickUpgradeBaseCost = 20;
    public float stoneClickUpgradeCostMultiplier = 1.3f;
    public int stoneClickUpgradeAmountPerLevel = 1;
    private int _currentStonePerClick = 1;

    [Header("Click Upgrade Caps")]
    public int initialClickUpgradeCap = 10;

    private int _maxClickLevel_Gold;
    private int _maxClickLevel_Wood;
    private int _maxClickLevel_Iron;
    private int _maxClickLevel_Stone;

    [Header("UI References - Upgrades")]
    public TextMeshProUGUI upgradeClickGoldButtonText;
    public TextMeshProUGUI upgradeWoodClickButtonText;
    public TextMeshProUGUI upgradeIronClickButtonText;
    public TextMeshProUGUI upgradeStoneClickButtonText;

    [Header("UI References - Troops")]
    public TextMeshProUGUI swordsmenText;
    public TextMeshProUGUI archersText;
    public TextMeshProUGUI shieldbearersText;

    [Header("Ads Rewards Temporary")]
    private long _pendingAfkGold;
    private int _pendingAfkWood;
    private int _pendingAfkIron;
    private int _pendingAfkStone;

    private int _swordsmenCount;
    private int _archersCount;
    private int _shieldbearersCount;

    public int SwordsmenCount { get { return _swordsmenCount; } set { _swordsmenCount = value; UpdateTroopUI(); } }
    public int ArchersCount { get { return _archersCount; } set { _archersCount = value; UpdateTroopUI(); } }
    public int ShieldbearersCount { get { return _shieldbearersCount; } set { _shieldbearersCount = value; UpdateTroopUI(); } }

    private int _currentCoinsPerClick;

    private float _currentPartialGold = 0f;
    private float _currentPartialWood = 0f;
    private float _currentPartialIron = 0f;
    private float _currentPartialStone = 0f;
    private float _currentPartialPeople = 0f;

    private long _gold;
    private int _wood;
    private int _iron;
    private int _stone;
    private int _people;
    private int _maxPeople;
   
    public long Gold { get { return _gold; } set { _gold = value; UpdateResourceUI(); } }
    public int Wood { get { return _wood; } set { _wood = value; UpdateResourceUI(); } }
    public int Iron { get { return _iron; } set { _iron = value; UpdateResourceUI(); } }
    public int Stone { get { return _stone; } set { _stone = value; UpdateResourceUI(); } }
    public int People { get { return _people; } set { _people = value; UpdateResourceUI(); } }
    public int MaxPeople { get { return _maxPeople; } set { _maxPeople = value; UpdateResourceUI(); } }

    void Start()
    {
        _gold = 0;
        _wood = 0;
        _iron = 0;
        _stone = 0;
        _people = 5;
        _maxPeople = 10;
        _swordsmenCount = 0;
        _archersCount = 0;
        _shieldbearersCount = 0;

        RecalculateMaxPeople();
        RecalculatePassiveIncome();
        RecalculateMaxClickLevels();
        UpdateResourceUI();
        UpdateTroopUI();
        ShowScreen(villageScreenPanel);

        // В начале игры, когда загрузилось меню:
        GP_Game.GameplayStart();
    }

    void Update()
    {
        AddPassiveResources(Time.deltaTime);
    }

    public void UpdateTroopUI()
    {
        if (swordsmenText != null) swordsmenText.text = $"Swordsmen: {_swordsmenCount}";
        if (archersText != null) archersText.text = $"Archers: {_archersCount}";
        if (shieldbearersText != null) shieldbearersText.text = $"Shieldbearers: {_shieldbearersCount}";
        //if (DungeonManager.Instance != null) DungeonManager.Instance.UpdateAvailableSoldiersUI();
    }

    private void AddPassiveResources(float deltaTime)
    {
        _currentPartialGold += (passiveGoldPerSecond + (People * goldPerPersonPerSecond)) * deltaTime;
        if (_currentPartialGold >= 1f) { long amountToAdd = (long)_currentPartialGold; Gold += amountToAdd; _currentPartialGold -= amountToAdd; }

        _currentPartialWood += passiveWoodPerSecond * deltaTime;
        if (_currentPartialWood >= 1f) { int amountToAdd = (int)_currentPartialWood; Wood += amountToAdd; _currentPartialWood -= amountToAdd; }

        _currentPartialIron += passiveIronPerSecond * deltaTime;
        if (_currentPartialIron >= 1f) { int amountToAdd = (int)_currentPartialIron; Iron += amountToAdd; _currentPartialIron -= amountToAdd; }

        _currentPartialStone += passiveStonePerSecond * deltaTime;
        if (_currentPartialStone >= 1f) { int amountToAdd = (int)_currentPartialStone; Stone += amountToAdd; _currentPartialStone -= amountToAdd; }

        _currentPartialPeople += _currentPassivePeoplePerSecond * deltaTime;
        if (_currentPartialPeople >= 1f)
        {
            int amountToAdd = (int)_currentPartialPeople;
            if (People + amountToAdd <= MaxPeople) { People += amountToAdd; _currentPartialPeople -= amountToAdd; }
            else { int remainingCapacity = MaxPeople - People; if (remainingCapacity > 0) { People += remainingCapacity; _currentPartialPeople -= remainingCapacity; } else { _currentPartialPeople = 0f; } }
        }
    }

    public void UpdateResourceUI()
    {
        if (goldText != null) goldText.text = "Gold: " + _gold.ToString("N0");
        if (woodText != null) woodText.text = "Wood: " + _wood.ToString();
        if (ironText != null) ironText.text = "Iron: " + _iron.ToString();
        if (stoneText != null) stoneText.text = "Stone: " + _stone.ToString();
        if (peopleText != null) peopleText.text = "People: " + _people.ToString() + "/" + _maxPeople.ToString();
    }

    // Этот метод мы вешаем на кнопку "Забрать х2" в UI
    public void ClaimDoubleAfkReward()
    {
        // Передаем строго по порядку: тег, метод награды, метод старта (null), метод закрытия
        GP_Ads.ShowRewarded("AFK_DOUBLE", OnDoubleRewardSuccess, null, OnDoubleRewardClose);
    }

    // 1. Метод для успешной награды (только х2)
    private void OnDoubleRewardSuccess(string value)
    {
        if (value == "AFK_DOUBLE")
        {
            Gold += _pendingAfkGold * 2;
            Wood += _pendingAfkWood * 2;
            Iron += _pendingAfkIron * 2;
            Stone += _pendingAfkStone * 2;
            UpdateResourceUI();
            if (CloudSaveManager.Instance != null) CloudSaveManager.Instance.SaveToCloud();
            CloseAfkPanel();
        }
    }

    // 2. Метод, который сработает ПРИ ЛЮБОМ ЗАКРЫТИИ рекламы
    private void OnDoubleRewardClose(bool success)
    {
        // Если success == false, значит награда НЕ была получена (реклама сорвалась или закрыта раньше)
        if (!success)
        {
            Debug.Log("Ads failed. Giving normal reward.");
            ClaimNormalAfkReward(); // Выдаем 1х и закрываем панель
        }
    }

    // ====== AFK REWARDS LOGIC ======
    public void ApplyAfkRewards(double secondsAway)
    {
        double cappedSeconds = Mathf.Min((float)secondsAway, maxAfkHours * 3600f);
        if (cappedSeconds < 60) return;

        // Считаем доход в секунду
        float gPerSec = BuildingManager.Instance.GetTotalGoldIncomePerSecond() + (People * goldPerPersonPerSecond);
        float wPerSec = BuildingManager.Instance.GetTotalWoodIncomePerSecond();
        float iPerSec = BuildingManager.Instance.GetTotalIronIncomePerSecond();
        float sPerSec = BuildingManager.Instance.GetTotalStoneIncomePerSecond();

        // Рассчитываем итоговую сумму
        _pendingAfkGold = (long)(gPerSec * cappedSeconds);
        _pendingAfkWood = (int)(wPerSec * cappedSeconds);
        _pendingAfkIron = (int)(iPerSec * cappedSeconds);
        _pendingAfkStone = (int)(sPerSec * cappedSeconds);

        // ВАЖНО: Мы НЕ добавляем ресурсы сразу здесь. 
        // Мы сделаем это в методе получения.

        if (afkPanel != null)
        {
            afkPanel.SetActive(true);
            int hours = Mathf.FloorToInt((float)cappedSeconds / 3600);
            int minutes = Mathf.FloorToInt(((float)cappedSeconds % 3600) / 60);

            string report = $"Welcome back!\nYour village produced resources in <b>{hours}h {minutes}m</b>:\n\n";
            if (_pendingAfkGold > 0) report += $"<color=yellow>Gold: +{_pendingAfkGold:N0}</color>\n";
            if (_pendingAfkWood > 0) report += $"<color=green>Wood: +{_pendingAfkWood}</color>\n";
            if (_pendingAfkIron > 0) report += $"<color=#FF4500>Iron: +{_pendingAfkIron}</color>\n";
            if (_pendingAfkStone > 0) report += $"<color=grey>Stone: +{_pendingAfkStone}</color>";
            afkReportText.text = report;
        }
    }

    public void SyncTotalPowerToLeaderboard()
    {
        // Проверяем, готов ли SDK
        if (!GP_Init.isReady) return;

        long totalpower = 0;

        // 1. Считаем силу героев (Атака + ХП + Защита)
        if (HeroManager.Instance != null && HeroManager.Instance.hiredHeroes != null)
        {
            foreach (Hero h in HeroManager.Instance.hiredHeroes)
            {
                totalpower += h.CurrentAttack + h.CurrentHealth + h.CurrentDefense;
            }
        }

        // 2. Считаем силу армии из BarracksManager
        if (BarracksManager.Instance != null)
        {
            if (BarracksManager.Instance.swordsmanDef != null)
                totalpower += (long)(SwordsmenCount * (BarracksManager.Instance.swordsmanDef.attack + BarracksManager.Instance.swordsmanDef.health + BarracksManager.Instance.swordsmanDef.defense));

            if (BarracksManager.Instance.archerDef != null)
                totalpower += (long)(ArchersCount * (BarracksManager.Instance.archerDef.attack + BarracksManager.Instance.archerDef.health + BarracksManager.Instance.archerDef.defense));

            if (BarracksManager.Instance.shieldbearerDef != null)
                totalpower += (long)(ShieldbearersCount * (BarracksManager.Instance.shieldbearerDef.attack + BarracksManager.Instance.shieldbearerDef.health + BarracksManager.Instance.shieldbearerDef.defense));
        }

        // 3. ОБНОВЛЕНИЕ ПОЛЯ ИГРОКА
        // Лидерборд в панели GamePush настроен на тег "TotalPower", 
        // поэтому он обновится автоматически при синхронизации этого поля.
        GP_Player.Set("TotalPower", totalpower);

        // Отправляем данные на сервер
        GP_Player.Sync();

        Debug.Log($"[Sync] Total Power {totalpower} updated in Player Fields. Leaderboard will sync automatically.");
    }
    public void OpenLeaderboardUI()
    {
        // Откроет стандартное окно GamePush с таблицей лидеров
        GP_Leaderboard.Open("TotalPower");
    }

    // ====== RESOURCE HELPERS ======
    public bool TryAddGold(long amount) { Gold += amount; return true; }
    public bool TrySpendGold(long amount) { if (_gold >= amount) { Gold -= amount; return true; } return false; }
    public bool CanAffordGold(long amount) { return _gold >= amount; }
    public bool TryAddWood(int amount) { Wood += amount; return true; }
    public bool TrySpendWood(int amount) { if (_wood >= amount) { Wood -= amount; return true; } return false; }
    public bool CanAffordWood(int amount) { return _wood >= amount; }
    public bool TryAddIron(int amount) { Iron += amount; return true; }
    public bool TrySpendIron(int amount) { if (_iron >= amount) { Iron -= amount; return true; } return false; }
    public bool CanAffordIron(int amount) { return _iron >= amount; }
    public bool TryAddStone(int amount) { Stone += amount; return true; }
    public bool TrySpendStone(int amount) { if (_stone >= amount) { Stone -= amount; return true; } return false; }
    public bool CanAffordStone(int amount) { return _stone >= amount; }
    public bool TrySpendPeople(int amount) { if (_people >= amount) { People -= amount; return true; } return false; }
    public bool CanAffordPeople(int amount) { return _people >= amount; }

    // ====== SCREEN NAVIGATION ======
    public void ShowScreen(GameObject screenToShow)
    {
        if (allScreenPanels == null) { allScreenPanels = new List<GameObject> { villageScreenPanel, resourceScreenPanel, heroScreenPanel, barracksScreenPanel, dungeonScreenPanel }; }
        foreach (GameObject panel in allScreenPanels) { if (panel != null) panel.SetActive(false); }
        if (screenToShow != null) screenToShow.SetActive(true);
    }

    // ====== CLICK ACTIONS ======
    public void ClickGold() { Gold += _currentCoinsPerClick; DailyMissionManager.Instance.ProgressMission(DailyMissionType.ClickGold, 1); }
    public void ClickWood() { Wood += _currentWoodPerClick; DailyMissionManager.Instance.ProgressMission(DailyMissionType.ClickWood, 1); }
    public void ClickIron() { Iron += _currentIronPerClick; DailyMissionManager.Instance.ProgressMission(DailyMissionType.ClickIron, 1); }
    public void ClickStone() { Stone += _currentStonePerClick; DailyMissionManager.Instance.ProgressMission(DailyMissionType.ClickStone, 1); }

    // ====== UPGRADE LOGIC ======
    public void UpgradeGoldClickPower()
    {
        if (goldClickUpgradeLevel >= _maxClickLevel_Gold) { ShowClickUpgradeMessage("Upgrade House to increase Cap!"); return; }
        long cost = CalculateUpgradeCost(goldClickUpgradeBaseCost, goldClickUpgradeCostMultiplier, goldClickUpgradeLevel);
        if (TrySpendGold(cost))
        {
            goldClickUpgradeLevel++;
            RecalculateCurrentClickPowers();
            UpdateGoldUpgradeButtonUI();
            // СОХРАНЯЕМ
            if (CloudSaveManager.Instance != null) CloudSaveManager.Instance.SaveToCloud();
        }
        else ShowClickUpgradeMessage("Not enough Gold!");
    }

    public void UpgradeWoodClickPower()
    {
        if (woodClickUpgradeLevel >= _maxClickLevel_Wood) { ShowClickUpgradeMessage("Upgrade Lumber Mill to increase Cap!"); return; }
        long cost = CalculateUpgradeCost(woodClickUpgradeBaseCost, woodClickUpgradeCostMultiplier, woodClickUpgradeLevel);
        if (TrySpendGold(cost))
        {
            woodClickUpgradeLevel++;
            RecalculateCurrentClickPowers();
            UpdateWoodUpgradeButtonUI();
            // СОХРАНЯЕМ
            if (CloudSaveManager.Instance != null) CloudSaveManager.Instance.SaveToCloud();
        }
        else ShowClickUpgradeMessage("Not enough Gold!");
    }

    public void UpgradeIronClickPower()
    {
        if (ironClickUpgradeLevel >= _maxClickLevel_Iron) { ShowClickUpgradeMessage("Upgrade Mine to increase Cap!"); return; }
        long cost = CalculateUpgradeCost(ironClickUpgradeBaseCost, ironClickUpgradeCostMultiplier, ironClickUpgradeLevel);
        if (TrySpendGold(cost))
        {
            ironClickUpgradeLevel++;
            RecalculateCurrentClickPowers();
            UpdateIronUpgradeButtonUI();
            // СОХРАНЯЕМ
            if (CloudSaveManager.Instance != null) CloudSaveManager.Instance.SaveToCloud();
        }
        else ShowClickUpgradeMessage("Not enough Gold!");
    }

    public void UpgradeStoneClickPower()
    {
        if (stoneClickUpgradeLevel >= _maxClickLevel_Stone) { ShowClickUpgradeMessage("Upgrade Quarry to increase Cap!"); return; }
        long cost = CalculateUpgradeCost(stoneClickUpgradeBaseCost, stoneClickUpgradeCostMultiplier, stoneClickUpgradeLevel);
        if (TrySpendGold(cost))
        {
            stoneClickUpgradeLevel++;
            RecalculateCurrentClickPowers();
            UpdateStoneUpgradeButtonUI();
            // СОХРАНЯЕМ
            if (CloudSaveManager.Instance != null) CloudSaveManager.Instance.SaveToCloud();
        }
        else ShowClickUpgradeMessage("Not enough Gold!");
    }

    private long CalculateUpgradeCost(long baseCost, float multiplier, int currentLevel) { return (long)(baseCost * Mathf.Pow(multiplier, currentLevel)); }

    private void UpdateGoldUpgradeButtonUI() { if (upgradeClickGoldButtonText != null) upgradeClickGoldButtonText.text = $"Upgrade Gold Click\nCost: {CalculateUpgradeCost(goldClickUpgradeBaseCost, goldClickUpgradeCostMultiplier, goldClickUpgradeLevel):N0}\nLvl: {goldClickUpgradeLevel + 1}/{_maxClickLevel_Gold + 1}"; }
    private void UpdateWoodUpgradeButtonUI() { if (upgradeWoodClickButtonText != null) upgradeWoodClickButtonText.text = $"Upgrade Wood Click\nCost: {CalculateUpgradeCost(woodClickUpgradeBaseCost, woodClickUpgradeCostMultiplier, woodClickUpgradeLevel):N0}\nLvl: {woodClickUpgradeLevel + 1}/{_maxClickLevel_Wood + 1}"; }
    private void UpdateIronUpgradeButtonUI() { if (upgradeIronClickButtonText != null) upgradeIronClickButtonText.text = $"Upgrade Iron Click\nCost: {CalculateUpgradeCost(ironClickUpgradeBaseCost, ironClickUpgradeCostMultiplier, ironClickUpgradeLevel):N0}\nLvl: {ironClickUpgradeLevel + 1}/{_maxClickLevel_Iron + 1}"; }
    private void UpdateStoneUpgradeButtonUI() { if (upgradeStoneClickButtonText != null) upgradeStoneClickButtonText.text = $"Upgrade Stone Click\nCost: {CalculateUpgradeCost(stoneClickUpgradeBaseCost, stoneClickUpgradeCostMultiplier, stoneClickUpgradeLevel):N0}\nLvl: {stoneClickUpgradeLevel + 1}/{_maxClickLevel_Stone + 1}"; }

    public void RecalculatePassiveIncome()
    {
        passiveGoldPerSecond = 0f; passiveWoodPerSecond = 0f; passiveIronPerSecond = 0f; passiveStonePerSecond = 0f;
        _currentPassivePeoplePerSecond = basePeoplePerSecondRate;
        if (BuildingManager.Instance != null)
        {
            if (BuildingManager.Instance.LumberMill != null) passiveWoodPerSecond += BuildingManager.Instance.LumberMill.GetCurrentWoodPerSecond();
            if (BuildingManager.Instance.Mine != null) passiveIronPerSecond += BuildingManager.Instance.Mine.GetCurrentIronPerSecond();
            if (BuildingManager.Instance.Quarry != null) passiveStonePerSecond += BuildingManager.Instance.Quarry.GetCurrentStonePerSecond();
            if (BuildingManager.Instance.House != null) { passiveGoldPerSecond += BuildingManager.Instance.House.GetCurrentGoldPerSecond(); _currentPassivePeoplePerSecond += BuildingManager.Instance.House.GetCurrentPeoplePerSecond(); }
        }
    }

    public void RecalculateMaxPeople()
    {
        _maxPeople = 10;
        if (BuildingManager.Instance != null && BuildingManager.Instance.House != null)
        {
            int bonus = BuildingManager.Instance.House.GetCurrentMaxPeopleIncrease();
            _maxPeople += bonus;
            Debug.Log($"[GameManager] Recalculating Max People. Bonus from House (Lvl {BuildingManager.Instance.House.Level}): {bonus}. Total: {_maxPeople}");
        }
        UpdateResourceUI();
    }

    public void RecalculateMaxClickLevels()
    {
        _maxClickLevel_Gold = _maxClickLevel_Wood = _maxClickLevel_Iron = _maxClickLevel_Stone = initialClickUpgradeCap;
        if (BuildingManager.Instance != null)
        {
            if (BuildingManager.Instance.House != null) _maxClickLevel_Gold += BuildingManager.Instance.House.Level * BuildingManager.Instance.House.Definition.clickLevelCapPerBuildingLevel;
            if (BuildingManager.Instance.LumberMill != null) _maxClickLevel_Wood += BuildingManager.Instance.LumberMill.Level * BuildingManager.Instance.LumberMill.Definition.clickLevelCapPerBuildingLevel;
            if (BuildingManager.Instance.Mine != null) _maxClickLevel_Iron += BuildingManager.Instance.Mine.Level * BuildingManager.Instance.Mine.Definition.clickLevelCapPerBuildingLevel;
            if (BuildingManager.Instance.Quarry != null) _maxClickLevel_Stone += BuildingManager.Instance.Quarry.Level * BuildingManager.Instance.Quarry.Definition.clickLevelCapPerBuildingLevel;
        }
        RecalculateCurrentClickPowers();
        UpdateGoldUpgradeButtonUI(); UpdateWoodUpgradeButtonUI(); UpdateIronUpgradeButtonUI(); UpdateStoneUpgradeButtonUI();
    }

    public void RecalculateCurrentClickPowers()
    {
        _currentCoinsPerClick = baseGoldPerClick + (goldClickUpgradeLevel * goldClickUpgradeAmountPerLevel) + (BuildingManager.Instance?.House?.GetCurrentClickPower() ?? 0);
        _currentWoodPerClick = baseWoodPerClick + (woodClickUpgradeLevel * woodClickUpgradeAmountPerLevel) + (BuildingManager.Instance?.LumberMill?.GetCurrentClickPower() ?? 0);
        _currentIronPerClick = baseIronPerClick + (ironClickUpgradeLevel * ironClickUpgradeAmountPerLevel) + (BuildingManager.Instance?.Mine?.GetCurrentClickPower() ?? 0);
        _currentStonePerClick = baseStonePerClick + (stoneClickUpgradeLevel * stoneClickUpgradeAmountPerLevel) + (BuildingManager.Instance?.Quarry?.GetCurrentClickPower() ?? 0);
    }

    public void ShowClickUpgradeMessage(string message)
    {
        if (clickUpgradeMessageText == null) return;
        if (currentClickMessageRoutine != null) StopCoroutine(currentClickMessageRoutine);
        clickUpgradeMessageText.text = message;
        clickUpgradeMessageText.gameObject.SetActive(true);
        currentClickMessageRoutine = StartCoroutine(HideClickMessageRoutine(clickMessageDisplayDuration));
    }

    public void ClaimNormalAfkReward()
    {
        // Выдаем обычную награду
        Gold += _pendingAfkGold;
        Wood += _pendingAfkWood;
        Iron += _pendingAfkIron;
        Stone += _pendingAfkStone;

        UpdateResourceUI();

        // Сохраняем сразу, чтобы не потерять
        if (CloudSaveManager.Instance != null) CloudSaveManager.Instance.SaveToCloud();

        CloseAfkPanel();
    }

    public void CloseAfkPanel()
    {
        if (afkPanel != null)
        {
            afkPanel.SetActive(false);
        }
    }

    private IEnumerator HideClickMessageRoutine(float delay) { yield return new WaitForSeconds(delay); if (clickUpgradeMessageText != null) clickUpgradeMessageText.gameObject.SetActive(false); currentClickMessageRoutine = null; }
}