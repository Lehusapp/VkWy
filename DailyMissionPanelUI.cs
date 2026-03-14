using UnityEngine;
using System.Collections.Generic;

public class DailyMissionPanelUI : MonoBehaviour
{
    public Transform container; // Куда спавнить карточки
    public GameObject missionEntryPrefab; // Префаб карточки задания
                                          
    // В файле DailyMissionPanelUI.cs добавь этот метод:
    private void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        // Очищаем старые элементы
        foreach (Transform child in container) Destroy(child.gameObject);

        // Получаем данные из менеджера
        var currentMissions = DailyMissionManager.Instance.GetCurrentMissionsData();
        var allDefs = DailyMissionManager.Instance.allPossibleMissions;

        for (int i = 0; i < currentMissions.Count; i++)
        {
            var data = currentMissions[i];
            var def = allDefs[data.missionIndex];

            GameObject go = Instantiate(missionEntryPrefab, container);
            DailyMissionEntryUI entryScript = go.GetComponent<DailyMissionEntryUI>();
            entryScript.Setup(i, def, data);
        }
    }
}