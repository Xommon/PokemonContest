using UnityEngine;

[CreateAssetMenu(fileName = "Attack", menuName = "ScriptableObjects/Attack", order = 0)]
public class Attack : ScriptableObject
{
    public enum Type { Cool, Tough, Beauty, Cute, Smart, Scary }
    public string description;
    public Type type;
    public int appeal; // 1+
    public int jam; // 1+
    public int avoid; // 1-2
    public int nervous; // 1-2
    public int confidence; // 1
    public bool priority;
    public bool mixUp;
    public bool firstAppeal;
    public bool lastAppeal;
}
