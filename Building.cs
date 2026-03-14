// Assets/Scripts/Building.cs
using UnityEngine; // Для Debug.Log и Mathf

public class Building
{
    public BuildingDefinition Definition { get; private set; } // Ссылка на определение здания
    public int Level { get; private set; } // Текущий уровень здания

    public Building(BuildingDefinition definition)
    {
        Definition = definition;
        Level = 0; // Все здания начинаются с уровня 0
    }

    // Методы для расчета стоимости следующего апгрейда (ресурсы кроме людей)
    public long GetNextUpgradeGoldCost() => (long)(Definition.baseGoldCost * Mathf.Pow(Definition.costMultiplier, Level));
    public int GetNextUpgradeWoodCost() => (int)(Definition.baseWoodCost * Mathf.Pow(Definition.costMultiplier, Level));
    public int GetNextUpgradeIronCost() => (int)(Definition.baseIronCost * Mathf.Pow(Definition.costMultiplier, Level));
    public int GetNextUpgradeStoneCost() => (int)(Definition.baseStoneCost * Mathf.Pow(Definition.costMultiplier, Level));
    
    // ИЗМЕНЕНО: Метод для расчета стоимости людей для следующего апгрейда
    public int GetNextUpgradePeopleCost()
    {
        // Специальная логика для Дома, чтобы его стоимость людей росла линейно
        if (Definition != null && Definition.buildingType == BuildingType.House)
        {
            // Используем линейную формулу для Дома
            // Level+1 потому что считаем стоимость для СЛЕДУЮЩЕГО уровня
            return Definition.housePeopleBasePeopleCost + (Definition.housePeopleCostLinearIncrease * (Level + 1));
        }
        else
        {
            // Для всех остальных зданий (или если у Дома не заданы специальные стоимости), используем стандартную экспоненциальную формулу
            return (int)(Definition.basePeopleCost * Mathf.Pow(Definition.costMultiplier, Level));
        }
    }

    // Метод для повышения уровня здания
    public bool Upgrade()
    {
        Level++;
        Debug.Log($"{Definition.buildingName} upgraded to Level {Level}!");
        return true;
    }

    // Методы для получения текущих бонусов от этого уровня здания
    public float GetCurrentGoldPerSecond() => Definition.goldPerSecondIncrease * Level;
    public float GetCurrentWoodPerSecond() => Definition.woodPerSecondIncrease * Level;
    public float GetCurrentIronPerSecond() => Definition.ironPerSecondIncrease * Level;
    public float GetCurrentStonePerSecond() => Definition.stonePerSecondIncrease * Level;
    public float GetCurrentPeoplePerSecond() => Definition.peoplePerSecondIncrease * Level;
    public int GetCurrentMaxPeopleIncrease() => Definition.maxPeopleIncrease * Level;

    // Метод для получения текущей силы клика от этого здания
    public int GetCurrentClickPower()
    {
        if (Definition != null)
        {
            return Definition.clickLevelCapPerBuildingLevel * Level;
        }
        return 0;
    }

    public void LoadLevel(int level)
    {
        // Мы просто напрямую меняем уровень, который загрузили из облака
        // Убедись, что поле называется именно Level (с большой буквы), как в твоем коде апгрейдов
        this.Level = level;
    }

}
