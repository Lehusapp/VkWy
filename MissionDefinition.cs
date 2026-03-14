using UnityEngine;

[CreateAssetMenu(fileName = "NewMission", menuName = "Game/Mission Definition")]
public class MissionDefinition : ScriptableObject
{
    [Header("Basic Info")]
    public string missionName;
    [TextArea] public string description;
    public Sprite missionIcon;

    [Header("Difficulty")]
    public float enemyPowerBase = 100f; // Базовая сила врагов для этой миссии
    public float threatMultiplier = 1.0f; // Насколько сильно угроза влияет на сложность здесь

    [Header("Rewards")]
    public long goldReward = 100;
    public int fragmentReward = 1;
    public long xpReward = 50;

    [Header("Settings")]
    public float durationSeconds = 30f; // Сколько длится поход
}
