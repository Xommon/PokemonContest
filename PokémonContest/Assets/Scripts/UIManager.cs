using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class UIManager : MonoBehaviour
{
    // References
    public Image textboxImage;
    public GameManager gameManager;

    // Variables
    public Sprite[] textboxSprites;

    void Update()
    {
        textboxImage.sprite = textboxSprites[gameManager.contestType];
    }
}
