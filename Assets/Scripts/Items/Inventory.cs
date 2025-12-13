using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    // Singleton (не обязателен, но удобен)
    public static Inventory Instance { get; private set; }

    // Список всех предметов
    public List<Item> items = new List<Item>();

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // --- Методы работы с инвентарём ---

    /// <summary>
    /// Добавить предмет в инвентарь.
    /// </summary>
    public void Add(Item item)
    {
        if (item == null)
        {
            Debug.LogWarning("Inventory: попытка добавить null!");
            return;
        }

        items.Add(item);
        Debug.Log($"Added item: {item.itemName}");
    }

    /// <summary>
    /// Удалить предмет из инвентаря.
    /// </summary>
    public void Remove(Item item)
    {
        if (item == null) return;

        if (items.Contains(item))
        {
            items.Remove(item);
            Debug.Log($"Removed item: {item.itemName}");
        }
        else
        {
            Debug.LogWarning($"Inventory: предмет {item.itemName} не найден!");
        }
    }

    /// <summary>
    /// Проверка — есть ли предмет.
    /// </summary>
    public bool Contains(Item item)
    {
        return items.Contains(item);
    }
}