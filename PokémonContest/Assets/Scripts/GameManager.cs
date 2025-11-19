using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameManager : MonoBehaviour
{
    public ActivePokemon[] contestants;
    public Textbox textbox;
    public AudioManager audioManager;

    public enum ContestStage
    {
        TooNervous = -1,
        Attack = 0,
        Jam = 1,
        Nervous = 3,
        Protection = 5,
        Confidence = 6,
        Crowd = 7,
        RoundEnd = 8
    }


    public int turn;
    public int playerPosition;
    public int round;
    [Range(0, 5)]
    public int contestType;
    public float contestantsMoveSpeed;
    private int silentStreak = 0; 
    public bool waitingForPlayer;
    public float musicVolume;


    public Attack[] attackRoster;
    public ActivePokemon crowdCapture;

    private Queue<ContestStage> stageQueue = new Queue<ContestStage>();
    private Coroutine turnRoutine;
    public UIManager uiManager;

    void Start()
    {
        foreach (var mon in contestants)
            mon.OnMoveInFinished = HandleMoveInFinished;

        audioManager.Play("Contest Music");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            NextTurn();

        // Get player position
        for (int i = 0; i < 4; i++)
        {
            if (contestants[i].trainer == "Micheal")
            {
                playerPosition = i;
                break;
            }
        }
    }

    ActivePokemon CurrentMon   => contestants[turn];
    Attack CurrentAttack       => attackRoster[turn];
    string PokeNameColored     => $"<color=red>{CurrentMon.pokemonName}</color>";

    void Say(string message)
    {
        textbox.PushMessage(message, null);
    }

    IEnumerator WaitForTextbox()
    {
        bool finished = false;

        textbox.onMessageFinished = () => finished = true;

        while (!finished)
            yield return null;

        yield return new WaitForSeconds(2f);
    }

    public void NextTurn()
    {
        if (waitingForPlayer)
            return;  // STOP ALL TURN FLOW

        turn++;

        if (turn >= contestants.Length)
            return;

        contestants[turn].state = 1;
    }

    public void ForceNextTurnFromUI()
    {
        waitingForPlayer = false;   // ✔ Now allow turn to start

        foreach (var mon in contestants)
            mon.state = 0;

        turn = playerPosition;
        contestants[turn].state = 1; // triggers next turn properly
    }

    void HandleMoveInFinished(ActivePokemon mon)
    {
        if (mon != contestants[turn])
            return;

        StartTurnSequence();
    }

    void StartTurnSequence()
    {
        // Prevent round from running
        if (waitingForPlayer)
            return;   
        
        BuildStageQueue();

        if (turnRoutine != null)
            StopCoroutine(turnRoutine);

        turnRoutine = StartCoroutine(RunTurnPipeline());
    }

    void BuildStageQueue()
    {
        stageQueue.Clear();

        if (CurrentMon.nervous)
        {
            stageQueue.Enqueue(ContestStage.TooNervous);
            return;
        }

        stageQueue.Enqueue(ContestStage.Attack);

        if (CurrentAttack.jam > 0)
            stageQueue.Enqueue(ContestStage.Jam);

        if (CurrentAttack.nervous > 0)
            stageQueue.Enqueue(ContestStage.Nervous);

        if (CurrentAttack.protection != 0)
            stageQueue.Enqueue(ContestStage.Protection);

        if (CurrentAttack.confidence != 0)
            stageQueue.Enqueue(ContestStage.Confidence);

        stageQueue.Enqueue(ContestStage.Crowd);
    }

    IEnumerator RunTurnPipeline()
    {
        while (stageQueue.Count > 0)
        {
            var stage = stageQueue.Dequeue();
            yield return ExecuteStage(stage);
        }

        EndTurn();
    }

    IEnumerator ExecuteStage(ContestStage stage)
    {
        switch (stage)
        {
            case ContestStage.TooNervous:
                Say($"{PokeNameColored} is too nervous to move.");
                yield return WaitForTextbox();

                // ⭐ If this is the LAST contestant, end the round
                if (turn == contestants.Length - 1)
                {
                    stageQueue.Enqueue(ContestStage.RoundEnd);
                    yield break;
                }

                // Normal behavior
                yield break;

            case ContestStage.Attack:
                yield return StageAttackAndAppeal();
                break;

            case ContestStage.Jam:
                yield return StageJam();
                break;

            case ContestStage.Nervous:
                yield return StageNervous();
                break;

            case ContestStage.Protection:
                yield return StageProtection();
                break;

            case ContestStage.Confidence:
                yield return StageConfidence();
                break;

            case ContestStage.Crowd:
                yield return StageCrowd();
                break;

            case ContestStage.RoundEnd:
                yield return StageRoundEnd();
                break;
        }
    }

    // ------------------------------
    // STAGE IMPLEMENTATIONS
    // ------------------------------

    IEnumerator StageAttackAndAppeal()
    {
        string attackName = CurrentAttack.ToString().Replace(" (Attack)", "");

        Say($"{PokeNameColored} used {attackName}!");
        yield return WaitForTextbox();

        // Base appeal gain
        CurrentMon.futureScore = CurrentMon.currentScore + CurrentAttack.appeal;
        yield return AddHeart();

        // ⭐ NEW: First Appeal Bonus
        if (CurrentAttack.firstAppeal && turn == 0)
        {
            CurrentMon.futureScore = CurrentMon.currentScore + 3;  // add 3 more
            yield return AddHeart();

            Say("The first appeal was done very well.");
            yield return WaitForTextbox();
        }

        // ⭐ NEW: Last Appeal Bonus
        if (CurrentAttack.lastAppeal && turn == contestants.Length - 1)
        {
            CurrentMon.futureScore = CurrentMon.currentScore + 3;  // add 3 more
            yield return AddHeart();

            Say("The last appeal was done very well.");
            yield return WaitForTextbox();
        }

        // Secondary messages (jam/nervous)
        if (CurrentAttack.jam > 0)
        {
            Say($"{PokeNameColored} tried to startle the others!");
            yield return WaitForTextbox();
        }
        else if (CurrentAttack.nervous > 0)
        {
            Say($"{PokeNameColored} tried to make others nervous!");
            yield return WaitForTextbox();
        }
    }

    IEnumerator AddHeart()
    {
        while (CurrentMon.currentScore < CurrentMon.futureScore)
        {
            CurrentMon.currentScore++;
            yield return new WaitForSeconds(0.15f);
        }
    }

    IEnumerator RemoveHeart()
    {
        while (CurrentMon.currentScore > CurrentMon.futureScore)
        {
            CurrentMon.currentScore--;
            yield return new WaitForSeconds(0.15f);
        }
    }

    IEnumerator StageJam()
    {
        if (turn == 0)
        {
            Say("But it failed!");
            yield return WaitForTextbox();

            if (CurrentAttack.nervous > 0)
            {
                Say($"{PokeNameColored} tried to make others nervous!");
                yield return WaitForTextbox();
            }
            yield break;
        }

        // Affect contestants BEFORE this one
        for (int i = 0; i < turn; i++)
        {
            if (contestants[i].protection == 0)
                contestants[i].futureScore -= CurrentAttack.jam - contestants[i].confidence;

            if (contestants[i].protection == 1)
                contestants[i].protection = 0;
        }

        yield return SubtractHeart();
    }

    IEnumerator SubtractHeart()
    {
        bool keepGoing = true;

        while (keepGoing)
        {
            keepGoing = false;

            for (int i = 0; i < turn; i++)
            {
                if (contestants[i].futureScore < contestants[i].currentScore)
                {
                    contestants[i].currentScore--;
                    keepGoing = true;
                }
            }

            if (keepGoing)
                yield return new WaitForSeconds(0.15f);
        }

        if (CurrentAttack.nervous > 0)
        {
            Say($"{PokeNameColored} tried to make others nervous!");
            yield return WaitForTextbox();
        }
    }

    IEnumerator StageNervous()
    {
        if (turn == contestants.Length - 1)
        {
            Say("But it failed!");
            yield return WaitForTextbox();
            yield break;
        }

        for (int i = turn + 1; i < contestants.Length; i++)
        {
            bool shouldAffect = true;

            // ⭐ NEW: Gender-based nervous effects (corrected: uses TARGET data)
            if (CurrentAttack.genderBased)
            {
                int targetSex = contestants[i].sex;           // 0 = male, 1 = female
                int targetOrientation = contestants[i].orientation; // 0 = straight, 1 = gay
                int userSex = CurrentMon.sex;                 // user still needed for comparison

                if (targetOrientation == 0)
                {
                    // Straight target → affected ONLY by opposite sex
                    shouldAffect = (targetSex != userSex);
                }
                else if (targetOrientation == 1)
                {
                    // Gay/lesbian target → affected ONLY by same sex
                    shouldAffect = (targetSex == userSex);
                }
            }

            if (!shouldAffect)
                continue;

            // ORIGINAL nervous chance applies if target qualifies
            if (Random.Range(0, 8 + contestants[i].confidence - CurrentAttack.nervous * 2) == 0)
                contestants[i].nervous = true;
        }

        yield return null;
    }

    IEnumerator StageProtection()
    {
        CurrentMon.protection = CurrentAttack.protection;

        if (CurrentAttack.protection == 1)
        {
            Say($"{PokeNameColored} became partially resistant!");
            yield return WaitForTextbox();
        }
        else if (CurrentAttack.protection == 2)
        {
            Say($"{PokeNameColored} became fully protected!");
            yield return WaitForTextbox();
        }
    }

    IEnumerator StageConfidence()
    {
        CurrentMon.confidence += CurrentAttack.confidence;

        if (CurrentAttack.confidence == 1)
        {
            Say($"{PokeNameColored} became more confident!");
            yield return WaitForTextbox();
        }
        else if (CurrentAttack.confidence == -1)
        {
            Say($"{PokeNameColored} became less confident!");
            yield return WaitForTextbox();
        }
    }

    IEnumerator StageCrowd()
    {
        bool cheered = false;
        bool silent = false;

        // ----------- CROWD REACTION -----------

        if (crowdCapture != null)
        {
            Say($"The crowd continues to watch {PokeNameColored}...");
            silent = false;
            cheered = true;
        }
        else if ((int)CurrentAttack.type == contestType)
        {
            Say($"The crowd is cheering for {PokeNameColored}!");
            cheered = true;
            silent = false;
        }
        else
        {
            if (silentStreak >= 1)
            {
                Say($"The crowd is booing {PokeNameColored}!");
            }
            else
            {
                Say("The crowd is silent...");
            }

            silent = true;
            cheered = false;
        }

        // Wait for textbox + 2 seconds
        yield return WaitForTextbox();

        // ----------- CHEER BONUS / BOO PENALTY -----------

        if (cheered)
        {
            silentStreak = 0;

            CurrentMon.futureScore = CurrentMon.currentScore + 1;
            yield return AddHeart();

            yield return new WaitForSeconds(2f);
        }
        else if (silent)
        {
            silentStreak++;

            if (silentStreak >= 2)
            {
                CurrentMon.futureScore = CurrentMon.currentScore - 1;
                yield return RemoveHeart();

                yield return new WaitForSeconds(2f);
            }
        }

        // ----------- END OF ROUND CHECK -----------

        if (turn == contestants.Length - 1)
        {
            contestants[3].state = 3;
            stageQueue.Enqueue(ContestStage.RoundEnd);
        }
    }

    IEnumerator StageRoundEnd()
    {
        // Reset turn for evaluation
        turn = -1;

        yield return new WaitForSeconds(2f);

        // Player score summary
        ActivePokemon player = contestants[playerPosition];
        string msg = "";

        int s = player.currentScore;

        if (s < 0)
            msg = $"{player.pokemonName} didn't do so well this time.";
        else if (s == 0)
            msg = $"{player.pokemonName} failed to stand out at all.";
        else if (s >= 1 && s <= 3)
            msg = $"{player.pokemonName} caught a little attention.";
        else if (s >= 4 && s <= 6)
            msg = $"{player.pokemonName} caught quite a bit of attention.";
        else if (s >= 7 && s <= 10)
            msg = $"{player.pokemonName} caught a lot of attention.";
        else if (s > 10)
            msg = $"{player.pokemonName} commanded total attention.";

        Say(msg);
        yield return WaitForTextbox(); // includes 2 sec wait

        // Add currentScore → totalScore for ALL contestants
        for (int i = 0; i < contestants.Length; i++)
            contestants[i].totalScore += contestants[i].currentScore;

        yield return new WaitForSeconds(1f);

        // ⭐ RESET ROUND: remove hearts until all = 0
        bool heartsRemaining = true;

        while (heartsRemaining)
        {
            heartsRemaining = false;

            for (int i = 0; i < contestants.Length; i++)
            {
                if (contestants[i].currentScore > 0)
                {
                    contestants[i].currentScore--;
                    heartsRemaining = true;
                }
            }

            // Only wait between each “global tick”
            if (heartsRemaining)
                yield return new WaitForSeconds(0.15f);
        }

        yield return new WaitForSeconds(1f);

        // Reorder UI cards from highest to lowest totalScore
        SortContestantCards();

        yield return new WaitForSeconds(1f);

        // --------------------------
        // SETUP NEXT ROUND
        // --------------------------

        string roundMsg = "";

        if (round == 4)
            roundMsg = "<color=red>Final round. <color=black>Which move will be played?";
        else
            roundMsg = $"Round {round + 1}! Which move will be played?";

        Say(roundMsg);
        yield return WaitForTextbox();

        // Enable UI for next move selection
        uiManager.attacksWindow.SetActive(true);

        // Increment player round value
        round++;

        // Reset turn
        waitingForPlayer = true;
        turn = 0;
        // Stop ALL Pokémon movement for the round reset
        foreach (var mon in contestants)
            mon.state = 0;
        yield break;
    }

    void SortContestantCards()
    {
        // Sort contestants array
        System.Array.Sort(uiManager.contestantCards, (a, b) => b.contestant.totalScore.CompareTo(a.contestant.totalScore));

        // Reapply sibling order (top = highest)
        for (int i = 0; i < 4; i++)
        {
            uiManager.contestantCards[i].transform.SetSiblingIndex(i);
        }
    }

    public void EndTurn()
    {
        // Do NOT continue turns if waiting for the player
        if (waitingForPlayer)
            return;

        contestants[turn].state = 3;
        NextTurn();
    }
}
