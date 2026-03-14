using UnityEngine;
using System.Collections.Generic;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [Header("Все возможные задания")]
    public List<MissionDefinition> allMissions;
    public List<MissionDefinition> allPossibleMissions => allMissions;

    [Header("Текущие активные миссии")]
    public List<MissionDefinition> currentAvailableMissions = new List<MissionDefinition>();

    [Header("Настройки Reroll")]
    public int maxFreeRerolls = 5;
    public int remainingRerolls = 5;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // Метод, который вызывает UI при открытии или обновлении
    public List<MissionDefinition> GetMissions(bool forceRefresh = false)
    {
        // Если список пуст или мы принудительно обновляем (через Reroll)
        if (currentAvailableMissions.Count == 0 || forceRefresh)
        {
            GenerateNewMissions(3);
        }
        return currentAvailableMissions;
    }

    private void GenerateNewMissions(int count)
    {
        currentAvailableMissions.Clear();
        List<MissionDefinition> tempPool = new List<MissionDefinition>(allMissions);

        for (int i = 0; i < count; i++)
        {
            if (tempPool.Count == 0) break;
            int randomIndex = Random.Range(0, tempPool.Count);
            currentAvailableMissions.Add(tempPool[randomIndex]);
            tempPool.RemoveAt(randomIndex);
        }
    }

    // Логика использования крутки
    public bool TryReroll()
    {
        if (remainingRerolls > 0)
        {
            remainingRerolls--;
            GetMissions(true); // Принудительно обновляем список
            return true;
        }
        return false; // Крутки кончились, нужно смотреть рекламу
    }

    // Метод для восстановления попыток (вызывается после рекламы)
    public void RestoreRerolls()
    {
        remainingRerolls = maxFreeRerolls;
    }
}
