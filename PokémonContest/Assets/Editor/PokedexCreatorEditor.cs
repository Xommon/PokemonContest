#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

[CustomEditor(typeof(PokedexCreator))]
public class PokedexCreatorEditor : Editor
{
    private const string CriesFolder    = "Assets/Resources/Audio/Cries";
    private const string SpritesFolder  = "Assets/Resources/Sprites/Pokemon";
    private const string AttacksFolder  = "Assets/Attacks";
    private const string AttackTemplate = AttacksFolder + "/---.asset";
    private const string PokemonFolder  = "Assets/Pokemon";

    private string ToTitleCase(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return slug;

        slug = slug.Replace("-", " ");

        return string.Join(" ",
            slug.Split(' ')
                .Where(s => s.Length > 0)
                .Select(s => char.ToUpper(s[0]) + s.Substring(1).ToLower())
        );
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var creator = (PokedexCreator)target;

        if (GUILayout.Button("Generate Pokedex"))
        {
            int choice = EditorUtility.DisplayDialogComplex(
                "Generate Pokedex",
                "Would you like to erase all previous data before?\n\n" +
                "Yes = Delete ALL previously downloaded data.\n" +
                "No = Keep existing files and add new ones (no duplicates).\n" +
                "Cancel = Abort.",
                "Yes (Erase All)",     // 0
                "No (Keep Existing)",  // 1
                "Cancel"               // 2
            );

            if (choice == 2)
            {
                Debug.Log("Pokedex generation cancelled.");
                return;
            }

            bool erase = (choice == 0);
            GeneratePokedex(creator, erase);
        }
    }

    // ---------------- MAIN ENTRY ----------------

    private void GeneratePokedex(PokedexCreator creator, bool erase)
    {
        var names = ParseNames(creator.pokedex);
        if (names.Count == 0)
        {
            Debug.LogWarning("PokedexCreator: No names found to process.");
            return;
        }

        try
        {
            EditorUtility.DisplayProgressBar("Pokedex Generator", "Preparing folders...", 0f);

            if (erase)
            {
                PrepareFolders();   // full wipe
            }
            else
            {
                EnsureFolder(CriesFolder);
                EnsureFolder(SpritesFolder);
                EnsureFolder(AttacksFolder);
                EnsureFolder(PokemonFolder);
            }

            var attackTemplate = AssetDatabase.LoadAssetAtPath<Attack>(AttackTemplate);
            if (attackTemplate == null)
            {
                Debug.LogError($"PokedexCreator: Could not find attack template at {AttackTemplate}");
                return;
            }

            float n = names.Count;
            for (int i = 0; i < n; i++)
            {
                string rawName = names[i];
                float progress = (float)i / n;
                EditorUtility.DisplayProgressBar(
                    "Pokedex Generator",
                    $"Processing {rawName} ({i + 1}/{(int)n})",
                    progress);

                try
                {
                    ProcessPokemon(rawName, attackTemplate);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error processing Pokémon '{rawName}': {ex}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log("PokedexCreator: Finished generating content.");
    }

    // ---------------- PARSING INPUT ----------------

    private List<string> ParseNames(string pokedex)
    {
        if (string.IsNullOrWhiteSpace(pokedex))
            return new List<string>();

        return pokedex
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(n => n.Trim())
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();
    }

    private string ToPokeApiSlug(string name)
    {
        return name.Trim().ToLower().Replace(' ', '-');
    }

    // ---------------- FOLDER PREP ----------------

    private void PrepareFolders()
    {
        EnsureFolder(CriesFolder);
        ClearFolderFiles(CriesFolder);

        EnsureFolder(SpritesFolder);
        ClearFolderFiles(SpritesFolder);

        EnsureFolder(AttacksFolder);
        ClearAttackAssetsExceptTemplate();

        EnsureFolder(PokemonFolder);
        ClearPokemonFolder();
    }

    private void EnsureFolder(string relativePath)
    {
        if (!AssetDatabase.IsValidFolder(relativePath))
        {
            string parent = Path.GetDirectoryName(relativePath).Replace("\\", "/");
            string folderName = Path.GetFileName(relativePath);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    private void ClearFolderFiles(string relativePath)
    {
        string fullPath = Path.Combine(Application.dataPath, relativePath.Substring("Assets/".Length));
        if (!Directory.Exists(fullPath))
            return;

        foreach (var file in Directory.GetFiles(fullPath))
        {
            if (file.EndsWith(".meta")) continue;
            File.Delete(file);
        }
    }

    private void ClearAttackAssetsExceptTemplate()
    {
        string fullPath = Path.Combine(Application.dataPath, "Attacks");
        if (!Directory.Exists(fullPath))
            return;

        foreach (var file in Directory.GetFiles(fullPath))
        {
            if (file.EndsWith(".meta")) continue;

            string fileName = Path.GetFileName(file);
            if (fileName == "Attack.cs" || fileName == "---.asset")
                continue;

            string assetPath = $"{AttacksFolder}/{fileName}";
            AssetDatabase.DeleteAsset(assetPath);
        }
    }

    private void ClearPokemonFolder()
    {
        string fullPath = Path.Combine(Application.dataPath, "Pokemon");
        if (!Directory.Exists(fullPath))
            return;

        foreach (var file in Directory.GetFiles(fullPath))
        {
            if (file.EndsWith(".meta")) continue;

            string fileName = Path.GetFileName(file);
            string ext = Path.GetExtension(file);

            // Only delete .asset, keep scripts etc.
            if (!string.Equals(ext, ".asset", StringComparison.OrdinalIgnoreCase))
                continue;

            if (fileName == "Missingno.asset")
                continue;

            string assetPath = $"{PokemonFolder}/{fileName}";
            AssetDatabase.DeleteAsset(assetPath);
        }
    }

    // ---------------- PER-POKÉMON PROCESS ----------------

    private void ProcessPokemon(string rawName, Attack attackTemplate)
    {
        string slug = ToPokeApiSlug(rawName);
        string url  = $"https://pokeapi.co/api/v2/pokemon/{slug}/";

        string json = GetText(url);
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning($"PokedexCreator: No JSON for {rawName} at {url}");
            return;
        }

        var root = JObject.Parse(json);

        string officialName = (string)root["species"]?["name"];
        string speciesUrl   = (string)root["species"]?["url"];

        // Get canon-localized English name
        if (!string.IsNullOrEmpty(speciesUrl))
        {
            string speciesJson  = GetText(speciesUrl);
            var    speciesRoot  = JObject.Parse(speciesJson);
            var    namesArray   = (JArray)speciesRoot["names"];

            if (namesArray != null)
            {
                var enName = namesArray.FirstOrDefault(n =>
                    (string)n["language"]?["name"] == "en");
                if (enName != null)
                    officialName = (string)enName["name"];
            }
        }

        // 1) Cry
        string cryUrl = (string)root["cries"]?["latest"];
        if (!string.IsNullOrEmpty(cryUrl))
        {
            byte[] cryBytes = GetBytes(cryUrl);
            if (cryBytes != null)
            {
                string cryPath = $"{CriesFolder}/{slug}.ogg";
                WriteFile(cryPath, cryBytes);
            }
        }

        // 2) Gen 5 back sprites
        var bwSprites = root["sprites"]?["versions"]?["generation-v"]?["black-white"];

        string spriteUrl      = (string)bwSprites?["back_default"];
        string spriteFemaleUrl = (string)bwSprites?["back_female"];
        string spriteUrlFront      = (string)bwSprites?["front_default"];
        string spriteFemaleUrlFront = (string)bwSprites?["front_female"];

        if (!string.IsNullOrEmpty(spriteUrl))
        {
            byte[] spriteBytes = GetBytes(spriteUrl);
            if (spriteBytes != null)
            {
                string spritePath = $"{SpritesFolder}/{slug}.png";
                WriteFile(spritePath, spriteBytes);
            }
        }

        if (!string.IsNullOrEmpty(spriteFemaleUrl))
        {
            byte[] spriteBytesF = GetBytes(spriteFemaleUrl);
            if (spriteBytesF != null)
            {
                string spritePathF = $"{SpritesFolder}/{slug}-f.png";
                WriteFile(spritePathF, spriteBytesF);
            }
        }

        if (!string.IsNullOrEmpty(spriteUrlFront))
        {
            byte[] spriteBytes = GetBytes(spriteUrlFront);
            if (spriteBytes != null)
            {
                string spritePath = $"{SpritesFolder}/{slug}_front.png";
                WriteFile(spritePath, spriteBytes);
            }
        }

        if (!string.IsNullOrEmpty(spriteFemaleUrlFront))
        {
            byte[] spriteBytes = GetBytes(spriteFemaleUrlFront);
            if (spriteBytes != null)
            {
                string spritePath = $"{SpritesFolder}/{slug}_front-f.png";
                WriteFile(spritePath, spriteBytes);
            }
        }

        // 3) Create Pokémon Asset -----------------------------------
        string pokemonAssetName   = officialName;
        string safePokemonName    = MakeSafeFileName(pokemonAssetName);
        string missingnoPath      = $"{PokemonFolder}/Missingno.asset";
        string newPokemonAssetPath = $"{PokemonFolder}/{safePokemonName}.asset";

        Pokemon pokemonTemplate = AssetDatabase.LoadAssetAtPath<Pokemon>(missingnoPath);
        if (pokemonTemplate == null)
        {
            Debug.LogError("Missingno.asset not found in Assets/Pokemon/");
            return;
        }

        // If this Pokémon asset already exists (e.g. erase = false), reuse & update it
        Pokemon newPokemon = AssetDatabase.LoadAssetAtPath<Pokemon>(newPokemonAssetPath);
        if (newPokemon == null)
        {
            newPokemon = UnityEngine.Object.Instantiate(pokemonTemplate);
            newPokemon.name = pokemonAssetName;   // display name
            AssetDatabase.CreateAsset(newPokemon, newPokemonAssetPath);
        }
        else
        {
            newPokemon.name = pokemonAssetName;
        }

        EditorUtility.SetDirty(newPokemon);

        // 4) Moves -> Attack ScriptableObjects
        List<Attack> attacks = new List<Attack>();
        JArray movesArray = (JArray)root["moves"];
        if (movesArray == null)
            movesArray = new JArray();

        foreach (var moveToken in movesArray)
        {
            try
            {
                string moveName = (string)moveToken["move"]?["name"];
                string moveUrl  = (string)moveToken["move"]?["url"];

                if (string.IsNullOrEmpty(moveName) || string.IsNullOrEmpty(moveUrl))
                    continue;

                Attack atk = CreateAttackAssetForMove(moveName, moveUrl, attackTemplate);
                if (atk != null && !attacks.Contains(atk))
                    attacks.Add(atk);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing move for {officialName}: {ex}");
            }
        }

        // Assign attacks to Pokémon ScriptableObject
        newPokemon.availableAttacks = attacks.ToArray();
        EditorUtility.SetDirty(newPokemon);
    }

    // ---------------- MOVE → ATTACK.ASSET ----------------

    private Attack CreateAttackAssetForMove(string moveName, string moveUrl, Attack template)
    {
        string moveJson = GetText(moveUrl);
        if (string.IsNullOrEmpty(moveJson))
            return null;

        var moveRoot = JObject.Parse(moveJson);

        // 1. Contest type
        string contestTypeName = (string)moveRoot["contest_type"]?["name"];
        if (string.IsNullOrEmpty(contestTypeName))
            return null; // skip moves with no contest type

        // 2. English proper name
        string englishName = null;
        var namesArray = (JArray)moveRoot["names"];
        if (namesArray != null)
        {
            var enNameEntry = namesArray.FirstOrDefault(n =>
                (string)n["language"]?["name"] == "en");
            if (enNameEntry != null)
                englishName = (string)enNameEntry["name"];
        }

        if (string.IsNullOrEmpty(englishName))
            englishName = ToTitleCase(moveName); // fallback

        string safeName  = MakeSafeFileName(englishName);
        string assetPath = $"{AttacksFolder}/{safeName}.asset";

        // 3. Reuse if already exists
        Attack existing = AssetDatabase.LoadAssetAtPath<Attack>(assetPath);
        if (existing != null)
            return existing;

        // 4. Description from contest_effect
        string description = "";
        string effectUrl   = (string)moveRoot["contest_effect"]?["url"];

        if (!string.IsNullOrEmpty(effectUrl))
        {
            string effectJson = GetText(effectUrl);
            if (!string.IsNullOrEmpty(effectJson))
            {
                var effectRoot = JObject.Parse(effectJson);
                var entries    = (JArray)effectRoot["flavor_text_entries"];

                if (entries != null)
                {
                    foreach (var entry in entries)
                    {
                        if ((string)entry["language"]?["name"] == "en")
                        {
                            description = ((string)entry["flavor_text"] ?? "")
                                .Replace("\n", " ")
                                .Replace("\f", " ")
                                .Trim();
                            break;
                        }
                    }
                }
            }
        }

        // 5. Create new Attack asset
        Attack atk = UnityEngine.Object.Instantiate(template);
        atk.name        = englishName;
        atk.description = description;
        atk.type        = MapContestType(contestTypeName);

        AssetDatabase.CreateAsset(atk, assetPath);
        EditorUtility.SetDirty(atk);

        switch (description)
        {
            // Appeal
            case "A highly appealing move.":
                atk.appeal = 4;
                break;

            case "Works well if it’s the same type as the one before.":
                description = "Works well if it's the same type as the one before.";
                atk.appeal = 2;
                atk.sameTypeAppeal = true;
                break;

            case "Makes the appeal as good as those before it.":
                atk.appeal = 0;
                atk.copyAppeal = true;
                break;

            case "The appeal works better the later it is performed.":
                atk.appeal = 0;
                atk.betterIfLater = true;
                break;

            case "Makes a great appeal, but allows no more to the end.":
                description = "A fantastic appeal, but the user can no longer make future appeals.";
                atk.appeal = 8;
                atk.exhaust = 2;
                break;

            case "An appeal that excites the audience in any contest.":
                atk.appeal = 2;
                atk.worksInAnyContest = true;
                break;

            case "Can be repeatedly used without boring the judge.":
                atk.appeal = 3;
                atk.repeatable = true;
                break;

            // Jam
            case "Jams the others, and misses one turn of appeals.":
                atk.appeal = 4;
                atk.jam = 4;
                atk.exhaust = 1;
                break;

            case "Badly startles the Pokémon in front.":
                description = "Badly startles the Pokémon directly before the user.";
                atk.appeal = 1;
                atk.jam = 3;
                break;

            case "Startles all Pokémon that have done their appeals.":
                atk.appeal = 2;
                atk.jam = 1;
                break;

            case "Badly startles all Pokémon that made good appeals.":
                atk.appeal = 1;
                atk.jam = 3;
                break;

            case "Startles Pokémon that made a same-type appeal.":
                atk.appeal = 2;
                atk.jam = 1;
                atk.sameTypeJam = true;
                break;

            case "Badly startles those that have made appeals.":
                atk.appeal = 1;
                atk.jam = 3;
                break;

            case "Startles the Pokémon that has the judge’s attention.":
                description = "Startles the Pokémon that has the judge's attention.";
                atk.appeal = 1;
                atk.jam = 3;
                break;

            // Protection
            case "Can avoid being startled by others.":
                atk.appeal = 1;
                atk.protection = 2;
                break;

            case "Can avoid being startled by others once.":
                atk.appeal = 2;
                atk.protection = 1;
                break;

            // Confidence
            case "Ups the user’s condition.  Helps prevent nervousness.":
                description = "Raises the user's confidence, making them harder to startle.";
                atk.appeal = 1;
                atk.confidence = 1;
                break;

            case "After this move, the user is more easily startled.":
                description = "Lowers the user's confidence, making them easier to startle.";
                atk.appeal = 6;
                atk.confidence = -1;
                break;

            case "Worsens the condition of those that made appeals.":
                description = "Lowers the confidence of those who made appeals";
                atk.appeal = 1;
                atk.lowerConfidence = true;
                break;

            // Priority
            case "Scrambles up the order of appeals on the next turn.":
                atk.appeal = 3;
                atk.mixUp = true;
                break;

            case "The next appeal can be made earlier next turn.":
                atk.appeal = 3;
                atk.priority = true;
                break;

            case "The next appeal can be made later next turn.":
                atk.appeal = 3;
                atk.notPriority = true;
                break;

            // Order
            case "The appeal works great if performed last.":
                atk.appeal = 2;
                atk.lastAppeal = true;
                break;

            case "The appeal works great if performed first.":
                atk.appeal = 2;
                atk.firstAppeal = true;
                break;

            // Unnerve
            case "Makes all Pokémon after the user nervous.":
                atk.appeal = 2;
                atk.nervous = 1;
                break;

            // Captivates
            case "Temporarily stops the crowd from getting excited.":
            case "Shifts the judge’s attention from others.":
                description = "Prevents the crowd from reacting to other Pokémon's appeals.";
                atk.appeal = 3;
                atk.captivates = true;
                break;

            


                // Add as many cases as you want
        }

        return atk;
    }

    private Attack.Type MapContestType(string contestTypeName)
    {
        switch (contestTypeName)
        {
            case "cool":   return Attack.Type.Cool;
            case "beauty": return Attack.Type.Beauty;
            case "smart":  return Attack.Type.Smart;
            case "tough":  return Attack.Type.Tough;
            case "cute":   return Attack.Type.Cute;
            default:
                Debug.LogWarning($"Unknown contest type '{contestTypeName}', defaulting to Cool.");
                return Attack.Type.Cool;
        }
    }

    private string MakeSafeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        foreach (var c in invalid)
            name = name.Replace(c, '_');
        return name;
    }

    // ---------------- HTTP HELPERS ----------------

    private string GetText(string url)
    {
        using (var req = UnityWebRequest.Get(url))
        {
            var op = req.SendWebRequest();
            while (!op.isDone) { }

#if UNITY_2020_1_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogWarning($"HTTP error GET {url}: {req.error}");
                return null;
            }

            return req.downloadHandler.text;
        }
    }

    private byte[] GetBytes(string url)
    {
        using (var req = UnityWebRequest.Get(url))
        {
            var op = req.SendWebRequest();
            while (!op.isDone) { }

#if UNITY_2020_1_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogWarning($"HTTP error GET {url}: {req.error}");
                return null;
            }

            return req.downloadHandler.data;
        }
    }

    private void WriteFile(string assetPath, byte[] data)
    {
        string fullPath = Path.Combine(Application.dataPath,
            assetPath.Substring("Assets/".Length));

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        File.WriteAllBytes(fullPath, data);
        AssetDatabase.ImportAsset(assetPath);
    }
}

#endif
