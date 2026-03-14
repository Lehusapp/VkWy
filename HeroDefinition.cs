using UnityEngine;

[CreateAssetMenu(fileName = "NewHeroDefinition", menuName = "Game/Hero Definition", order = 1)]
public class HeroDefinition : ScriptableObject
{
    [Header("Basic Info")]
    public string heroTypeName;
    public string heroDescription;
    public Sprite heroPortrait;

    [Header("Base Stats at Level 1")]
    public int baseAttack = 10;
    public int baseHealth = 100;
    public int baseDefense = 5;

    [Header("Growth Per Level")]
    public int attackGrowthPerLevel = 2;
    public int healthGrowthPerLevel = 10;
    public int defenseGrowthPerLevel = 1;

    [Header("Transcendence Settings")]
    public int transcendenceInterval = 10;
    public int baseFragmentsRequired = 10;
    public float fragmentRequirementMultiplier = 1.5f;
    public long goldCostForTranscendence = 1000;
    public float transcendencePower = 2.0f;

    [Header("Hire Cost")]
    public long baseHireGoldCost = 500;
    public int baseHirePeopleCost = 5;

    [Header("Dungeon Bonus")]
    public float xpBonusPerLevel = 0.1f;
}