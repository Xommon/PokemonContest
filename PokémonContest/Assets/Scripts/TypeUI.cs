using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class TypeUI : MonoBehaviour
{
    [Range(0, 5)]
    public int type;
    public Color[] colours;
    public string[] types = { "Cool", "Beauty", "Smart", "Tough", "Cute", "Scary"};
    public Image background;
    public TextMeshProUGUI textbox;

    void Update()
    {
        background.color = colours[type];
        textbox.text = types[type];
    }
}
