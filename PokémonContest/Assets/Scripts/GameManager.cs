using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class GameManager : MonoBehaviour
{
    public ActivePokemon[] contestants;
    public GameObject[] trainers;
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
    [Range(1, 5)]
    public int round;
    [Range(0, 5)]
    public int contestType;
    public enum ContestLevel { Normal = 0, Super = 1, Hyper = 2, Master = 3 }
    public ContestLevel contestLevel;
    public float contestantsMoveSpeed;
    private int silentStreak = 0; 
    public bool waitingForPlayer;
    public float musicVolume;

    // Crowd
    public GameObject[] crowd;
    public bool react;
    private float crowdTimer = 0f;
    private bool isCheering = false;
    private bool isBooing = false;
    private int cheerLevel;
    public int cheerThreshold;

    public Attack[] attackRoster;
    public Attack[] previousAttackRoster;
    public ActivePokemon crowdCapture;

    private Queue<ContestStage> stageQueue = new Queue<ContestStage>();
    private Coroutine turnRoutine;
    public UIManager uiManager;

    void Start()
    {
        foreach (var mon in contestants)
            mon.OnMoveInFinished = HandleMoveInFinished;

        // Start the intro and music
        audioManager.Play("Contest Music");
        StartCoroutine(ContestIntroSequence());
    }

    IEnumerator MoveTrainerAcross(GameObject trainerObj, System.Action onMidpoint = null)
    {
        RectTransform rt = trainerObj.GetComponent<RectTransform>();

        // PHASE 0 — Start offscreen
        rt.anchoredPosition = new Vector2(-1250f, rt.anchoredPosition.y);

        float enterSpeed = 1600f;   // fast in
        float exitSpeed  = 1600f;   // fast out
        float centerPause = 1.2f;   // how long they stay in center

        // PHASE 1 — Move from -1250 → -500 (trigger text)
        Vector2 midPoint = new Vector2(-500f, rt.anchoredPosition.y);

        while (rt.anchoredPosition.x < -500f)
        {
            rt.anchoredPosition = Vector2.MoveTowards(
                rt.anchoredPosition,
                midPoint,
                enterSpeed * Time.deltaTime
            );

            yield return null;
        }

        // Trigger the textbox here
        onMidpoint?.Invoke();

        // PHASE 2 — Move from -500 → 0 (center)
        Vector2 center = new Vector2(0f, rt.anchoredPosition.y);

        while (rt.anchoredPosition.x < 0f)
        {
            rt.anchoredPosition = Vector2.MoveTowards(
                rt.anchoredPosition,
                center,
                enterSpeed * Time.deltaTime
            );

            yield return null;
        }

        // Pause in center
        yield return new WaitForSeconds(centerPause);

        // PHASE 3 — Move from center → +1250 (exit fast)
        Vector2 exitTarget = new Vector2(1250f, rt.anchoredPosition.y);

        while (rt.anchoredPosition.x < 1200f)
        {
            rt.anchoredPosition = Vector2.MoveTowards(
                rt.anchoredPosition,
                exitTarget,
                exitSpeed * Time.deltaTime
            );

            yield return null;
        }
    }

    IEnumerator ContestIntroSequence()
    {
        // 1. Contest welcome message
        Say($"Welcome to the {(ContestLevel)contestLevel} {(Attack.Type)contestType} Contest!");
        yield return WaitForTextbox();

        Say("Today we have four great trainers competing for our top prize!");
        yield return WaitForTextbox();

        Say("Let's introduce them now!");
        yield return WaitForTextbox();

        // FORCE MAX CROWD CHEER FOR INTRO
        cheerLevel = cheerThreshold + 1;
        react = true;
        isCheering = true;
        isBooing = false;

        // 2. INTRODUCE EACH TRAINER
        for (int i = 0; i < contestants.Length; i++)
        {
            GameObject trainer = trainers[i];

            // Move trainer across the screen FIRST
            yield return StartCoroutine(MoveTrainerAcross(trainer));

            // THEN announce them
            Say($"{contestants[i].trainer} and <color=red>{contestants[i].pokemonName}</color>!");
            yield return WaitForTextbox();

            yield return new WaitForSeconds(1f);
        }

        // Stop intro cheer
        react = false;
        isCheering = false;
        isBooing = false;
        cheerLevel = 0;

        // 3. Move to appeals phase
        Say("It's time to move onto the appeals phase. Trainers, take your positions.");
        yield return WaitForTextbox();

        // 4. ENABLE CONTESTANT UI FIRST
        uiManager.contestantsWindowBackground.SetActive(true);
        uiManager.contestantsWindow.SetActive(true);

        yield return new WaitForSeconds(1f);

        // 5. Now show "Round 1…" *before* opening the attack menu
        Say("Round 1! Which move will be played?");
        yield return WaitForTextbox();

        // 6. FINALLY open the attacks window for player choice
        uiManager.attacksWindow.SetActive(true);

        // Enable turn flow
        waitingForPlayer = true;
        turn = -1;
    }

    void Update()
    {
        crowdTimer += Time.deltaTime;

        if (react && crowdTimer >= Mathf.Lerp(0.05f, 0.001f, cheerLevel / cheerThreshold))
        {
            crowdTimer = 0f;

            GameObject selected = crowd[Random.Range(0, crowd.Length)];

            CrowdMember cm = selected.GetComponent<CrowdMember>();
            if (cm != null)
            {
                if (isBooing && silentStreak > 1)
                    cm.PlayBoo();
                else if (isCheering)
                    cm.PlayJump();
            }
        }

        // SPACE → debug next turn
        if (Input.GetKeyDown(KeyCode.Space))
        {
            for (int i = 1; i < 4; i++)
                attackRoster[i] = contestants[i].attacks[Random.Range(0, contestants[i].attacks.Length)];

            NextTurn();
        }

        // Update player position
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
        audioManager.Play("New Contestant");
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

        // ⭐ PRIORITY EFFECT (Next Turn Manipulation)
        if (CurrentAttack.priority)
        {
            Say($"{PokeNameColored} will appeal earlier next round.");
            yield return WaitForTextbox();

            // Step 1: Try to assign nextTurn = 1
            if (CurrentMon.contestantCard.nextTurn == 0)
            {
                CurrentMon.contestantCard.nextTurn = 1;
            }

            // Step 4: If anyone has 3, bump to 4
            foreach (var c in contestants)
            {
                if (c.contestantCard.nextTurn == 3 && c != CurrentMon)
                    c.contestantCard.nextTurn = 4;
            }

            // Step 3: If anyone has 2, bump to 3
            foreach (var c in contestants)
            {
                if (c.contestantCard.nextTurn == 2 && c != CurrentMon)
                    c.contestantCard.nextTurn = 3;
            }

            // Step 2: If ANYONE already has nextTurn=1, bump them to 2
            foreach (var c in contestants)
            {
                if (c != CurrentMon && c.contestantCard.nextTurn == 1)
                    c.contestantCard.nextTurn = 2;
            }
        }

        // ⭐ CAPTIVATE EFFECT
        if (CurrentAttack.captivates && crowdCapture == null)
        {
            Say($"The crowd was captivated by {PokeNameColored}!");
            yield return WaitForTextbox();

            crowdCapture = CurrentMon;
        }

        // Works well if it’s the same type as the one before
        if (CurrentAttack.sameTypeAppeal)
        {
            if (turn > 0 && attackRoster[turn].type == attackRoster[turn - 1].type)
            {
                CurrentMon.futureScore = CurrentMon.currentScore + 4;  // add 4 more
                yield return AddHeart();

                Say("The appeal was the same type as the one before.");
                yield return WaitForTextbox();
            }
            else
            {
                CurrentMon.futureScore = CurrentMon.currentScore;
                yield return AddHeart();

                Say("It didn't go over quite right...");
                yield return WaitForTextbox();
            }
        }

        // Copies the previous appeal
        if (CurrentAttack.copyAppeal)
        {
            if (turn > 0)
            {
                CurrentMon.futureScore = contestants[turn -1].currentScore;  // copy previous score
                yield return AddHeart();

                Say("It did as well as the previous appeal!");
                yield return WaitForTextbox();
            }
            else
            {
                CurrentMon.futureScore = CurrentMon.currentScore;
                yield return AddHeart();

                Say("It didn't go over quite right...");
                yield return WaitForTextbox();
            }
        }

        // Better if later
        if (CurrentAttack.betterIfLater)
        {
            if (turn == 0)
            {
                CurrentMon.futureScore = CurrentMon.currentScore;
                yield return AddHeart();

                Say("The appeal didn't go so well...");
                yield return WaitForTextbox();
            }
            else if (turn == 1)
            {
                CurrentMon.futureScore = CurrentMon.currentScore + 2;
                yield return AddHeart();

                Say("The appeal went alright.");
                yield return WaitForTextbox();
            }
            else if (turn == 2)
            {
                CurrentMon.futureScore = CurrentMon.currentScore + 4;
                yield return AddHeart();

                Say("The appeal went pretty well.");
                yield return WaitForTextbox();
            }
            else if (turn == 3)
            {
                CurrentMon.futureScore = CurrentMon.currentScore + 6;
                yield return AddHeart();

                Say("The appeal went great!");
                yield return WaitForTextbox();
            }
        }

        // Exhausted
        if (CurrentAttack.exhaust == 2)
        {
            CurrentMon.futureScore = CurrentMon.currentScore;
            yield return AddHeart();

            Say($"The appeal went amazingly, but <color=red>{CurrentMon}</color> is now exhausted.");
            yield return WaitForTextbox();
        }

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
            audioManager.Play("Red Heart", 1 + (CurrentMon.currentScore * 0.05f));
            yield return new WaitForSeconds(0.2f);
        }
    }

    IEnumerator RemoveHeart()
    {
        while (CurrentMon.currentScore > CurrentMon.futureScore)
        {
            CurrentMon.currentScore--;
            audioManager.Play("Black Heart", 1 - (CurrentMon.currentScore * 0.05f));
            yield return new WaitForSeconds(0.2f);
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
            {
                audioManager.Play("Black Heart", 1 - (CurrentMon.currentScore * 0.05f));
                yield return new WaitForSeconds(0.15f);
            }
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
        // Reset reaction flags for this turn
        isCheering = false;
        isBooing = false;

        // Remember previous attack to check for repeats
        if (attackRoster[turn] == previousAttackRoster[turn])
        {
            Say($"The judge is disappointed by the repeated move.");
            yield return WaitForTextbox();

            CurrentMon.futureScore = CurrentMon.currentScore - 2;
            yield return RemoveHeart();

            yield return new WaitForSeconds(2f);
            yield break;
        }

        // ----------- CAPTURED CROWD OVERRIDE -----------
        if (crowdCapture != null && crowdCapture != contestants[turn])
        {
            Say($"The crowd continues to watch <color=red>{crowdCapture.pokemonName}</color>...");
            yield return WaitForTextbox();

            // The crowd is cheering due to the captivation effect
            isCheering = true;
            isBooing = false;

            // Trigger crowd animation for a short period
            yield return new WaitForSeconds(2f);
            yield break; // Skip normal crowd logic
        }

        // Determine crowd reaction normally
        bool matchesType = ((int)CurrentAttack.type == contestType) || CurrentAttack.worksInAnyContest;

        if (matchesType)
        {
            if (cheerLevel == cheerThreshold)
            {
                Say($"The crowd is going absolutely wild for {PokeNameColored}!");
            }
            else
            {
                Say($"The crowd is cheering for {PokeNameColored}!");
            }

            cheerLevel++;
            isCheering = true;
            isBooing = false;
        }
        else
        {
            if (silentStreak >= 1)
            {
                Say($"The crowd is booing {PokeNameColored}!");
                cheerLevel--;
                isBooing = true;
                isCheering = false;
            }
            else
            {
                Say("The crowd is silent...");
                isCheering = false;
                isBooing = false;
            }
        }

        yield return WaitForTextbox();

        // -------- CROWD ANIMATION (Update() handles actual movement) --------
        react = true;
        yield return new WaitForSeconds(1f);

        // ----------- CHEER BONUS / BOO PENALTY -----------
        if (isCheering)
        {
            silentStreak = 0;

            CurrentMon.futureScore = (cheerLevel == cheerThreshold + 1) ? CurrentMon.currentScore + 6 : CurrentMon.currentScore + 1;
            yield return AddHeart();

            if (cheerLevel > cheerThreshold)
            {
                cheerLevel = 0;
            }

            yield return new WaitForSeconds(2f);
        }
        else if (isBooing)
        {
            silentStreak++;

            if (silentStreak >= 2)
            {
                CurrentMon.futureScore = CurrentMon.currentScore - 2;
                yield return RemoveHeart();

                yield return new WaitForSeconds(2f);
            }
        }
        else
        {
            // Completely silent — no streak change
            silentStreak++;
        }

        // End reaction
        yield return new WaitForSeconds(1.0f);
        react = false;

        // ----------- END OF ROUND CHECK -----------
        if (turn == contestants.Length - 1)
        {
            contestants[3].state = 3;
            stageQueue.Enqueue(ContestStage.RoundEnd);
        }
    }

    IEnumerator StageRoundEnd()
    {
        // Move onto the next set of moves
        previousAttackRoster = attackRoster;

        // Reset turn for evaluation
        turn = -1;

        // Reset crowd capture
        crowdCapture = null;

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
                else if (contestants[i].currentScore < 0)
                {
                    contestants[i].currentScore++;
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
        var cards = uiManager.contestantCards.ToList();

        // Step 1: Split by nextTurn category
        var forced = new Dictionary<int, ContestantCard>(); // 1→index0, 2→index1, etc.
        var normal = new List<ContestantCard>();            // nextTurn == 0
        var negative = new List<ContestantCard>();          // nextTurn < 0

        foreach (ContestantCard card in cards)
        {
            int nt = card.nextTurn;

            if (nt >= 1 && nt <= 4)
            {
                forced[nt] = card; // forced placements
            }
            else if (nt == 0)
            {
                normal.Add(card);
            }
            else // nt < 0
            {
                negative.Add(card);
            }
        }

        // Step 2: Sort nextTurn == 0 normally by totalScore, highest first
        normal.Sort((a, b) => b.contestant.totalScore.CompareTo(a.contestant.totalScore));

        // Step 3: Shuffle all negative contestants
        negative = negative.OrderBy(x => UnityEngine.Random.value).ToList();

        // Step 4: Build the final ordered list (4 slots)
        ContestantCard[] finalOrder = new ContestantCard[4];

        // Fill forced placements into exact positions
        foreach (var kv in forced)
        {
            int nt = kv.Key;             // 1–4
            finalOrder[nt - 1] = kv.Value;
        }

        // Step 5: Fill remaining empty slots with normal contestants first
        int fillIndex = 0;
        foreach (var card in normal)
        {
            while (fillIndex < 4 && finalOrder[fillIndex] != null)
                fillIndex++;

            if (fillIndex < 4)
            {
                finalOrder[fillIndex] = card;
                fillIndex++;
            }
        }

        // Step 6: Fill remaining slots with negative (random) contestants
        foreach (var card in negative)
        {
            while (fillIndex < 4 && finalOrder[fillIndex] != null)
                fillIndex++;

            if (fillIndex < 4)
            {
                finalOrder[fillIndex] = card;
                fillIndex++;
            }
        }

        // Step 7: Apply final sibling order
        for (int i = 0; i < 4; i++)
        {
            finalOrder[i].transform.SetSiblingIndex(i);
        }

        // Step 8: Reset nextTurn
        foreach (ContestantCard card in cards)
        {
            card.nextTurn = 0;
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
