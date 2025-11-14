using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameManager : MonoBehaviour
{
    public ActivePokemon[] contestants;
    public Textbox textbox;

    public enum ContestStage
    {
        TooNervous   = -1,
        Attack       = 0,
        Jam          = 1,
        Nervous      = 3,
        Protection   = 5,
        Confidence   = 6,
        Crowd        = 7,
    }

    public int turn;
    [Range(0, 5)]
    public int contestType;
    public float contestantsMoveSpeed;
    private int silentStreak = 0; 

    public Attack[] attackRoster;
    public ActivePokemon crowdCapture;

    private Queue<ContestStage> stageQueue = new Queue<ContestStage>();
    private Coroutine turnRoutine;

    void Start()
    {
        foreach (var mon in contestants)
            mon.OnMoveInFinished = HandleMoveInFinished;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            NextTurn();
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
        turn++;

        if (turn >= contestants.Length)
            return;

        contestants[turn].state = 1;
    }

    void HandleMoveInFinished(ActivePokemon mon)
    {
        if (mon != contestants[turn])
            return;

        StartTurnSequence();
    }

    void StartTurnSequence()
    {
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

        if (CurrentAttack.avoid != 0)
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
                contestants[i].futureScore -= CurrentAttack.jam - contestants[i].confidence * 2;

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
            if (Random.Range(0, 8 + contestants[i].confidence * 2 - CurrentAttack.nervous * 2) == 0)
                contestants[i].nervous = true;
        }
    }

    IEnumerator StageProtection()
    {
        CurrentMon.protection = CurrentAttack.avoid;

        if (CurrentAttack.avoid == 1)
        {
            Say($"{PokeNameColored} became partially resistant!");
            yield return WaitForTextbox();
        }
        else if (CurrentAttack.avoid == 2)
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
            cheered = true;   // Counts as cheering so streak resets
        }
        else if ((int)CurrentAttack.type == contestType)
        {
            Say($"The crowd is cheering for {PokeNameColored}!");
            cheered = true;
            silent = false;
        }
        else
        {
            // Silent crowd
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

        // Wait for message to finish + 2 seconds
        yield return WaitForTextbox();

        // ----------- CROWD BONUSES / PENALTIES -----------

        if (cheered)
        {
            // Reset silence streak
            silentStreak = 0;

            // ⭐ CHEER BONUS: +1 heart
            CurrentMon.futureScore = CurrentMon.currentScore + 1;
            yield return AddHeart();     

            // Wait 2 seconds before continuing
            yield return new WaitForSeconds(2f);
        }
        else if (silent)
        {
            // Increase silent streak
            silentStreak++;

            if (silentStreak >= 2)
            {
                // ⭐ BOO PENALTY: -1 heart
                CurrentMon.futureScore = CurrentMon.currentScore - 1;
                yield return RemoveHeart();   // New coroutine below

                yield return new WaitForSeconds(2f);
            }
        }
    }


    public void EndTurn()
    {
        contestants[turn].state = 3;
        NextTurn();
    }
}
