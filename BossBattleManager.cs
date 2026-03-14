using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic; // Добавлено, если HeroesManager использует List<Hero>

public class BossBattleManager : MonoBehaviour
{
    public static BossBattleManager Instance { get; private set; }

    [Header("UI")]
    public GameObject bossPanel;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI damageText;
    public Image bossImage; // Возможно, ты используешь Button, тогда это должно быть Sprite/Image компонентом

    [Header("Settings")]
    public float battleDuration = 60f;
    private float currentTimer;
    private long totalDamage;
    private bool isBattleActive = false;

    void Awake()
    {
        // Изменение: Стандартная Singleton-логика с DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Изменение: Явное отключение bossPanel в Start()
        if (bossPanel != null) bossPanel.SetActive(false);
    }

    public void StartBossBattle()
    {
        if (isBattleActive)
        {
            // Если битва уже активна, сообщаем об этом
            if (DungeonManager.Instance != null) DungeonManager.Instance.ShowDungeonMessage("Boss battle already active!");
            return;
        }

        // Добавлена проверка на DungeonManager для более чистого потока
        if (DungeonManager.Instance == null)
        {
            Debug.LogError("[BossBattleManager] DungeonManager.Instance is null. Cannot start boss battle.");
            return;
        }

        // Проверка, что босс готов к битве (добавил для надежности, так как вызов может прийти извне)
        if (DungeonManager.Instance.completedMissionsCounter < DungeonManager.Instance.missionsToBoss)
        {
            DungeonManager.Instance.ShowDungeonMessage($"Not enough missions completed to fight the Boss!");
            return;
        }

        if (bossPanel != null) bossPanel.SetActive(true);
        totalDamage = 0;
        currentTimer = battleDuration;
        isBattleActive = true;
        UpdateUI();
        StartCoroutine(BattleRoutine());

        // Оповещаем DungeonManager, что битва началась (возможно, чтобы отключить другие действия)
        DungeonManager.Instance.ShowDungeonMessage("Boss Battle Commencing! Good Luck!");
    }

    public void OnBossClick()
    {
        if (!isBattleActive) return;

        long clickDamage = CalculateGlobalPower();
        totalDamage += clickDamage;

        // Изменение: Проверяем, что bossImage не null перед попыткой тряски
        if (bossImage != null)
        {
            StartCoroutine(ShakeEffect());
        }
        UpdateUI();
    }

    private long CalculateGlobalPower()
    {
        long power = 0;

        // Герои
        if (HeroManager.Instance != null && HeroManager.Instance.hiredHeroes != null)
        {
            foreach (Hero h in HeroManager.Instance.hiredHeroes)
                power += h.CurrentAttack; // Используем только атаку героя, если так задумано
        }

        // Солдаты
        if (BarracksManager.Instance != null)
        {
            // Изменение: Добавлены проверки на null для SoldierDefinition
            if (BarracksManager.Instance.swordsmanDef != null)
                power += (long)(GameManager.Instance.SwordsmenCount * BarracksManager.Instance.swordsmanDef.attack);
            if (BarracksManager.Instance.archerDef != null)
                power += (long)(GameManager.Instance.ArchersCount * BarracksManager.Instance.archerDef.attack);
            if (BarracksManager.Instance.shieldbearerDef != null)
                power += (long)(GameManager.Instance.ShieldbearersCount * BarracksManager.Instance.shieldbearerDef.attack);
        }

        return power;
    }

    IEnumerator BattleRoutine()
    {
        // Изменение: Добавлена проверка isBattleActive в while для надежности
        while (currentTimer > 0 && isBattleActive)
        {
            currentTimer -= Time.deltaTime;
            UpdateUI();
            yield return null;
        }
        EndBattle();
    }

    private void EndBattle()
    {
        isBattleActive = false;
        long reward = totalDamage / 25; // Пример: 1 золото за 25 урона
        if (GameManager.Instance != null) GameManager.Instance.Gold += reward;

        // Сброс прогресса к следующему боссу и обновление его UI
        if (DungeonManager.Instance != null)
        {
            DungeonManager.Instance.completedMissionsCounter = 0;
            DungeonManager.Instance.UpdateBossButtonVisibility(); // ОБЯЗАТЕЛЬНО обновить после сброса!
            DungeonManager.Instance.ShowDungeonMessage($"Boss defeated! Damage: {totalDamage:N0}. Reward: {reward:N0} Gold!");
        }


        // DailyMissionManager
        if (DailyMissionManager.Instance != null)
        {
            DailyMissionManager.Instance.ProgressMission(DailyMissionType.BossVictory, 1);
        }

        // Сохранение в облако (как и договорились, оставляем, т.к. это критический момент)
        if (CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.SaveToCloud();
        }
        if (GameManager.Instance != null) GameManager.Instance.SyncTotalPowerToLeaderboard(); // Обновляем лидерборд

        if (bossPanel != null) bossPanel.SetActive(false);
    }

    void UpdateUI()
    {
        if (timerText != null) timerText.text = $"Time: {currentTimer:F1}s";
        if (damageText != null) damageText.text = $"Total Damage: {totalDamage:N0}";
    }

    IEnumerator ShakeEffect()
    {
        // Убеждаемся, что bossImage не null
        if (bossImage == null) yield break;

        Vector3 originalPos = bossImage.transform.localPosition;
        float shakeDuration = 0.05f; // Длительность одного "тряска"
        float shakeMagnitude = 5f;   // Насколько сильно трясти

        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float x = UnityEngine.Random.Range(-1f, 1f) * shakeMagnitude;
            float y = UnityEngine.Random.Range(-1f, 1f) * shakeMagnitude;
            bossImage.transform.localPosition = originalPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        bossImage.transform.localPosition = originalPos; // Возвращаем в исходное положение
    }
}