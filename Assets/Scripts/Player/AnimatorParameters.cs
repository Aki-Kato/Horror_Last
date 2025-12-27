using UnityEngine;

public static class AnimatorParameters
{
    public const string X = "X";
    public const string Y = "Y";
    public const string Speed = "Speed";
    public const string Crouch = "Crouch";
    public const string Weapon = "Weapon";
    public static readonly int Attack = Animator.StringToHash("Attack");
    public const string AttackId = "AttackID";
    public const string Dash = "Dash";
    public static readonly int DodgeTrigger = Animator.StringToHash("Dodge"); // Trigger
    public static readonly int ExitDodge = Animator.StringToHash("ExitDodge");
    public static readonly int TakeWeapon = Animator.StringToHash("TakeWeapon");
    
}