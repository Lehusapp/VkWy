using System;
using System.Collections.Generic;

[Serializable]
public class HeroSaveData
{
    public string heroTypeName;
    public string name;
    public int level;
    public long currentXP;
    public int currentFragments;
}

[Serializable]
public class DailyMissionSaveData
{
    public int missionIndex;
    public int progress;
    public bool isClaimed;
}

// --- НОВЫЙ КЛАСС ДЛЯ СОХРАНЕНИЯ ЭКСПЕДИЦИИ ---
[Serializable]
public class ExpeditionSaveData
{
    public bool isActive;
    public string startTime;        // Время начала (ISO формат)
    public float duration;         // Длительность в секундах
    public string heroTypeName;    // Какой герой ушел (если был)
    public int swordsmen;
    public int archers;
    public int shieldbearers;
    public float playerPower;
    public float enemyPower;
    public int missionIndex;       // Индекс миссии в списке всех миссий
}

[Serializable]
public class GameData
{
    // Ресурсы
    public long gold;
    public int wood;
    public int iron;
    public int stone;
    public int people;

    // Здания
    public int lumberMillLvl;
    public int mineLvl;
    public int quarryLvl;
    public int houseLvl;

    // Уровни кликов
    public int goldClickLvl;
    public int woodClickLvl;
    public int ironClickLvl;
    public int stoneClickLvl;

    // Армия
    public int swordsmenCount;
    public int archersCount;
    public int shieldbearersCount;

    // Герои
    public List<HeroSaveData> heroes = new List<HeroSaveData>();

    // Прогресс подземелья
    public int bossCounter;
    public int completedMissionsCounter; // Счетчик для босса
    public float threat;

    // --- ЭКСПЕДИЦИЯ ---
    public ExpeditionSaveData activeExpedition = new ExpeditionSaveData();

    // Лимит найма людей
    public int currentDayHires;

    // Время выхода (для AFK дохода)
    public string lastExitTime;

    // Список текущих заданий на сегодня
    public List<DailyMissionSaveData> currentDailyMissions = new List<DailyMissionSaveData>();

    // Индексы текущих доступных миссий (чтобы не обновлялись при F5)
    public List<int> currentAvailableMissionIndices = new List<int>();
}
