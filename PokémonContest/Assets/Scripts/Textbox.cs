using System.Collections;
using TMPro;
using UnityEngine;

public class Textbox : MonoBehaviour
{
    public TextMeshProUGUI textbox;

    private string newMessage;
    private int newMessageIndex;

    public System.Action onMessageFinished;

    public void PushMessage(string message, System.Action callback)
    {
        StopAllCoroutines();
        textbox.text = "";

        newMessage = message;
        newMessageIndex = 0;
        onMessageFinished = callback;

        StartCoroutine(PushLetter());
    }

    IEnumerator PushLetter()
    {
        if (string.IsNullOrEmpty(newMessage) || newMessageIndex >= newMessage.Length)
        {
            // No delay — GameManager controls timing
            onMessageFinished?.Invoke();
            yield break;
        }

        if (newMessage[newMessageIndex] == '<')
        {
            string tag = "";
            while (newMessage[newMessageIndex] != '>')
            {
                tag += newMessage[newMessageIndex];
                newMessageIndex++;
            }
            tag += '>';
            newMessageIndex++;

            textbox.text += tag;

            if (newMessageIndex < newMessage.Length)
            {
                textbox.text += newMessage[newMessageIndex];
                newMessageIndex++;
            }
        }
        else
        {
            textbox.text += newMessage[newMessageIndex];
            newMessageIndex++;
        }

        yield return new WaitForSeconds(0.025f);
        StartCoroutine(PushLetter());
    }
}
