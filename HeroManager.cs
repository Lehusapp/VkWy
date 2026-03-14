using UnityEngine;
using System.Collections.Generic;
using TMPro; // Для работы с TextMeshPro в UI
using UnityEngine.UI; // Добавлено для работы с Button
using System.Collections; // Добавлено для корутин

public class HeroManager : MonoBehaviour
{
    // Singleton для легкого доступа к менеджеру героев
    public static HeroManager Instance { get; private set; }

    // Список всех доступных типов героев (назначаем в Инспекторе!)
    [Header("Hero Definitions")]
    public List<HeroDefinition> availableHeroDefinitions;

    // Список всех нанятых героев
    public List<Hero> hiredHeroes = new List<Hero>();

    [Header("Hero Settings")]
    public int maxHeroes = 3;
    public int fragmentsPerDuplicateHire = 1; // Сколько фрагментов дается за найм дубликата

    [Header("UI References - Heroes")]
    public GameObject heroEntryUIPrefab; // Ссылка на префаб карточки героя (для нанятых)
    public Transform heroListContentParent; // Контейнер для нанятых героев
    public GameObject heroOfferUIPrefab; // Ссылка на префаб предложения героя
    public Transform heroOffersContentParent; // Контейнер для предложений героев
    public TextMeshProUGUI heroHireMessageText; // Для сообщения о нехватке ресурсов при найме героев

    // Ссылки на элементы UI панели деталей героя
    public GameObject heroDetailPanel; // Панель, которая теперь всегда активна
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailLevelText;
    public TextMeshProUGUI detailXPText;
    public TextMeshProUGUI detailAttackText;
    public TextMeshProUGUI detailHealthText;
    public TextMeshProUGUI detailDefenseText;
    public TextMeshProUGUI detailFragmentsText;
    public GameObject transcendButton;
    public TextMeshProUGUI noHeroSelectedMessageText; // Для сообщения, если герой не выбран/не нанят
    public GameObject heroDetailsContentParent; // Родитель для всех UI-элементов деталей героя, кроме noHeroSelectedMessageText.

    public Hero displayedHero; // Это поле хранит ссылку на героя, чьи детали сейчас отображаются

    [Header("Hero Hire Message Settings")]
    public float heroHireMessageDisplayDuration = 2.0f; // Длительность показа сообщения

    private Coroutine currentHeroHireMessageRoutine;
    
    [Header("Hero Offer Settings")]
    public int numberOfOffers = 3; // Сколько предложений будет показано одновременно
    public float offerRefreshTime = 60f; // Время в секундах до автоматического обновления предложений (или 0 для ручного)
    private float offerRefreshTimer; // Таймер для автоматического обновления

    private List<HeroDefinition> currentOffers = new List<HeroDefinition>(); // Список текущих предложений

    void Awake()
    {
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
        InitializeHeroDetailPanel(); // Инициализируем состояние панели деталей при старте
        GenerateHeroOffers(); // Инициализация и генерация предложений
        offerRefreshTimer = offerRefreshTime;
    }

    void Update()
    {
        if (offerRefreshTime > 0) // Если автоматическое обновление включено
        {
            offerRefreshTimer -= Time.deltaTime;
            if (offerRefreshTimer <= 0)
            {
                GenerateHeroOffers();
                offerRefreshTimer = offerRefreshTime;
            }
        }
    }

    // НОВЫЙ МЕТОД: Инициализация состояния панели деталей героя
    private void InitializeHeroDetailPanel()
    {
        if (noHeroSelectedMessageText == null || heroDetailsContentParent == null || heroDetailPanel == null)
        {
            Debug.LogError("Hero Detail Panel UI components not assigned in InitializeHeroDetailPanel!");
            return;
        }

        // Если есть нанятые герои, по умолчанию отображаем первого
        if (hiredHeroes.Count > 0)
        {
            displayedHero = hiredHeroes[0]; 
            DisplayHeroDetails(displayedHero); // Сразу отображаем первого героя
        }
        else
        {
            // Иначе показываем сообщение "нет героев"
            noHeroSelectedMessageText.gameObject.SetActive(true); // ИСПРАВЛЕНИЕ: Исправлено на noHeroSelectedMessageText
            heroDetailsContentParent.SetActive(false); // Скрываем остальной контент деталей
            displayedHero = null; // Никто не выбран
        }
        heroDetailPanel.SetActive(true); // Убеждаемся, что сама корневая панель активна
    }

    // Метод для найма конкретного героя по определению
    public void HireSpecificHero(HeroDefinition heroDefToHire)
    {
        Hero existingHeroOfType = hiredHeroes.Find(h => h.Definition == heroDefToHire);

        long hireCostGold = heroDefToHire.baseHireGoldCost;
        int hireCostPeople = heroDefToHire.baseHirePeopleCost;

        bool canAffordHire = GameManager.Instance.CanAffordGold(hireCostGold) &&
                             GameManager.Instance.CanAffordPeople(hireCostPeople);

        if (existingHeroOfType != null)
        {
            if (canAffordHire)
            {
                GameManager.Instance.TrySpendGold(hireCostGold);
                GameManager.Instance.TrySpendPeople(hireCostPeople);

                existingHeroOfType.GainFragments(fragmentsPerDuplicateHire);
                Debug.Log($"Gained {fragmentsPerDuplicateHire} fragments for {existingHeroOfType.Name} ({existingHeroOfType.Definition.heroTypeName}) from duplicate hire!"); // Опечатка: H
                UpdateHeroUI();
                GenerateHeroOffers();
            }
            else
            {
                ShowHeroHireMessage("Not enough resources for hero fragments!");
            }
            return;
        }

        if (hiredHeroes.Count >= maxHeroes)
        {
            Debug.Log("Max heroes reached! Cannot hire more.");
            ShowHeroHireMessage("Max heroes reached! Upgrade to expand barracks!");
            return;
        }

        if (canAffordHire)
        {
            GameManager.Instance.TrySpendGold(hireCostGold);
            GameManager.Instance.TrySpendPeople(hireCostPeople);

            string newHeroName = GenerateHeroName(hiredHeroes.Count);
            Hero newHero = new Hero(heroDefToHire, newHeroName);

            hiredHeroes.Add(newHero);
            Debug.Log($"Hired new hero: {newHero.Name} ({newHero.Definition.heroTypeName})!");
            UpdateHeroUI();
            GenerateHeroOffers();

            if (hiredHeroes.Count == 1) // Если нанят первый герой, сразу отображаем его детали
            {
                DisplayHeroDetails(newHero);
            }
        }
        else
        {
            ShowHeroHireMessage("Not enough resources to hire this hero!");
        }
    }

    private string GenerateHeroName(int index)
    {
        string[] names = {"Sir Reginald", "Lady Lyra", "Grizzled Barbarian", "Elven Archer", "Mystic Mage", "Dwarf Warrior"};
        if (index < names.Length)
        {
            return names[index];
        }
        return $"Hero {index + 1}";
    }

    public void GenerateHeroOffers()
    {
        if (heroOffersContentParent == null || heroOfferUIPrefab == null)
        {
            Debug.LogError("Hero offer UI components not assigned!");
            return;
        }

        foreach (Transform child in heroOffersContentParent)
        {
            Destroy(child.gameObject);
        }
        currentOffers.Clear();

        List<HeroDefinition> potentialOffers = new List<HeroDefinition>(availableHeroDefinitions);
        if (potentialOffers.Count == 0)
        {
            Debug.LogWarning("No hero definitions available to generate offers!");
            return;
        }

        for (int i = 0; i < numberOfOffers; i++)
        {
            if (potentialOffers.Count == 0) break;

            int randomIndex = Random.Range(0, potentialOffers.Count);
            HeroDefinition offerDef = potentialOffers[randomIndex];
            potentialOffers.RemoveAt(randomIndex);

            currentOffers.Add(offerDef);

            GameObject offerGO = Instantiate(heroOfferUIPrefab, heroOffersContentParent);

            TextMeshProUGUI typeNameText = offerGO.transform.Find("TypeNameText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI descriptionText = offerGO.transform.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI statsText = offerGO.transform.Find("StatsText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI costText = offerGO.transform.Find("CostText")?.GetComponent<TextMeshProUGUI>();
            Button hireButton = offerGO.transform.Find("HireButton")?.GetComponent<Button>();
            Image portraitImage = offerGO.GetComponent<Image>();


            if (typeNameText != null) typeNameText.text = offerDef.heroTypeName;
            if (descriptionText != null) descriptionText.text = offerDef.heroDescription;
            if (statsText != null) statsText.text = $"ATK: {offerDef.baseAttack} HP: {offerDef.baseHealth} DEF: {offerDef.baseDefense}";
            if (costText != null) costText.text = $"Cost: {offerDef.baseHireGoldCost.ToString("N0")} Gold, {offerDef.baseHirePeopleCost} People";
            if (portraitImage != null)
            {
                if (offerDef.heroPortrait != null)
                {
                    portraitImage.sprite = offerDef.heroPortrait;
                    portraitImage.color = Color.white;
                }
                else
                {
                    portraitImage.sprite = null;
                    portraitImage.color = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
                }
            }

            if (hireButton !=null)
            {
                hireButton.onClick.RemoveAllListeners();
                hireButton.onClick.AddListener(() => HireSpecificHero(offerDef));
            }
        }
        Debug.Log("Generated new hero offers.");
    }

    // Метод для обновления UI, показывающего нанятых героев
    public void UpdateHeroUI()
    {
        if (heroListContentParent == null || heroEntryUIPrefab == null)
        {
            Debug.LogError("Hero list UI components not assigned!");
            return;
        }

        // Очищаем все существующие карточки героев
        foreach (Transform child in heroListContentParent)
        {
            Destroy(child.gameObject);
        }

        if (hiredHeroes.Count == 0)
        {
            // Если героев нет, показываем сообщение "нет героев" и сбрасываем отображаемого героя
            noHeroSelectedMessageText.gameObject.SetActive(true);
            heroDetailsContentParent.SetActive(false);
            displayedHero = null;
            return; 
        }
        
        // Создаем новые карточки для каждого героя
        foreach (Hero hero in hiredHeroes)
        {
            GameObject heroEntryGO = Instantiate(heroEntryUIPrefab, heroListContentParent);

            TextMeshProUGUI nameText = heroEntryGO.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI statsText = heroEntryGO.transform.Find("StatsText")?.GetComponent<TextMeshProUGUI>();

            if (nameText != null) nameText.text = $"{hero.Name} Lvl: {hero.Level}";
            if (statsText != null) statsText.text = $"({hero.Definition.heroTypeName})";

            Button entryButton = heroEntryGO.GetComponent<Button>();
            if (entryButton != null)
            {
                entryButton.onClick.RemoveAllListeners();
                // Эта лямбда-функция гарантирует, что при клике на карточку,
                // DisplayHeroDetails будет вызвана для КОНКРЕТНОГО героя, связанного с этой карточкой.
                entryButton.onClick.AddListener(() => DisplayHeroDetails(hero));
            }
        }

        // После обновления списка героев, убеждаемся, что панель деталей отображает правильного героя.
        // Если отображаемый герой все еще существует и находится в списке, обновляем его детали.
        // Иначе, если герои есть, выбираем первого в списке.
        if (displayedHero != null && hiredHeroes.Contains(displayedHero))
        {
            DisplayHeroDetails(displayedHero); // Обновляем детали ранее выбранного героя
        }
        else if (hiredHeroes.Count > 0)
        {
            DisplayHeroDetails(hiredHeroes[0]); // Выбираем первого героя в списке, если предыдущий был недействителен
        }
        // else: hiredHeroes.Count == 0, уже обработано в начале этого метода.
    }

    // Отображает детальную информацию для выбранного героя
    public void DisplayHeroDetails(Hero hero)
    {
        // ЭТА ЛОГ-СТРОКА КРАЙНЕ ВАЖНА ДЛЯ ДИАГНОСТИКИ!
        Debug.Log($"DisplayHeroDetails: Setting displayedHero to {hero.Name} (Lvl {hero.Level}, Type: {hero.Definition.heroTypeName})");
        
        displayedHero = hero; // <-- ВОЗВРАЩЕНО

        if (heroDetailPanel != null && noHeroSelectedMessageText != null && heroDetailsContentParent != null)
        {
            noHeroSelectedMessageText.gameObject.SetActive(false); // Скрываем сообщение "нет героев"
            heroDetailsContentParent.SetActive(true); // Показываем контент деталей героя

            // Заполнение текстовых полей
            if (detailNameText != null) detailNameText.text = $"{hero.Name} ({hero.Definition.heroTypeName})";
            if (detailLevelText != null) detailLevelText.text = $"Level: {hero.Level}";
            if (detailXPText != null) detailXPText.text = $"XP: {hero.CurrentXP}/{hero.XPToNextLevel}";
            if (detailAttackText != null) detailAttackText.text = $"Attack: {hero.CurrentAttack}";
            if (detailHealthText != null) detailHealthText.text = $"Health: {hero.CurrentHealth}";
            if (detailDefenseText != null) detailDefenseText.text = $"Defense: {hero.CurrentDefense}";

            // Обновляем фрагменты
            if (detailFragmentsText != null)
            {
                detailFragmentsText.text = $"Fragments: {hero.CurrentFragments}/{hero.FragmentsRequiredForTranscendence}";
            }

            // Обновляем кнопку возвышения
            if (transcendButton != null)
            {
                bool isTranscendentLevel = hero.IsMaxLevelForTier;
                transcendButton.SetActive(isTranscendentLevel); // Показываем кнопку только на нужных уровнях

                Button btn = transcendButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners(); // УДАЛЯЕМ ВСЕ ПРЕДЫДУЩИЕ СЛУШАТЕЛИ

                    // НОВАЯ ЛОГИКА: Лямбда-функция ЗАХВАТЫВАЕТ текущий экземпляр `hero`
                    // и вызывает его методы напрямую, а затем обновляет UI
                    btn.onClick.AddListener(() =>
                    {
                        // Дублирование проверок для лучшей обратной связи UI
                        if (!hero.IsMaxLevelForTier)
                        {
                            Debug.LogWarning($"{hero.Name} ({hero.Definition.heroTypeName}) cannot Transcend. Not at a transcendence level.");
                            ShowHeroHireMessage($"{hero.Name} ({hero.Definition.heroTypeName}) is not at a transcendence level (Level {hero.Level}).");
                            return;
                        }
                        if (hero.CurrentFragments < hero.FragmentsRequiredForTranscendence)
                        {
                            Debug.LogWarning($"{hero.Name} needs more fragments ({hero.CurrentFragments}/{hero.FragmentsRequiredForTranscendence}).");ShowHeroHireMessage($"{hero.Name} needs {hero.FragmentsRequiredForTranscendence - hero.CurrentFragments} more fragments!");
                            return;
                        }
                        if (!GameManager.Instance.CanAffordGold(hero.Definition.goldCostForTranscendence))
                        {
                            Debug.LogWarning($"Not enough Gold for {hero.Name} to Transcend. Needs {hero.Definition.goldCostForTranscendence}.");
                            ShowHeroHireMessage($"{hero.Name} needs {hero.Definition.goldCostForTranscendence.ToString("N0")} Gold!");
                            return;
                        }

                        // Если все проверки пройдены, пытаемся возвысить КОНКРЕТНОГО `hero`
                        if (hero.Transcend())
                        {
                            Debug.Log($"Successfully Transcended {hero.Name}. Calling UpdateHeroUI.");
                            // После возвышения, обновим весь UI, чтобы перерисовать список героев
                            // и обновить панель деталей для этого же героя (если он все еще выбран)
                            // или выбрать первого героя в списке.
                            UpdateHeroUI(); 
                        }
                    });

                    // Кнопка активна, если герой на нужном уровне И достаточно фрагментов И золота
                    bool canTranscend = hero.IsMaxLevelForTier && 
                                        hero.CurrentFragments >= hero.FragmentsRequiredForTranscendence &&
                                        GameManager.Instance.Gold >= hero.Definition.goldCostForTranscendence; // ИСПРАВЛЕНИЕ: goldCostForTranscendence
                    btn.interactable = canTranscend;

                    // Обновляем текст кнопки с указанием стоимости
                    transcendButton.GetComponentInChildren<TextMeshProUGUI>().text =
                        $"Transcend\nCost: {hero.FragmentsRequiredForTranscendence} Frags, {hero.Definition.goldCostForTranscendence.ToString("N0")} Gold";
                }
            }
        }
    }

    // Этот метод теперь используется для сброса панели в состояние "нет выбранного героя"
    public void HideHeroDetailsPanel()
    {
        InitializeHeroDetailPanel();
    }

    public void ShowHeroHireMessage(string message)
    {
        if (heroHireMessageText == null)
        {
            Debug.LogError("HeroHireMessageText is not assigned in the Inspector for HeroManager!");
            return;
        }

        if (currentHeroHireMessageRoutine != null)
        {
            StopCoroutine(currentHeroHireMessageRoutine);
        }

        heroHireMessageText.text = message;
        heroHireMessageText.gameObject.SetActive(true);

        currentHeroHireMessageRoutine = StartCoroutine(HideHeroHireMessageRoutine(heroHireMessageDisplayDuration));
    }

    private IEnumerator HideHeroHireMessageRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (heroHireMessageText != null)
        {
            heroHireMessageText.gameObject.SetActive(false);
        }
        currentHeroHireMessageRoutine = null;
    }
}