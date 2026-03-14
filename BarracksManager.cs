using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class BarracksManager : MonoBehaviour
{
    public static BarracksManager Instance { get; private set; }

    [Header("Soldier Definitions")]
    public SoldierDefinition swordsmanDef;
    public SoldierDefinition archerDef;
    public SoldierDefinition shieldbearerDef;

    [Header("UI References - Barracks")]
    public TextMeshProUGUI swordsmanCostText;
    public TextMeshProUGUI archerCostText;
    public TextMeshProUGUI shieldbearerCostText;
    public Button trainSwordsmanButton;
    public Button trainArcherButton;
    public Button trainShieldbearerButton;

    public TextMeshProUGUI swordsmanStatsText;
    public TextMeshProUGUI archerStatsText;
    public TextMeshProUGUI shieldbearerStatsText;
    public TextMeshProUGUI insufficientResourcesText;

    [Header("Message Settings")]
    public float messageDisplayDuration = 2.0f;
    private Coroutine currentMessageRoutine;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        UpdateTrainButtonsUI();
    }

    public void TrainSwordsman()
    {
        if (swordsmanDef == null) return;

        bool canAffordAll = GameManager.Instance.CanAffordGold(swordsmanDef.goldCost) &&
                            GameManager.Instance.CanAffordPeople(swordsmanDef.peopleCost) &&
                            GameManager.Instance.CanAffordIron(swordsmanDef.ironCost) &&
                            GameManager.Instance.CanAffordWood(swordsmanDef.woodCost) &&
                            GameManager.Instance.CanAffordStone(swordsmanDef.stoneCost);

        if (canAffordAll)
        {
            GameManager.Instance.TrySpendGold(swordsmanDef.goldCost);
            GameManager.Instance.TrySpendPeople(swordsmanDef.peopleCost);
            GameManager.Instance.TrySpendIron(swordsmanDef.ironCost);
            GameManager.Instance.TrySpendWood(swordsmanDef.woodCost);
            GameManager.Instance.TrySpendStone(swordsmanDef.stoneCost);

            GameManager.Instance.SwordsmenCount++;
            GameManager.Instance.UpdateTroopUI();
            UpdateTrainButtonsUI();

            // СОХРАНЕНИЕ В ОБЛАКО
            if (CloudSaveManager.Instance != null) CloudSaveManager.Instance.SaveToCloud();
            DailyMissionManager.Instance.ProgressMission(DailyMissionType.HireSwordsman, 1);
        }
        else { ShowInsufficientResourcesMessage("Not enough resources for Swordsman!"); }
    }

    public void TrainArcher()
    {
        if (archerDef == null) return;

        bool canAffordAll = GameManager.Instance.CanAffordGold(archerDef.goldCost) &&
                            GameManager.Instance.CanAffordPeople(archerDef.peopleCost) &&
                            GameManager.Instance.CanAffordIron(archerDef.ironCost) &&
                            GameManager.Instance.CanAffordWood(archerDef.woodCost) &&
                            GameManager.Instance.CanAffordStone(archerDef.stoneCost);

        if (canAffordAll)
        {
            GameManager.Instance.TrySpendGold(archerDef.goldCost);
            GameManager.Instance.TrySpendPeople(archerDef.peopleCost);
            GameManager.Instance.TrySpendIron(archerDef.ironCost);
            GameManager.Instance.TrySpendWood(archerDef.woodCost);
            GameManager.Instance.TrySpendStone(archerDef.stoneCost);

            GameManager.Instance.ArchersCount++;
            GameManager.Instance.UpdateTroopUI();
            UpdateTrainButtonsUI();

            // СОХРАНЕНИЕ В ОБЛАКО
            if (CloudSaveManager.Instance != null) CloudSaveManager.Instance.SaveToCloud();
            DailyMissionManager.Instance.ProgressMission(DailyMissionType.HireArcher, 1);
        }
        else { ShowInsufficientResourcesMessage("Not enough resources for Archer!"); }
    }

    public void TrainShieldbearer()
    {
        if (shieldbearerDef == null) return;

        bool canAffordAll = GameManager.Instance.CanAffordGold(shieldbearerDef.goldCost) &&
                            GameManager.Instance.CanAffordPeople(shieldbearerDef.peopleCost) &&
                            GameManager.Instance.CanAffordIron(shieldbearerDef.ironCost) &&
                            GameManager.Instance.CanAffordWood(shieldbearerDef.woodCost) &&
                            GameManager.Instance.CanAffordStone(shieldbearerDef.stoneCost);

        if (canAffordAll)
        {
            GameManager.Instance.TrySpendGold(shieldbearerDef.goldCost);
            GameManager.Instance.TrySpendPeople(shieldbearerDef.peopleCost);
            GameManager.Instance.TrySpendIron(shieldbearerDef.ironCost);
            GameManager.Instance.TrySpendWood(shieldbearerDef.woodCost);
            GameManager.Instance.TrySpendStone(shieldbearerDef.stoneCost);

            GameManager.Instance.ShieldbearersCount++;
            GameManager.Instance.UpdateTroopUI();
            UpdateTrainButtonsUI();

            // СОХРАНЕНИЕ В ОБЛАКО
            if (CloudSaveManager.Instance != null) CloudSaveManager.Instance.SaveToCloud();
            DailyMissionManager.Instance.ProgressMission(DailyMissionType.HireShieldbearer, 1);
        }
        else { ShowInsufficientResourcesMessage("Not enough resources for Shieldbearer!"); }
    }

    public void UpdateTrainButtonsUI()
    {
        if (swordsmanDef != null && swordsmanCostText != null)
        {
            swordsmanCostText.text = $"Cost: {swordsmanDef.goldCost:N0}G, {swordsmanDef.peopleCost}P, {swordsmanDef.ironCost}I";
            if (swordsmanStatsText != null) swordsmanStatsText.text = $"ATK: {swordsmanDef.attack} HP: {swordsmanDef.health} DEF: {swordsmanDef.defense}";
        }
        if (archerDef != null && archerCostText != null)
        {
            archerCostText.text = $"Cost: {archerDef.goldCost:N0}G, {archerDef.peopleCost}P, {archerDef.woodCost}W";
            if (archerStatsText != null) archerStatsText.text = $"ATK: {archerDef.attack} HP: {archerDef.health} DEF: {archerDef.defense}";
        }
        if (shieldbearerDef != null && shieldbearerCostText != null)
        {
            shieldbearerCostText.text = $"Cost: {shieldbearerDef.goldCost:N0}G, {shieldbearerDef.peopleCost}P, {shieldbearerDef.ironCost}I, {shieldbearerDef.stoneCost}S";
            if (shieldbearerStatsText != null) shieldbearerStatsText.text = $"ATK: {shieldbearerDef.attack} HP: {shieldbearerDef.health} DEF: {shieldbearerDef.defense}";
        }
    }

    public void ShowInsufficientResourcesMessage(string message)
    {
        if (insufficientResourcesText == null) return;
        if (currentMessageRoutine != null) StopCoroutine(currentMessageRoutine);
        insufficientResourcesText.text = message; insufficientResourcesText.gameObject.SetActive(true);
        currentMessageRoutine = StartCoroutine(HideMessageRoutine(messageDisplayDuration));
    }

    private IEnumerator HideMessageRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (insufficientResourcesText != null) insufficientResourcesText.gameObject.SetActive(false);
        currentMessageRoutine = null;
    }
}