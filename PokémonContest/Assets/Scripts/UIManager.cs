using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class UIManager : MonoBehaviour
{
    // References
    public Image textboxImage;
    public GameManager gameManager;
    public AttackUI[] attackUIButtons;
    public GameObject contestantsWindowBackground;
    public GameObject contestantsWindow;

    // Attack information
    public GameObject attacksWindow;
    public TextMeshProUGUI attackDescription;
    public TextMeshProUGUI attackStats;
    public ContestantCard[] contestantCards;

    // Variables
    public Sprite[] textboxSprites;

    void Update()
    {
        textboxImage.sprite = textboxSprites[gameManager.contestType];
    }
}
