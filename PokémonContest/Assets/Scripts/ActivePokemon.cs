using Mono.Cecil;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class ActivePokemon : MonoBehaviour
{
    public Image targetImage;
    public RectTransform rectTransform;
    public Pokemon pokemon;
    public string pokemonName;
    public string trainer;
    public Attack[] attacks;
    public int sex;
    public int orientation;
    public int confidence;
    public bool nervous;
    public int protection;

    // 0 = inactive, 1 = moving in, 2 = ready/performing, 3 = moving out
    public int state;

    private Vector2 moveStart;
    private Vector2 moveTarget;
    private float moveT;

    // Let GameManager subscribe to this so there's no recursion problems
    public System.Action<ActivePokemon> OnMoveInFinished;
    public System.Action<ActivePokemon> OnMoveOutFinished;

    // Stats
    public int totalScore;
    [Range(-10, 20)]
    public int currentScore;
    [HideInInspector]
    public int futureScore;

    void Update()
    {
        // Update name
        pokemonName = pokemon.name;

        // Update sprite
        targetImage.SetNativeSize();

        if (sex == 0)
            targetImage.sprite = Resources.Load<Sprite>("Sprites/Pokemon/" + pokemon.name.Replace(" ", "-").ToLower());
        else
        {
            try
            {
                targetImage.sprite = Resources.Load<Sprite>("Sprites/Pokemon/" + pokemon.name.Replace(" ", "-").ToLower() + "-f");
            }
            catch
            {
                targetImage.sprite = Resources.Load<Sprite>("Sprites/Pokemon/" + pokemon.name.Replace(" ", "-").ToLower());
            }
        }
        targetImage.color = Color.white;

        // ---------------------------------
        // MOVEMENT: ENTER STAGE (state = 1)
        // ---------------------------------
        if (state == 1)
        {
            if (moveT == 0f)
            {
                moveStart = rectTransform.anchoredPosition;
                moveTarget = new Vector2(700, -250);
            }

            moveT += Time.deltaTime * 1f; // speed multiplier
            rectTransform.anchoredPosition =
                Vector2.LerpUnclamped(moveStart, moveTarget,
                Mathf.SmoothStep(0f, 1f, moveT));

            // Snap + notify GameManager
            if (Vector2.Distance(rectTransform.anchoredPosition, moveTarget) < 1f)
            {
                rectTransform.anchoredPosition = moveTarget;
                moveT = 0f;

                state = 2; // Now in position, ready to perform

                // Notify GameManager the movement is done
                OnMoveInFinished?.Invoke(this);
            }
        }

        // ----------------------------------
        // MOVEMENT: EXIT STAGE (state = 3)
        // ----------------------------------
        else if (state == 3)
        {
            if (moveT == 0f)
            {
                moveStart = rectTransform.anchoredPosition;
                moveTarget = new Vector2(-1250, -250);
            }

            moveT += Time.deltaTime * 2f; // Faster exit speed
            rectTransform.anchoredPosition =
                Vector2.LerpUnclamped(moveStart, moveTarget,
                Mathf.SmoothStep(0f, 1f, moveT));

            if (Vector2.Distance(rectTransform.anchoredPosition, moveTarget) < 1f)
            {
                rectTransform.anchoredPosition = new Vector2(1250, -250);
                moveT = 0f;

                state = 0; // Reset

                // Notify GameManager that exit animation finished, if needed
                OnMoveOutFinished?.Invoke(this);
            }
        }
    }

    // ------------------------------------
    // Utility randomizers
    // ------------------------------------
    int GetRandomByRatio(float a, float b)
    {
        float total = a + b;
        float rand = Random.value * total;
        return rand < a ? 0 : 1;
    }

    [ContextMenu("Refresh Pokemon")]
    public void RefreshPokemon()
    {
        sex = GetRandomByRatio(pokemon.sexRatio[0], pokemon.sexRatio[1]);
        orientation = GetRandomByRatio(0.9f, 0.1f);
        targetImage.SetNativeSize();
    }
}
