using UnityEngine;

// Определяем типы заданий (enum)
public enum DailyMissionType
{
    // Клики
    ClickGold, ClickWood, ClickIron, ClickStone,

    // Армия (нанять солдат)
    HireSwordsman, HireArcher, HireShieldbearer,

    // Здания (улучшить)
    UpgradeLumberMill, UpgradeMine, UpgradeQuarry, UpgradeHouse,

    // Подземелье и Боссы
    FinishExpedition,
    BossVictory
}

[CreateAssetMenu(fileName = "NewDailyMission", menuName = "Game/Daily Mission")]
public class DailyMissionDefinition : ScriptableObject
{
    [Header("Main Settings")]
    public DailyMissionType missionType; // Тот самый тип, который ищет менеджер

    [TextArea]
    public string description; // Описание (например: "Mine 100 Iron Ore")

    [Header("Requirements & Rewards")]
    public int requiredAmount; // Сколько нужно сделать (например: 100)
    public long goldReward;    // Награда золотом
}
