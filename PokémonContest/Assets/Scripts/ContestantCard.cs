using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Net.Mail;

[ExecuteAlways]
public class ContestantCard : MonoBehaviour
{
    // References
    public Image background;
    public GameManager gameManager;
    public TextMeshProUGUI cardText;
    public Sprite[] heartSprites;
    public Image[] hearts;
    public ActivePokemon contestant;

    void Update()
    {
        // Background
        if (gameManager.turn == -1)
        {
            background.color = new Color(1, 1, 1, 1);
        }
        else
        {
            background.color = (gameManager.turn == transform.GetSiblingIndex()) ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, 0.35f);
        }

        // Get sex
        string sex = "";
        if (contestant.sex == 0)
        {
            sex = "<color=blue>♂️</color>";
        }
        else if (contestant.sex == 1)
        {
            sex = "<color=red>♀️</color>";
        }

        // Update text
        cardText.text = $"<b><size=52>{contestant.pokemonName}{sex}</b>\n<size=42><color=black>{contestant.trainer}";

        // Update hearts
        for (int i = 0; i < 10; i++)
        {
            // Enable/Disable hearts
            hearts[i].gameObject.SetActive(!(i + 1 > Mathf.Abs(contestant.currentScore)));

            // Display colour of hearts
            if (contestant.currentScore < 0)
            {
                hearts[i].sprite = heartSprites[0];
            }
            else if (contestant.currentScore < 11 || i + 1 > contestant.currentScore - 10)
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
