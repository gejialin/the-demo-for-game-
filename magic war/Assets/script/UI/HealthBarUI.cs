using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private HealthComponent target;
    [SerializeField] private Image fillImage;
    [SerializeField] private Slider slider;
    [SerializeField] private Text valueText;
    [SerializeField] private bool faceCamera = true;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        float normalized = target.NormalizedHealth;

        if (fillImage != null)
            fillImage.fillAmount = normalized;

        if (slider != null)
            slider.value = normalized;

        if (valueText != null)
            valueText.text = Mathf.CeilToInt(target.currentHealth) + " / " + Mathf.CeilToInt(target.maxHealth);

        if (faceCamera)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera != null)
                transform.forward = mainCamera.transform.forward;
        }
    }

    public void SetTarget(HealthComponent newTarget)
    {
        target = newTarget;
    }
}
