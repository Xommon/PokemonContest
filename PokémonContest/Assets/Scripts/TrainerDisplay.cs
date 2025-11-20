using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class TrainerDisplay : MonoBehaviour
{
    public Image mainImage;
    public Image pokemonImage;

    void Update()
    {
        mainImage.SetNativeSize();
        pokemonImage.SetNativeSize();
    }
}
