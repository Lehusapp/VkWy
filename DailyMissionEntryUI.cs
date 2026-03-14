using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DailyMissionEntryUI : MonoBehaviour
{
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI rewardText;
    public Slider progressSlider;
    public Button claimButton;
    public GameObject completedOverlay; // Опционально: галочка или затемнение

    private int _missionListIndex;

    public void Setup(int index, DailyMissionDefinition def, DailyMissionSaveData data)
    {
        _missionListIndex = index;
        descriptionText.text = def.description;
        rewardText.text = $"+{def.goldReward:N0} Gold";

        // Обновляем слайдер и текст прогресса
        progressSlider.maxValue = def.requiredAmount;
        progressSlider.value = data.progress;
        progressText.text = $"{data.progress} / {def.requiredAmount}";

        // Логика кнопки "Забрать"
        bool canClaim = data.progress >= def.requiredAmount && !data.isClaimed;
        claimButton.interactable = canClaim;

        // Текст на кнопке
        TextMeshProUGUI btnText = claimButton.GetComponentInChildren<TextMeshProUGUI>();
        if (data.isClaimed)
        {
            btnText.text = "Claimed";
            if (completedOverlay != null) completedOverlay.SetActive(true);
        }
        else
        {
            btnText.text = canClaim ? "Claim!" : "In Progress";
            if (completedOverlay != null) completedOverlay.SetActive(false);
        }

        claimButton.onClick.RemoveAllListeners();
        claimButton.onClick.AddListener(() => DailyMissionManager.Instance.ClaimReward(_missionListIndex));
    }
}