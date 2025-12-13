using UnityEngine;

public class PickupItem : InteractableTrigger
{
    [Header("Item Settings")]
    [Tooltip("Данные предмета (ScriptableObject Item).")]
    [SerializeField] private Item itemData;

    [Tooltip("Удалить объект из сцены после подбора.")]
    [SerializeField] private bool destroyOnPickup = true;

    [Header("FX (опционально)")]
    [Tooltip("Звук, который проигрывается при подборе.")]
    [SerializeField] private AudioSource pickupSound;

    public override bool Interact()
    {
        // Если нет данных предмета — ничего не делаем
        if (itemData == null)
        {
            Debug.LogWarning($"PickupItem на объекте {name}: не назначен ItemData!");
            return false;
        }

        // Если нет инвентаря — тоже предупредим
        if (Inventory.Instance == null)
        {
            Debug.LogWarning("PickupItem: Inventory.Instance == null. Положить предмет некуда.");
            return false;
        }

        // 1) Добавляем предмет в инвентарь
        Inventory.Instance.Add(itemData);

        // 2) Проигрываем звук, если есть
        if (pickupSound != null)
        {
            pickupSound.Play();
        }

        // 3) Удаляем объект из сцены (после звука, если есть)
        if (destroyOnPickup)
        {
            if (pickupSound != null && pickupSound.clip != null)
            {
                Destroy(gameObject, pickupSound.clip.length);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        return true;
    }
}
