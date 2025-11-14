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
    public Attack[] attackRoster;
    public ActivePokemon crowdCapture;

    private Queue<ContestStage> stageQueue = new Queue<ContestStage>();
    private Coroutine turnRoutine;

    void Start()
    {
        // Subscribe to movement callbacks
        foreach (var mon in contestants)
            mon.OnMoveInFinished = HandleMoveInFinished;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            NextTurn();
    }

    // ------------------------------
    // HELPERS
    // ------------------------------

    ActivePokemon CurrentMon   => contestants[turn];
    Attack CurrentAttack       => attackRoster[turn];
    string PokeNameColored     => $"<color=red>{CurrentMon.pokemonName}</color>";

    void Say(string message)
    {
        textbox.PushMessage(message, null); // Wait logic is handled separately
    }

    IEnumerator WaitForTextbox()
    {
        bool finished = false;

        textbox.onMessageFinished = () => finished = true;

        // Wait until text fully printed
        while (!finished)
            yield return null;

        // Then wait 2 seconds
        yield return new WaitForSeconds(2f);
    }

    // ------------------------------
    // TURN FLOW
    // ------------------------------

    public void NextTurn()
    {
        turn++;

        if (turn >= contestants.Length)
            return;

        // Trigger Pokémon moving in
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

        // Appeal hearts
        CurrentMon.futureScore = CurrentMon.currentScore + CurrentAttack.appeal;
        yield return AddHeart();

        // Secondary message
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

        for (int i = 0; i < turn; i++)
        {
            if (contestants[i].protection == 0)
                contestants[i].futureScore -= CurrentAttack.jam - contestants[i].confidence * 2;

            // remove temporary protection
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

        yield return null;
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
        if (crowdCapture != null)
        {
            Say($"The crowd continues to watch {PokeNameColored}...");
            yield return WaitForTextbox();
        }
        else if ((int)CurrentAttack.type == contestType)
        {
            Say($"The crowd is cheering for {PokeNameColored}!");
            yield return WaitForTextbox();
        }
        else
        {
            Say("The crowd is silent...");
            yield return WaitForTextbox();
        }
    }

    public void EndTurn()
    {
        contestants[turn].state = 3;
        NextTurn();
    }
}
