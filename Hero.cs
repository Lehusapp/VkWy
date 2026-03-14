using UnityEngine;

public class Hero
{
    public HeroDefinition Definition { get; private set; }
    public string Name { get; private set; }
    public int Level { get; private set; }
    public long CurrentXP { get; private set; }
    public long XPToNextLevel { get; private set; }
    public int CurrentFragments { get; private set; }
    public bool IsOnExpedition { get; private set; }

    public int CurrentAttack { get; private set; }
    public int CurrentHealth { get; private set; }
    public int CurrentDefense { get; private set; }

    public bool IsMaxLevelForTier => Level > 0 && Level % Definition.transcendenceInterval == 0;
    public int FragmentsRequiredForTranscendence => CalculateFragmentsRequired(Level / Definition.transcendenceInterval);

    public Hero(HeroDefinition definition, string uniqueName)
    {
        Definition = definition;
        Name = uniqueName;
        Level = 1;
        CurrentXP = 0;
        CurrentFragments = 0;
        UpdateStats();
    }

    // МЕТОД ДЛЯ ЗАГРУЗКИ ИЗ ОБЛАКА
    public void LoadHeroData(int loadedLevel, long loadedXP, int loadedFragments)
    {
        this.Level = loadedLevel;
        this.CurrentXP = loadedXP;
        this.CurrentFragments = loadedFragments;
        UpdateStats();
    }

    private void UpdateStats()
    {
        CurrentAttack = Definition.baseAttack + (Definition.attackGrowthPerLevel * (Level - 1));
        CurrentHealth = Definition.baseHealth + (Definition.healthGrowthPerLevel * (Level - 1));
        CurrentDefense = Definition.baseDefense + (Definition.defenseGrowthPerLevel * (Level - 1));
        XPToNextLevel = CalculateXPToNextLevel(Level);
    }

    public void SetExpeditionStatus(bool status) => IsOnExpedition = status;

    public void LevelUp()
    {
        Level++;
        UpdateStats();
        Debug.Log($"{Name} leveled up to {Level}!");
    }

    public void GainXP(long amount)
    {
        CurrentXP += amount;
        while (CurrentXP >= XPToNextLevel)
        {
            if (IsMaxLevelForTier) break;
            CurrentXP -= XPToNextLevel;
            LevelUp();
        }
    }

    public void GainFragments(int amount) => CurrentFragments += amount;

    public bool Transcend()
    {
        if (!IsMaxLevelForTier || CurrentFragments < FragmentsRequiredForTranscendence) return false;
        if (!GameManager.Instance.TrySpendGold(Definition.goldCostForTranscendence)) return false;

        CurrentFragments -= FragmentsRequiredForTranscendence;
        long excessXP = CurrentXP;
        Level++;
        CurrentXP = 0;
        UpdateStats();
        GainXP(excessXP);

        // Сохраняем после важного события
        if (CloudSaveManager.Instance != null) CloudSaveManager.Instance.SaveToCloud();
        return true;
    }

    private long CalculateXPToNextLevel(int currentLevel)
    {
        int transcendenceTier = (currentLevel - 1) / Definition.transcendenceInterval + 1;
        return 100L * currentLevel * (long)Mathf.Pow(transcendenceTier, Definition.transcendencePower);
    }

    private int CalculateFragmentsRequired(int transcendenceCount)
    {
        if (transcendenceCount <= 0) return 0;
        return (int)(Definition.baseFragmentsRequired * Mathf.Pow(Definition.fragmentRequirementMultiplier, transcendenceCount - 1));
    }
}