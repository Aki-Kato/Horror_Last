using UnityEngine;

[CreateAssetMenu(
    fileName = "NewItem",
    menuName = "Game/Item",
    order = 1)]
public class Item : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Название предмета, отображаемое игроку.")]
    public string itemName;

    [Tooltip("Иконка предмета для UI.")]
    public Sprite icon;
}
