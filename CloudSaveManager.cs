using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using GamePush;

public class CloudSaveManager : MonoBehaviour
{
    public static CloudSaveManager Instance { get; private set; }
    public GameData _gameData = new GameData();

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        StartCoroutine(WaitAndLoad());

        // ВКЛЮЧАЕМ АВТОМАТИЧЕСКУЮ СИНХРОНИЗАЦИЮ ГЕЙМПУШ (каждые 30 сек)
        GP_Player.EnableAutoSync(30, SyncStorageType.cloud);
    }

    private IEnumerator WaitAndLoad()
    {
        Debug.Log("<color=white>Waiting for SDK...</color>");
        while (!GP_Init.isReady) yield return null;

        Debug.Log("<color=white>SDK Ready. Calling Player Load...</color>");
        GP_Player.Load();
        yield return new WaitForSeconds(2.0f);

        Debug.Log("<color=red>!!!!! STARTING LOAD FROM CLOUD !!!!!</color>");
        LoadFromCloud();
        GP_Game.GameReady();
    }

    public void SaveToCloud()
    {
        // 1. СОБИРАЕМ ВСЕ ДАННЫЕ
        _gameData.gold = GameManager.Instance.Gold;
        _gameData.wood = GameManager.Instance.Wood;
        _gameData.iron = GameManager.Instance.Iron;
        _gameData.stone = GameManager.Instance.Stone;
        _gameData.people = GameManager.Instance.People;

        _gameData.lumberMillLvl = BuildingManager.Instance.LumberMill.Level;
        _gameData.mineLvl = BuildingManager.Instance.Mine.Level;
        _gameData.quarryLvl = BuildingManager.Instance.Quarry.Level;
        _gameData.houseLvl = BuildingManager.Instance.House.Level;

        _gameData.goldClickLvl = GameManager.Instance.goldClickUpgradeLevel;
        _gameData.woodClickLvl = GameManager.Instance.woodClickUpgradeLevel;
        _gameData.ironClickLvl = GameManager.Instance.ironClickUpgradeLevel;
        _gameData.stoneClickLvl = GameManager.Instance.stoneClickUpgradeLevel;
        _gameData.swordsmenCount = GameManager.Instance.SwordsmenCount;
        _gameData.archersCount = GameManager.Instance.ArchersCount;
        _gameData.shieldbearersCount = GameManager.Instance.ShieldbearersCount;

        _gameData.completedMissionsCounter = DungeonManager.Instance.completedMissionsCounter;
        _gameData.threat = DungeonManager.Instance.threatLevel;

        _gameData.heroes.Clear();
        foreach (var hero in HeroManager.Instance.hiredHeroes)
        {
            _gameData.heroes.Add(new HeroSaveData
            {
                heroTypeName = hero.Definition.heroTypeName,
                name = hero.Name,
                level = hero.Level,
                currentXP = hero.CurrentXP,
                currentFragments = hero.CurrentFragments
            });
        }

        if (DailyMissionManager.Instance != null)
            _gameData.currentDailyMissions = DailyMissionManager.Instance.GetCurrentMissionsData();

        _gameData.lastExitTime = DateTime.Now.ToBinary().ToString();

        // 2. ОТПРАВЛЯЕМ В ГЕЙМПУШ (AutoSync синхронизирует это само)
        string json = JsonUtility.ToJson(_gameData);
        GP_Player.Set("SaveSlot1", json);

        if (GameManager.Instance != null) GameManager.Instance.SyncTotalPowerToLeaderboard();

        // Принудительно вызываем Sync только в критических местах (например, закрытие окна)
        GP_Player.Sync();
        Debug.Log("Prepared data for Cloud Sync: " + json);
    }

    public void LoadFromCloud()
    {
        if (GP_Player.Has("SaveSlot1"))
        {
            string json = GP_Player.GetString("SaveSlot1");
            _gameData = JsonUtility.FromJson<GameData>(json);

            // ПРИМЕНЯЕМ РЕСУРСЫ
            GameManager.Instance.Gold = _gameData.gold;
            GameManager.Instance.Wood = _gameData.wood;
            GameManager.Instance.Iron = _gameData.iron;
            GameManager.Instance.Stone = _gameData.stone;
            GameManager.Instance.People = _gameData.people;

            // ЗДАНИЯ И АПГРЕЙДЫ
            BuildingManager.Instance.LumberMill.LoadLevel(_gameData.lumberMillLvl);
            BuildingManager.Instance.Mine.LoadLevel(_gameData.mineLvl);
            BuildingManager.Instance.Quarry.LoadLevel(_gameData.quarryLvl);
            BuildingManager.Instance.House.LoadLevel(_gameData.houseLvl);

            GameManager.Instance.goldClickUpgradeLevel = _gameData.goldClickLvl;
            GameManager.Instance.woodClickUpgradeLevel = _gameData.woodClickLvl;
            GameManager.Instance.ironClickUpgradeLevel = _gameData.ironClickLvl;
            GameManager.Instance.stoneClickUpgradeLevel = _gameData.stoneClickLvl;
            GameManager.Instance.SwordsmenCount = _gameData.swordsmenCount;
            GameManager.Instance.ArchersCount = _gameData.archersCount;
            GameManager.Instance.ShieldbearersCount = _gameData.shieldbearersCount;

            // ПОДЗЕМЕЛЬЕ
            DungeonManager.Instance.completedMissionsCounter = _gameData.completedMissionsCounter;
            DungeonManager.Instance.threatLevel = _gameData.threat;

            // ГЕРОИ
            HeroManager.Instance.hiredHeroes.Clear();
            foreach (var hData in _gameData.heroes)
            {
                HeroDefinition def = HeroManager.Instance.availableHeroDefinitions.Find(d => d.heroTypeName == hData.heroTypeName);
                if (def != null)
                {
                    Hero h = new Hero(def, hData.name);
                    h.LoadHeroData(hData.level, hData.currentXP, hData.currentFragments);
                    HeroManager.Instance.hiredHeroes.Add(h);
                }
            }

            // ЕЖЕДНЕВКИ
            if (DailyMissionManager.Instance != null)
                DailyMissionManager.Instance.InitMissions(_gameData.currentDailyMissions);

            // ВОССТАНОВЛЕНИЕ ЭКСПЕДИЦИИ (ЕСЛИ БЫЛА)
            if (_gameData.activeExpedition != null && _gameData.activeExpedition.isActive)
            {
                DungeonManager.Instance.ResumeExpeditionAfterLoad(_gameData.activeExpedition);
            }

            SyncAllUI();
        }
        else
        {
            Debug.Log("No save found. Starting new game.");
        }
    }

    private void SyncAllUI()
    {
        // СНАЧАЛА пересчитываем логику в GameManager
        GameManager.Instance.RecalculateMaxPeople();       // Исправит лимит 10
        GameManager.Instance.RecalculatePassiveIncome();   // Настроит доход
        GameManager.Instance.RecalculateMaxClickLevels();  // Настроит лимиты кликов

        // ЗАТЕМ обновляем визуальную часть
        BuildingManager.Instance.UpdateBuildingUI();
        HeroManager.Instance.UpdateHeroUI();
        GameManager.Instance.UpdateTroopUI();
        GameManager.Instance.UpdateResourceUI();
        DungeonManager.Instance.UpdateThreatUI();
        DungeonManager.Instance.UpdateBossButtonVisibility();
    }

}