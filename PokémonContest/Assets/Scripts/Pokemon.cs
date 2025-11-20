using UnityEngine;

[CreateAssetMenu(fileName = "Pokemon", menuName = "ScriptableObjects/Pokemon", order = 0)]
public class Pokemon : ScriptableObject
{
    public Attack.Type[] types;
    public Attack[] availableAttacks;
    public float[] sexRatio;
}
