using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

[ExecuteAlways]
public class AttackUI : MonoBehaviour
{
    public TextMeshProUGUI text;
    public Attack attack;

    private Button button;
    private TypeUI typeUI;
    private UIManager uiManager;
    private GameManager gameManager;
    public ActivePokemon playerContestant;
    public Attack noneAttack;

    void Awake()
    {
        if (!Application.isPlaying)
            return;

        button = GetComponent<Button>();
        typeUI = FindFirstObjectByType<TypeUI>();
        uiManager = FindFirstObjectByType<UIManager>();
        gameManager = FindFirstObjectByType<GameManager>();
    }

    void Start()
    {
        if (Application.isPlaying)
        {
            EventSystem.current.SetSelectedGameObject(null);

            if (transform.GetSiblingIndex() == 0)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }

    void Update()
    {
        // Get the player contestant object
        try
        {
            attack = playerContestant.attacks[transform.GetSiblingIndex()];
        }
        catch
        {
            attack = noneAttack;
            text.text = "---";
        }

        // Change UI
        if (attack != noneAttack)
            text.text = attack.name.Replace("( Attack)", "");

        if (!Application.isPlaying)
            return;

        // When this attack button is selected, update the UI panel
        if (EventSystem.current.currentSelectedGameObject == button.gameObject)
        {
            typeUI.type = (int)attack.type;
            uiManager.attackDescription.text = attack.description;
            uiManager.attackStats.text =
                $"<color=#584858>Appeal: <color=red>{new string('♥', attack.appeal)}\n" +
                $"<color=#584858>Jam: <color=black>{new string('♥', attack.jam)}\n" +
                $"<color=#584858>Unnerve: <color=green>{new string('♥', attack.nervous)}";
        }
    }

    public void SelectAttack()
    {
        if (attack != null && attack.name != "--- (Attack)")
            StartCoroutine(SelectAttackWithDelay());
    }

    IEnumerator SelectAttackWithDelay()
    {
        uiManager.attacksWindow.SetActive(false);

        gameManager.attackRoster[gameManager.playerPosition] = attack;

        yield return new WaitForSeconds(1f);

        gameManager.ForceNextTurnFromUI();
    }
}
