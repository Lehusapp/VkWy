using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    [Header("Building Definitions")]
    public BuildingDefinition lumberMillDef;
    public BuildingDefinition mineDef;
    public BuildingDefinition quarryDef;
    public BuildingDefinition houseDef;

    [Header("UI References - Buildings")]
    public GameObject buildingUIEntryPrefab;
    public Transform buildingContainerParent;

    [Header("Message Settings")]
    public TextMeshProUGUI insufficientResourcesMessageText;
    public float messageDisplayDuration = 2.0f;
    private Coroutine currentMessageRoutine;

    // Ссылки на созданные экземпляры зданий
    public Building LumberMill => _lumberMill;
    public Building Mine => _mine;
    public Building Quarry => _quarry;
    public Building House => _house;

    private Building _lumberMill;
    private Building _mine;
    private Building _quarry;
    private Building _house;

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

    void Start()
    {
        _lumberMill = new Building(lumberMillDef);
        _mine = new Building(mineDef);
        _quarry = new Building(quarryDef);
        _house = new Building(houseDef);

        UpdateBuildingUI();
    }

    public void UpgradeBuilding(Building buildingToUpgrade)
    {
        if (buildingToUpgrade == null)
        {
            Debug.LogError("Attempted to upgrade a null building!");
            return;
        }

        long goldCost = buildingToUpgrade.GetNextUpgradeGoldCost();
        int woodCost = buildingToUpgrade.GetNextUpgradeWoodCost();
        int ironCost = buildingToUpgrade.GetNextUpgradeIronCost();
        int stoneCost = buildingToUpgrade.GetNextUpgradeStoneCost();
        int peopleCost = buildingToUpgrade.GetNextUpgradePeopleCost();

        bool canAffordAll = GameManager.Instance.CanAffordGold(goldCost) &&
                            GameManager.Instance.CanAffordWood(woodCost) &&
                            GameManager.Instance.CanAffordIron(ironCost) &&
                            GameManager.Instance.CanAffordStone(stoneCost) &&
                            GameManager.Instance.CanAffordPeople(peopleCost);

        if (canAffordAll)
        {
            GameManager.Instance.TrySpendGold(goldCost);
            GameManager.Instance.TrySpendWood(woodCost);
            GameManager.Instance.TrySpendIron(ironCost);
            GameManager.Instance.TrySpendStone(stoneCost);
            GameManager.Instance.TrySpendPeople(peopleCost);

            if (buildingToUpgrade.Upgrade())
            {
                // --- НОВАЯ ЛОГИКА: МГНОВЕННЫЙ ПРИРОСТ НАСЕЛЕНИЯ ПРИ УЛУЧШЕНИИ ДОМА ---
                if (buildingToUpgrade == _house)
                {
                    // Прогрессивная формула: Уровень * 2 (например, на 5 уровне придет 10 чел.)
                    int bonusPeople = buildingToUpgrade.Level * 2;

                    // Сначала пересчитываем лимит (MaxPeople), чтобы новые жители влезли
                    GameManager.Instance.RecalculateMaxPeople();

                    // Добавляем людей
                    GameManager.Instance.People = Mathf.Min(GameManager.Instance.People + bonusPeople, GameManager.Instance.MaxPeople);
                    Debug.Log($"<color=cyan>[House Upgrade] +{bonusPeople} inhabitants arrived!</color>");
                }
                // ---------------------------------------------------------------------

                GameManager.Instance.RecalculatePassiveIncome();
                GameManager.Instance.RecalculateMaxPeople(); // Повторный вызов на случайвлияния других зданий
                GameManager.Instance.RecalculateMaxClickLevels();
                UpdateBuildingUI();
                GameManager.Instance.UpdateResourceUI();

                if (DailyMissionManager.Instance != null)
                {
                    if (buildingToUpgrade == _lumberMill)
                        DailyMissionManager.Instance.ProgressMission(DailyMissionType.UpgradeLumberMill, 1);
                    else if (buildingToUpgrade == _mine)
                        DailyMissionManager.Instance.ProgressMission(DailyMissionType.UpgradeMine, 1);
                    else if (buildingToUpgrade == _quarry)
                        DailyMissionManager.Instance.ProgressMission(DailyMissionType.UpgradeQuarry, 1);
                    else if (buildingToUpgrade == _house)
                        DailyMissionManager.Instance.ProgressMission(DailyMissionType.UpgradeHouse, 1);
                }

                if (CloudSaveManager.Instance != null)
                {
                    CloudSaveManager.Instance.SaveToCloud();
                }
            }
        }
        else
        {
            ShowInsufficientResourcesMessage($"Not enough resources to upgrade {buildingToUpgrade.Definition.buildingName} to Level {buildingToUpgrade.Level + 1}!");
        }
    }


    public void UpdateBuildingUI()
    {
        foreach (Transform child in buildingContainerParent)
        {
            Destroy(child.gameObject);
        }

        CreateOrUpdateBuildingEntry(_lumberMill);
        CreateOrUpdateBuildingEntry(_mine);
        CreateOrUpdateBuildingEntry(_quarry);
        CreateOrUpdateBuildingEntry(_house);
    }

    private void CreateOrUpdateBuildingEntry(Building building)
    {
        if (building == null || building.Definition == null) return;

        GameObject entryGO = Instantiate(buildingUIEntryPrefab, buildingContainerParent);

        TextMeshProUGUI nameText = entryGO.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI statsText = entryGO.transform.Find("StatsText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI costText = entryGO.transform.Find("CostText")?.GetComponent<TextMeshProUGUI>();
        Button upgradeButton = entryGO.transform.Find("UpgradeButton")?.GetComponent<Button>();
        Image buildingSpriteImage = entryGO.transform.Find("BuildingSpriteImage")?.GetComponent<Image>();

        if (nameText != null) nameText.text = $"{building.Definition.buildingName} (Lvl. {building.Level})";

        string stats = "";
        if (building.Definition.goldPerSecondIncrease > 0) stats += $"+{building.GetCurrentGoldPerSecond():F2} G/sec\n";
        if (building.Definition.woodPerSecondIncrease > 0) stats += $"+{building.GetCurrentWoodPerSecond():F2} W/sec\n";
        if (building.Definition.ironPerSecondIncrease > 0) stats += $"+{building.GetCurrentIronPerSecond():F2} I/sec\n";
        if (building.Definition.stonePerSecondIncrease > 0) stats += $"+{building.GetCurrentStonePerSecond():F2} S/sec\n";
        if (building.Definition.peoplePerSecondIncrease > 0) stats += $"+{building.GetCurrentPeoplePerSecond():F2} P/sec\n";
        if (building.Definition.maxPeopleIncrease > 0) stats += $"+{building.GetCurrentMaxPeopleIncrease()} Max.P\n";

        // --- ДОБАВЛЕНО: ИНФОРМАЦИЯ О СЛЕДУЮЩЕМ БОНУСЕ ЖИТЕЛЕЙ (ТОЛЬКО ДЛЯ ДОМА) ---
        if (building == _house)
        {
            int nextInhabitantsBonus = (building.Level + 1) * 2;
            stats += $"Next Lvl: +{nextInhabitantsBonus} People</color>";
        }
        // -------------------------------------------------------------------------

        if (statsText != null) statsText.text = stats.Trim();

        long nextGoldCost = building.GetNextUpgradeGoldCost();
        int nextWoodCost = building.GetNextUpgradeWoodCost();
        int nextIronCost = building.GetNextUpgradeIronCost();
        int nextStoneCost = building.GetNextUpgradeStoneCost();
        int nextPeopleCost = building.GetNextUpgradePeopleCost();

        string costString = $"Cost Lvl.{building.Level + 1}:\n";
        if (nextGoldCost > 0) costString += $"{nextGoldCost.ToString("N0")} G, ";
        if (nextWoodCost > 0) costString += $"{nextWoodCost} W, ";
        if (nextIronCost > 0) costString += $"{nextIronCost} I, ";
        if (nextStoneCost > 0) costString += $"{nextStoneCost} S, ";
        if (nextPeopleCost > 0) costString += $"{nextPeopleCost} P";
        costString = costString.TrimEnd(' ', ',');

        if (costText != null) costText.text = costString;

        if (buildingSpriteImage != null && building.Definition.buildingSprite != null)
            buildingSpriteImage.sprite = building.Definition.buildingSprite;

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(() => UpgradeBuilding(building));
        }
    }

    public float GetTotalGoldIncomePerSecond()
    {
        float total = 0;
        if (_lumberMill != null) total += _lumberMill.GetCurrentGoldPerSecond();
        if (_mine != null) total += _mine.GetCurrentGoldPerSecond();
        if (_quarry != null) total += _quarry.GetCurrentGoldPerSecond();
        if (_house != null) total += _house.GetCurrentGoldPerSecond();
        return total;
    }

    public float GetTotalWoodIncomePerSecond()
    {
        float total = 0;
        if (_lumberMill != null) total += _lumberMill.GetCurrentWoodPerSecond();
        if (_mine != null) total += _mine.GetCurrentWoodPerSecond();
        if (_quarry != null) total += _quarry.GetCurrentWoodPerSecond();
        if (_house != null) total += _house.GetCurrentWoodPerSecond();
        return total;
    }

    public float GetTotalIronIncomePerSecond()
    {
        float total = 0;
        if (_lumberMill != null) total += _lumberMill.GetCurrentIronPerSecond(); // Была опечатка в оригинале (_mine), исправил для надежности
        if (_mine != null) total += _mine.GetCurrentIronPerSecond();
        if (_quarry != null) total += _quarry.GetCurrentIronPerSecond();
        if (_house != null) total += _house.GetCurrentIronPerSecond();
        return total;
    }

    public float GetTotalStoneIncomePerSecond()
    {
        float total = 0;
        if (_lumberMill != null) total += _lumberMill.GetCurrentStonePerSecond();
        if (_mine != null) total += _mine.GetCurrentStonePerSecond();
        if (_quarry != null) total += _quarry.GetCurrentStonePerSecond();
        if (_house != null) total += _house.GetCurrentStonePerSecond();
        return total;
    }

    public void ShowInsufficientResourcesMessage(string message)
    {
        if (insufficientResourcesMessageText == null) return;
        if (currentMessageRoutine != null) StopCoroutine(currentMessageRoutine);
        insufficientResourcesMessageText.text = message;
        insufficientResourcesMessageText.gameObject.SetActive(true);
        currentMessageRoutine = StartCoroutine(HideMessageRoutine(messageDisplayDuration));
    }

    private IEnumerator HideMessageRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (insufficientResourcesMessageText != null) insufficientResourcesMessageText.gameObject.SetActive(false);
        currentMessageRoutine = null;
    }
}