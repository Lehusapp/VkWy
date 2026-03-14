using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using GamePush; // Обязательно для работы рекламы

public class ExpeditionPreparationUI : MonoBehaviour
{
    private GameManager gameManager;
    private HeroManager heroManager;
    private DungeonManager dungeonManager;
    private BarracksManager barracksManager;
    private MissionManager missionManager;

    [Header("Hero Selection (Carousel)")]
    public Transform heroContainer;
    public Button heroBtnNext; // Стрелка ВНИЗ
    public Button heroBtnPrev; // Стрелка ВВЕРХ

    [Header("Mission Selection (Carousel)")]
    public Transform missionContainer;
    public GameObject missionCardPrefab;
    public Button missionBtnNext;
    public Button missionBtnPrev;

    [Header("Mission Reroll Settings")]
    public Button rerollMissionsButton;
    public TextMeshProUGUI rerollCounterText; // Текст для отображения "5/5" или "AD"

    [Header("Selected Mission Details UI")]
    public GameObject selectedMissionDetailsParent;
    public TextMeshProUGUI selectedMissionNameText;
    public TextMeshProUGUI selectedMissionDescriptionText;
    public TextMeshProUGUI selectedMissionEnemyPowerText;
    public TextMeshProUGUI selectedMissionThreatMultText;

    [Header("Selected Hero Details UI")]
    public GameObject selectedHeroDetailsParent;
    public TextMeshProUGUI selectedHeroNameText;
    public TextMeshProUGUI selectedHeroLevelText;
    public TextMeshProUGUI selectedHeroXPText;
    public TextMeshProUGUI selectedHeroAttackText;
    public TextMeshProUGUI selectedHeroHealthText;
    public TextMeshProUGUI selectedHeroDefenseText;
    public TextMeshProUGUI selectedHeroFragmentsText;
    public TextMeshProUGUI noHeroSelectedMessageText;

    [Header("Soldier Selection (Sliders)")]
    public TextMeshProUGUI swordsmanAvailableText;
    public Slider swordsmanSlider;
    public TextMeshProUGUI swordsmanSelectedText;
    public TextMeshProUGUI swordsmanPowerText;

    public TextMeshProUGUI archerAvailableText;
    public Slider archerSlider;
    public TextMeshProUGUI archerSelectedText;
    public TextMeshProUGUI archerPowerText;

    public TextMeshProUGUI shieldbearerAvailableText;
    public Slider shieldbearerSlider;
    public TextMeshProUGUI shieldbearerSelectedText;
    public TextMeshProUGUI shieldbearerPowerText;

    [Header("Overall Expedition Stats")]
    public TextMeshProUGUI totalExpeditionPowerDisplay;
    public Button startExpeditionButton;
    public Button closePanelButton;

    private List<Hero> _availableHeroes = new List<Hero>();
    private List<GameObject> _heroCardObjects = new List<GameObject>();
    private int _currentHeroIndex = 0;
    private Hero _selectedHeroForExpedition;

    private List<MissionDefinition> _availableMissions = new List<MissionDefinition>();
    private List<GameObject> _missionCardObjects = new List<GameObject>();
    private int _currentMissionIndex = 0;
    private MissionDefinition _selectedMission;

    private int _swordsmenToSend;
    private int _archersToSend;
    private int _shieldbearersToSend;

    void Awake()
    {
        gameManager = GameManager.Instance;
        heroManager = HeroManager.Instance;
        dungeonManager = DungeonManager.Instance;
        barracksManager = BarracksManager.Instance;
        missionManager = MissionManager.Instance;
    }

    void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        // 1. Инициализация Героев
        if (heroManager != null)
        {
            _availableHeroes = new List<Hero>(heroManager.hiredHeroes);
            _currentHeroIndex = 0;
            SpawnHeroCards();
            UpdateHeroDisplay();
        }

        // 2. Инициализация Миссий (Берем стабильный список из менеджера)
        if (missionManager != null)
        {
            _availableMissions = missionManager.GetMissions(); _currentMissionIndex = 0;
            SpawnMissionCards();
            UpdateMissionDisplay();
            UpdateRerollUI();
        }

        // 3. Сброс солдат
        _swordsmenToSend = 0;
        _archersToSend = 0;
        _shieldbearersToSend = 0;

        SetupButtons();
        UpdateSoldierSelectionSliders();
        UpdateTotalExpeditionPowerUI();
    }

    private void SetupButtons()
    {
        heroBtnNext.onClick.RemoveAllListeners();
        heroBtnNext.onClick.AddListener(NextHero);
        heroBtnPrev.onClick.RemoveAllListeners();
        heroBtnPrev.onClick.AddListener(PrevHero);

        missionBtnNext.onClick.RemoveAllListeners();
        missionBtnNext.onClick.AddListener(NextMission);
        missionBtnPrev.onClick.RemoveAllListeners();
        missionBtnPrev.onClick.AddListener(PrevMission);

        rerollMissionsButton.onClick.RemoveAllListeners();
        rerollMissionsButton.onClick.AddListener(OnRerollMissionsButtonClicked);

        closePanelButton.onClick.RemoveAllListeners();
        closePanelButton.onClick.AddListener(() => gameObject.SetActive(false));

        startExpeditionButton.onClick.RemoveAllListeners();
        startExpeditionButton.onClick.AddListener(OnStartExpeditionButtonClicked);
    }

    // --- ЛОГИКА ОБНОВЛЕНИЯ МИССИЙ (REROLL) ---
    private void UpdateRerollUI()
    {
        if (rerollCounterText != null && missionManager != null)
        {
            if (missionManager.remainingRerolls > 0)
            {
                rerollCounterText.text = $"Reroll: {missionManager.remainingRerolls}/{missionManager.maxFreeRerolls}";
                rerollCounterText.color = Color.white; // Обычный цвет
            }
            else
            {
                rerollCounterText.text = "Get Rerolls <color=yellow>(AD)</color>";
                rerollCounterText.color = Color.yellow; // Выделяем, что это реклама
            }
        }
    }


    public void OnRerollMissionsButtonClicked()
    {
        if (missionManager == null) return;

        if (missionManager.remainingRerolls > 0)
        {
            if (missionManager.TryReroll())
            {
                _availableMissions = missionManager.currentAvailableMissions;
                _currentMissionIndex = 0;
                SpawnMissionCards();
                UpdateMissionDisplay();
                UpdateRerollUI();
                dungeonManager.ShowDungeonMessage("Missions refreshed!");
            }
        }
        else
        {
            // Показываем рекламу для восстановления попыток
            GP_Ads.ShowRewarded("REBUILD_REROLLS", OnAdRewardSuccess);
        }
    }

    private void OnAdRewardSuccess(string tag)
    {
        if (tag == "REBUILD_REROLLS")
        {
            missionManager.RestoreRerolls();
        //  missionManager.TryReroll(); // Сразу один раз обновляем бесплатно после рекламы
            _availableMissions = missionManager.currentAvailableMissions;
            _currentMissionIndex = 0;
            SpawnMissionCards();
            UpdateMissionDisplay();
            UpdateRerollUI();
            dungeonManager.ShowDungeonMessage("Attempts restored!");
        }
    }

    // --- ЛОГИКА ЗАКОЛЬЦОВАННОЙ КАРУСЕЛИ ГЕРОЕВ ---
    private void SpawnHeroCards()
    {
        foreach (Transform child in heroContainer) Destroy(child.gameObject);
        _heroCardObjects.Clear();
        if (_availableHeroes.Count == 0) return;

        for (int i = 0; i < _availableHeroes.Count; i++)
        {
            GameObject go = Instantiate(heroManager.heroEntryUIPrefab, heroContainer);
            go.transform.localPosition = Vector3.zero;
            go.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = _availableHeroes[i].Name;
            go.transform.Find("StatsText").GetComponent<TextMeshProUGUI>().text = $"({_availableHeroes[i].Definition.heroTypeName})";
            _heroCardObjects.Add(go);
            go.SetActive(false);
        }
    }

    public void NextHero()
    {
        if (_heroCardObjects.Count <= 1) return;
        _currentHeroIndex = (_currentHeroIndex + 1) % _heroCardObjects.Count; // Закольцовка вперед
        UpdateHeroDisplay();
    }

    public void PrevHero()
    {
        if (_heroCardObjects.Count <= 1) return;
        _currentHeroIndex--;
        if (_currentHeroIndex < 0) _currentHeroIndex = _heroCardObjects.Count - 1; // Закольцовка назад
        UpdateHeroDisplay();
    }

    private void UpdateHeroDisplay()
    {
        if (_heroCardObjects.Count == 0)
        {
            selectedHeroDetailsParent.SetActive(false);
            noHeroSelectedMessageText.gameObject.SetActive(true);
            _selectedHeroForExpedition = null;
            return;
        }

        for (int i = 0; i < _heroCardObjects.Count; i++) _heroCardObjects[i].SetActive(i == _currentHeroIndex);
        _selectedHeroForExpedition = _availableHeroes[_currentHeroIndex];
        UpdateStatsPanel();
        UpdateTotalExpeditionPowerUI();
    }

    // --- ЛОГИКА КАРОУСЕЛИ МИССИЙ ---
    private void SpawnMissionCards()
    {
        foreach (Transform child in missionContainer) Destroy(child.gameObject);
        _missionCardObjects.Clear();
        if (_availableMissions.Count == 0) return;

        for (int i = 0; i < _availableMissions.Count; i++)
        {
            GameObject go = Instantiate(missionCardPrefab, missionContainer);
            go.transform.localPosition = Vector3.zero;
            go.transform.Find("TitleText").GetComponent<TextMeshProUGUI>().text = _availableMissions[i].missionName;
            go.transform.Find("RewardText").GetComponent<TextMeshProUGUI>().text = $"{_availableMissions[i].goldReward:N0} Gold";
            go.transform.Find("TimeText").GetComponent<TextMeshProUGUI>().text = $"{_availableMissions[i].durationSeconds:F0} sec";
            _missionCardObjects.Add(go);
            go.SetActive(false);
        }
    }

    public void NextMission()
    {
        if (_missionCardObjects.Count <= 1) return;
        _currentMissionIndex = (_currentMissionIndex + 1) % _missionCardObjects.Count;
        UpdateMissionDisplay();
    }

    public void PrevMission()
    {
        if (_missionCardObjects.Count <= 1) return;
        _currentMissionIndex--;
        if (_currentMissionIndex < 0) _currentMissionIndex = _missionCardObjects.Count - 1;
        UpdateMissionDisplay();
    }

    private void UpdateMissionDisplay()
    {
        if (_missionCardObjects.Count == 0)
        {
            _selectedMission = null;
            selectedMissionDetailsParent.SetActive(false);
            return;
        }

        for (int i = 0; i < _missionCardObjects.Count; i++) _missionCardObjects[i].SetActive(i == _currentMissionIndex);
        _selectedMission = _availableMissions[_currentMissionIndex];
        UpdateMissionStatsPanel();
        UpdateTotalExpeditionPowerUI();
    }

    private void UpdateMissionStatsPanel()
    {
        if (_selectedMission == null) { selectedMissionDetailsParent.SetActive(false); return; }
        selectedMissionDetailsParent.SetActive(true);
        selectedMissionNameText.text = _selectedMission.missionName;
        selectedMissionDescriptionText.text = _selectedMission.description;
        float enemyPower = _selectedMission.enemyPowerBase + (dungeonManager.enemyPowerPerThreatPercent * dungeonManager.threatLevel * _selectedMission.threatMultiplier);
        selectedMissionEnemyPowerText.text = $"Enemy Might: {enemyPower:N0}";
        selectedMissionThreatMultText.text = $"Threat Impact: x{_selectedMission.threatMultiplier:F1}";
        selectedMissionThreatMultText.color = (_selectedMission.threatMultiplier > 1.5f) ? Color.red : Color.white;
    }

    private void UpdateStatsPanel()
    {
        if (_selectedHeroForExpedition == null)
        {
            selectedHeroDetailsParent.SetActive(false);
            noHeroSelectedMessageText.gameObject.SetActive(true);
            return;
        }
        selectedHeroDetailsParent.SetActive(true);
        noHeroSelectedMessageText.gameObject.SetActive(false);
        selectedHeroNameText.text = _selectedHeroForExpedition.Name;
        selectedHeroLevelText.text = $"Lvl: {_selectedHeroForExpedition.Level}";
        selectedHeroXPText.text = $"XP: {_selectedHeroForExpedition.CurrentXP}/{_selectedHeroForExpedition.XPToNextLevel}";
        selectedHeroAttackText.text = $"ATK: {_selectedHeroForExpedition.CurrentAttack}";
        selectedHeroHealthText.text = $"HP: {_selectedHeroForExpedition.CurrentHealth}";
        selectedHeroDefenseText.text = $"DEF: {_selectedHeroForExpedition.CurrentDefense}";
        selectedHeroFragmentsText.text = $"Fragm: {_selectedHeroForExpedition.CurrentFragments}/{_selectedHeroForExpedition.FragmentsRequiredForTranscendence}";
    }

    public void UpdateSoldierSelectionSliders()
    {
        SetupSlider(swordsmanSlider, gameManager.SwordsmenCount, _swordsmenToSend, swordsmanAvailableText, "Available", OnSwordsmanSliderChanged);
        SetupSlider(archerSlider, gameManager.ArchersCount, _archersToSend, archerAvailableText, "Available", OnArcherSliderChanged);
        SetupSlider(shieldbearerSlider, gameManager.ShieldbearersCount, _shieldbearersToSend, shieldbearerAvailableText, "Available", OnShieldbearerSliderChanged);
        UpdateTotalExpeditionPowerUI();
    }

    private void SetupSlider(Slider slider, int max, int current, TextMeshProUGUI txt, string label, UnityEngine.Events.UnityAction<float> action)
    {
        if (slider == null) return;
        txt.text = $"{label}: {max}";
        slider.maxValue = max;
        slider.value = Mathf.Min(current, max);
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(action);
    }

    private void OnSwordsmanSliderChanged(float val) { _swordsmenToSend = (int)val; swordsmanSelectedText.text = $"Send: {_swordsmenToSend}"; UpdateSoldierPowerDisplay(swordsmanPowerText, _swordsmenToSend, barracksManager.swordsmanDef); UpdateTotalExpeditionPowerUI(); }
    private void OnArcherSliderChanged(float val) { _archersToSend = (int)val; archerSelectedText.text = $"Send: {_archersToSend}"; UpdateSoldierPowerDisplay(archerPowerText, _archersToSend, barracksManager.archerDef); UpdateTotalExpeditionPowerUI(); }
    private void OnShieldbearerSliderChanged(float val) { _shieldbearersToSend = (int)val; shieldbearerSelectedText.text = $"Send: {_shieldbearersToSend}"; UpdateSoldierPowerDisplay(shieldbearerPowerText, _shieldbearersToSend, barracksManager.shieldbearerDef); UpdateTotalExpeditionPowerUI(); }

    private void UpdateSoldierPowerDisplay(TextMeshProUGUI txt, int count, SoldierDefinition def)
    {
        if (def == null) return;
        long p = (long)count * (def.attack + def.health + def.defense);
        txt.text = $"Might: {p:N0}";
    }

    private void UpdateTotalExpeditionPowerUI()
    {
        long power = 0;
        if (_selectedHeroForExpedition != null) power += _selectedHeroForExpedition.CurrentAttack + _selectedHeroForExpedition.CurrentHealth + _selectedHeroForExpedition.CurrentDefense;
        if (barracksManager.swordsmanDef != null) power += (long)_swordsmenToSend * (barracksManager.swordsmanDef.attack + barracksManager.swordsmanDef.health + barracksManager.swordsmanDef.defense);
        if (barracksManager.archerDef != null) power += (long)_archersToSend * (barracksManager.archerDef.attack + barracksManager.archerDef.health + barracksManager.archerDef.defense);
        if (barracksManager.shieldbearerDef != null) power += (long)_shieldbearersToSend * (barracksManager.shieldbearerDef.attack + barracksManager.shieldbearerDef.health + barracksManager.shieldbearerDef.defense);

        float enemyPower = 0;
        if (_selectedMission != null) enemyPower = _selectedMission.enemyPowerBase + (dungeonManager.enemyPowerPerThreatPercent * dungeonManager.threatLevel * _selectedMission.threatMultiplier);

        totalExpeditionPowerDisplay.text = $"Your Power: {power:N0}  vs  Enemy: {enemyPower:N0}";
        if (power >= enemyPower * dungeonManager.winRatioThreshold) totalExpeditionPowerDisplay.color = Color.green;
        else if (power < enemyPower * dungeonManager.partialWinRatioThreshold) totalExpeditionPowerDisplay.color = Color.red;
        else totalExpeditionPowerDisplay.color = Color.yellow;

        startExpeditionButton.interactable = (power > 0 && _selectedMission != null);
    }

    public void OnStartExpeditionButtonClicked()
    {
        if (_selectedMission == null) return;
        dungeonManager.StartExpedition(_selectedHeroForExpedition, _swordsmenToSend, _archersToSend, _shieldbearersToSend, _selectedMission);
        gameObject.SetActive(false);
    }
}