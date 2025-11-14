using UnityEngine;

[CreateAssetMenu(fileName = "Pokemon", menuName = "ScriptableObjects/Pokemon", order = 0)]
public class Pokemon : ScriptableObject
{
    public Attack[] availableAttacks;
    public Sprite[] sprites;
    public float[] sexRatio;
}
