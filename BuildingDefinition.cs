// Assets/Scripts/BuildingDefinitions/BuildingDefinition.cs
using UnityEngine;

// НОВОЕ: Enum для типов зданий
public enum BuildingType
{
    Generic, // Общий тип, если не указано иное
    House,
    LumberMill,
    Mine,
    Quarry
}

// Позволяет создавать ассеты этого типа через меню Assets/Create
[CreateAssetMenu(fileName = "NewBuildingDefinition", menuName = "Game/Building Definition", order = 1)]
public class BuildingDefinition : ScriptableObject
{
    [Header("Basic Info")]
    public BuildingType buildingType = BuildingType.Generic; // НОВОЕ: Тип здания (назначить в Инспекторе!)
    public string buildingName; // Название здания, например, "Лесопилка", "Шахта", "Дом"
    public string buildingDescription; // Краткое описание
    public Sprite buildingSprite; // Спрайт для отображения здания (на будущее)

    [Header("Upgrade Costs (Base)")]
    public long baseGoldCost;
    public int baseWoodCost;
    public int baseIronCost;
    public int baseStoneCost;
    public int basePeopleCost; // Это поле будет использоваться для всех зданий, КРОМЕ ДОМА (если у Дома заданы House Specific Costs)

    public float costMultiplier = 1.2f; // Множитель стоимости апгрейда за уровень

    [Header("Passive Income Per Level")]
    public float goldPerSecondIncrease = 0f;
    public float woodPerSecondIncrease = 0f;
    public float ironPerSecondIncrease = 0f;
    public float stonePerSecondIncrease = 0f;
    public float peoplePerSecondIncrease = 0f; // Увеличение прироста людей (если здание генерирует людей)

    [Header("Capacity Increase Per Level")]
    public int maxPeopleIncrease = 0; // Увеличение максимального количества людей (для домов)

    [Header("Click Upgrade Cap")]
    public int clickLevelCapPerBuildingLevel = 5; // Например, каждый уровень здания разблокирует 5 уровней клика

    // НОВОЕ: Отдельные настройки для стоимости людей при апгрейде ДОМА
    // Эти поля будут иметь значение ТОЛЬКО для HouseDefinition
    [Header("House Specific People Cost (if applicable)")]
    public int housePeopleBasePeopleCost = 0; // Базовая стоимость людей для Дома (например, 10 или 20)
    public int housePeopleCostLinearIncrease = 0; // Насколько линейно увеличивается стоимость людей за каждый уровень Дома (например, 5 или 6)
}
