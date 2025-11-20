using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CrowdMember : MonoBehaviour
{
    private RectTransform rect;
    private bool isBusy = false; // prevents overlap animations

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    // ---------------------------------------------------------
    // JUMP
    // ---------------------------------------------------------
    public void PlayJump()
    {
        if (!isBusy)
            StartCoroutine(JumpRoutine());
    }

    IEnumerator JumpRoutine()
    {
        isBusy = true;

        float duration = 0.35f;   // total jump time
        float height = 25f;       // jump height
        float t = 0f;

        Vector2 start = rect.anchoredPosition;

        // ----------- UP PHASE -----------
        while (t < 0.5f)
        {
            float p = t / 0.5f;

            // Ease-out (fast → slow)
            float eased = 1f - Mathf.Pow(1f - p, 2f);

            rect.anchoredPosition = start + new Vector2(0, eased * height);

            t += Time.deltaTime / duration;
            yield return null;
        }

        // ----------- DOWN PHASE -----------
        t = 0f;
        while (t < 0.5f)
        {
            float p = t / 0.5f;

            // Ease-in (slow → fast)
            float eased = Mathf.Pow(p, 2f);

            rect.anchoredPosition = start + new Vector2(0, (1f - eased) * height);

            t += Time.deltaTime / duration;
            yield return null;
        }

        // reset
        rect.anchoredPosition = start;
        isBusy = false;
    }

    // ---------------------------------------------------------
    // BOO (little shake)
    // ---------------------------------------------------------
    public void PlayBoo()
    {
        if (!isBusy)
            StartCoroutine(BooRoutine());
    }

    IEnumerator BooRoutine()
    {
        isBusy = true;

        float time = 0.5f;
        float strength = Random.Range(1, 10);
        float t = 0f;

        Vector2 start = rect.anchoredPosition;

        while (t < time)
        {
            float shake = Mathf.Sin(Time.time * 40f) * strength;
            rect.anchoredPosition = start + new Vector2(shake, 0);

            t += Time.deltaTime;
            yield return null;
        }

        rect.anchoredPosition = start;
        isBusy = false;
    }
}
