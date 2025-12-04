using System.Collections.Generic;
using UnityEngine;

public static class DamagablesRegistry
{
    public static readonly Dictionary<Collider, IDamagable> All = new();
    public static IDamagable PlayerDamagable;
}