using UnityEngine;

public enum RewardType { Gold, Wood, Iron, Stone, HeroFragment, FullPopulation }

[CreateAssetMenu(fileName = "NewDailyReward", menuName = "Game/Daily Reward")]
public class DailyRewardDefinition : ScriptableObject
{
    public RewardType type;
    public string rewardName;
    public int amount;
    public Sprite icon;
}