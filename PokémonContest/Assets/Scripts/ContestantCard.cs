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
    public Sprite trainerSprite;
    public Sprite[] heartSprites;
    public Image[] hearts;
    public ActivePokemon contestant;
    public int nextTurn;
    public TextMeshProUGUI nextTurnText;

    // Variables
    public int totalScore;
    [Range(-10,20)]
    public int currentScore;
    public Pokemon[] pokemon;
    private float alpha;

    void Update()
    {
        if (contestant.contestantCard == null)
        {
            contestant.contestantCard = this;
        }

        // Background
        alpha = (gameManager.turn == -1) ? 1 : 0.35f;
        background.color = (gameObject.name == "Contestant1") ? new Color(1, 0.5f, 0.4f, alpha) : new Color(1, 1, 1, alpha);

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

        // Next turn
        if (nextTurn == 0)
        {
            nextTurnText.text = "";
        }
        else if (nextTurn > 0 && nextTurn < 5)
        {
            nextTurnText.text = "Next Turn <b>" + nextTurn;
        }
        else if (nextTurn < 0)
        {
            nextTurnText.text = "Next Turn <b>?";
        }

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
