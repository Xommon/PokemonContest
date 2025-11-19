using UnityEngine;

public class PokedexCreator : MonoBehaviour
{
    // Comma-separated Pokémon names (e.g. "bulbasaur, ivysaur, venusaur")
    [TextArea(3, 30)]
    public string pokedex;
}
