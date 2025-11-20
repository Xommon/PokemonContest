using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Crowd : MonoBehaviour
{
    [Tooltip("List of possible sprites the crowd can pull from.")]
    public List<Sprite> possibleSprites = new List<Sprite>();

    [HideInInspector]
    public Image[] crowdImages;

    // Add to Inspector (right-click component → RefreshCrowd)
    [ContextMenu("Refresh Crowd")]
    public void RefreshCrowd()
    {
        // Get all Image components in children
        crowdImages = GetComponentsInChildren<Image>(includeInactive: true);

        if (possibleSprites == null || possibleSprites.Count == 0)
        {
            Debug.LogWarning("Crowd: No possibleSprites assigned.");
            return;
        }

        if (possibleSprites.Count == 1 && crowdImages.Length > 1)
        {
            Debug.LogWarning("Crowd: Only 1 sprite available but multiple crowd slots exist — neighbors will match.");
        }

        // Assign sprites safely
        for (int i = 0; i < crowdImages.Length; i++)
        {
            Sprite chosen = GetValidSprite(i);

            crowdImages[i].sprite = chosen;
            crowdImages[i].SetNativeSize();   // ⭐ NEW: force native size
        }

        Debug.Log("Crowd refreshed.");
    }

    /// <summary>
    /// Picks a sprite that does NOT match the neighbor on the left or right.
    /// </summary>
    private Sprite GetValidSprite(int index)
    {
        List<Sprite> options = new List<Sprite>(possibleSprites);

        // Remove left neighbor's sprite
        if (index > 0 && crowdImages[index - 1].sprite != null)
        {
            options.Remove(crowdImages[index - 1].sprite);
        }

        // Remove right neighbor's sprite (for consistency on repeated refreshes)
        if (index < crowdImages.Length - 1 && crowdImages[index + 1].sprite != null)
        {
            options.Remove(crowdImages[index + 1].sprite);
        }

        // Fallback if all options removed
        if (options.Count == 0)
            return possibleSprites[Random.Range(0, possibleSprites.Count)];

        return options[Random.Range(0, options.Count)];
    }
}
