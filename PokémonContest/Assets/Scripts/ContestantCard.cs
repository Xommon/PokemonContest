using UnityEngine;
using TMPro;
using UnityEngine.UI;

[ExecuteAlways]
public class ContestantCard : MonoBehaviour
{
    // References
    public TextMeshProUGUI cardText;
    public Sprite[] heartSprites;
    public Image[] hearts;

    // Stats
    public string pokemonName;
    public string trainerName;
    public int totalScore;
    [Range(-10, 20)]
    public int currentScore;

    void Update()
    {
        // Update text
            cardText.text = $"<b><size=52>{pokemonName}</b>\n<size=42><color=black>{trainerName}";

        // Update hearts
        for (int i = 0; i < 10; i++)
        {
            // Enable/Disable hearts
            hearts[i].gameObject.SetActive(!(i + 1 > Mathf.Abs(currentScore)));

            // Display colour of hearts
            if (currentScore < 0)
            {
                hearts[i].sprite = heartSprites[0];
            }
            else if (currentScore < 11 || i + 1 > currentScore - 10)
            {
                hearts[i].sprite = heartSprites[1];
            }
            else
            {
                hearts[i].sprite = heartSprites[2];
            }
        }
    }
}
