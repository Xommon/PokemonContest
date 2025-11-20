using UnityEngine;

[CreateAssetMenu(fileName = "Attack", menuName = "ScriptableObjects/Attack", order = 0)]
public class Attack : ScriptableObject
{
    public enum Type { Cool, Beauty, Smart, Tough, Cute, Scary }
    public Type type;
    public string description;
    public int appeal; // 1+
    public int jam; // 1+
    public int protection; // 1-2
    public int nervous; // 1-2
    public int confidence; // 1
    public bool priority; // T/F
    public bool notPriority; // T/F
    public bool mixUp; // T/F
    public bool firstAppeal; // T/F
    public bool lastAppeal; // T/F
    public bool genderBased; // T/F
    public bool captivates; // T/F
    public bool sameTypeAppeal; // T/F
    public bool sameTypeJam; // T/F
    public bool lowerConfidence;
    public bool repeatable;
    public bool worksInAnyContest;
    public int exhaust;
    public bool copyAppeal;
    public bool betterIfLater;
    public bool betterIfEarlier;
}
