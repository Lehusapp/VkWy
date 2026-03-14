 // Assets/Scripts/SoldierDefinitions/SoldierDefinition.cs
    using UnityEngine;

    [CreateAssetMenu(fileName = "NewSoldierDefinition", menuName = "Game/Soldier Definition", order = 1)]
    public class SoldierDefinition : ScriptableObject
    {
        [Header("Basic Info")]
        public string soldierTypeName; // Например, "Swordsman", "Archer", "Shieldbearer"
        public string soldierDescription; // Краткое описание роли солдата
        public Sprite soldierSprite; // Спрайт для отображения солдата (на будущее)

        [Header("Base Stats")] // Солдаты не имеют уровней, это их постоянные статы
        public int attack;
        public int health;
        public int defense;

        [Header("Training Cost")]
        public long goldCost;
        public int peopleCost;
        public int woodCost; // Дополнительные ресурсы для некоторых типов солдат
        public int ironCost; // Дополнительные ресурсы для некоторых типов солдат
        public int stoneCost; // Дополнительные ресурсы для некоторых типов солдат
    }