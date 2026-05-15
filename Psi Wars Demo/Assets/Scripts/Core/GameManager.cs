using UnityEngine;
using UnityEngine.Events;

public enum TurnPhase { Replenish, Draw, Creation, Battle, Cleanup }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── Game state ────────────────────────────────────────────────────────────
    public PlayerState[] Players  { get; private set; }
    public int  CurrentPlayerIndex { get; private set; }
    public TurnPhase CurrentPhase  { get; private set; }
    public bool IsFirstTurn        { get; private set; }
    public bool GameOver           { get; private set; }
    public int  WinnerIndex        { get; private set; }
    public int  TurnNumber         { get; private set; }

    public PlayerState CurrentPlayer  => Players[CurrentPlayerIndex];
    public PlayerState OpponentPlayer => Players[1 - CurrentPlayerIndex];

    // ── Events ────────────────────────────────────────────────────────────────
    public UnityEvent             onGameStateChanged = new UnityEvent();
    public UnityEvent             onPhaseChanged     = new UnityEvent();
    public UnityEvent             onTurnChanged      = new UnityEvent();
    public UnityEvent             onGameOver         = new UnityEvent();

    // ── MonoBehaviour ─────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Game flow ─────────────────────────────────────────────────────────────

    public void StartGame(int hp)
    {
        Players    = new PlayerState[2];
        Players[0] = new PlayerState("Player 1", hp);
        Players[1] = new PlayerState("Player 2", hp);

        Players[0].SetupDecks(CardDatabase.DemoBattleDeck(), CardDatabase.DemoResourceDeck());
        Players[1].SetupDecks(CardDatabase.DemoBattleDeck(), CardDatabase.DemoResourceDeck());

        // Draw 5-card starting hands
        for (int p = 0; p < 2; p++)
            for (int i = 0; i < 5; i++)
            { var c = Players[p].DrawFromBattleDeck(); if (c != null) Players[p].hand.Add(c); }

        // Roll to determine first player
        int r0, r1;
        do { r0 = RollPhysical(); r1 = RollPhysical(); } while (r0 == r1);
        CurrentPlayerIndex = r0 > r1 ? 0 : 1;

        IsFirstTurn = true;
        GameOver    = false;
        TurnNumber  = 1;

        EnterPhase(TurnPhase.Replenish);
    }

    public void AdvancePhase()
    {
        switch (CurrentPhase)
        {
            case TurnPhase.Replenish: EnterPhase(TurnPhase.Draw);     break;
            case TurnPhase.Draw:      EnterPhase(TurnPhase.Creation); break;
            case TurnPhase.Creation:  EnterPhase(TurnPhase.Battle);   break;
            case TurnPhase.Battle:    EnterPhase(TurnPhase.Cleanup);  break;
            case TurnPhase.Cleanup:   EndTurn();                       break;
        }
    }

    private void EnterPhase(TurnPhase phase)
    {
        CurrentPhase = phase;
        switch (phase)
        {
            case TurnPhase.Replenish: ExecReplenish(); break;
            case TurnPhase.Draw:      ExecDraw();      break;
        }
        onPhaseChanged.Invoke();
        onGameStateChanged.Invoke();
    }

    private void ExecReplenish() => CurrentPlayer.ReplenishAll();

    private void ExecDraw()
    {
        // Draw one resource card → place creation unit directly in lab
        var res = CurrentPlayer.DrawFromResourceDeck();
        if (res != null)
        {
            if (res.data.cardType == CardType.CreationUnit)
                CurrentPlayer.creationUnits.Add(res);
            else
                CurrentPlayer.hand.Add(res); // Creation Oracle goes to hand
        }

        // Draw one battle card — skip on the very first turn of the game
        if (!IsFirstTurn)
        {
            var bat = CurrentPlayer.DrawFromBattleDeck();
            if (bat != null) CurrentPlayer.hand.Add(bat);
        }
    }

    private void EndTurn()
    {
        if (CheckWinCondition()) return;
        IsFirstTurn = false;
        CurrentPlayerIndex = 1 - CurrentPlayerIndex;
        TurnNumber++;
        onTurnChanged.Invoke();
        EnterPhase(TurnPhase.Replenish);
    }

    // ── Creation phase actions ────────────────────────────────────────────────

    /// <summary>Play a Battle Unit or Equipment card from hand to the Lab.</summary>
    public bool TryPlayCard(CardInstance card)
    {
        if (CurrentPhase != TurnPhase.Creation) return false;
        if (!CurrentPlayer.hand.Contains(card))  return false;
        if (!CurrentPlayer.CanAfford(card.data)) return false;

        CurrentPlayer.PayCost(card.data);
        CurrentPlayer.hand.Remove(card);

        if (card.data.cardType == CardType.BattleUnit || card.data.cardType == CardType.Equipment)
            CurrentPlayer.lab.Add(card);
        // Power cards: effect handled by GameUI then discarded
        else if (card.data.cardType == CardType.Power)
            CurrentPlayer.discardPile.Add(card);

        onGameStateChanged.Invoke();
        return true;
    }

    /// <summary>Attach an Equipment card from the lab to a Battle Unit.</summary>
    public bool TryAttachEquipment(CardInstance equipment, CardInstance target)
    {
        if (target.equippedItem != null)       return false; // Already equipped
        if (!EquipmentCompatible(equipment, target)) return false;

        target.equippedItem = equipment;
        CurrentPlayer.lab.Remove(equipment);
        onGameStateChanged.Invoke();
        return true;
    }

    private bool EquipmentCompatible(CardInstance eq, CardInstance unit)
    {
        bool typeOk = (eq.data.equipForCreature && unit.data.unitType == UnitType.Creature) ||
                      (eq.data.equipForRobot     && unit.data.unitType == UnitType.Robot)    ||
                      (eq.data.equipForCyborg    && unit.data.unitType == UnitType.Cyborg);
        if (!typeOk) return false;

        // Unit must have > 0 in the same stat the equipment boosts
        if (eq.data.equipCyberBonus   > 0 && unit.data.cyberStrength   == 0) return false;
        if (eq.data.equipPsionicBonus > 0 && unit.data.psionicStrength == 0) return false;
        return true;
    }

    // ── Win condition ─────────────────────────────────────────────────────────

    public bool CheckWinCondition()
    {
        for (int i = 0; i < 2; i++)
        {
            if (Players[i].HasLost)
            {
                GameOver    = true;
                WinnerIndex = 1 - i;
                onGameOver.Invoke();
                return true;
            }
        }
        return false;
    }

    // ── Dice ─────────────────────────────────────────────────────────────────

    public struct DieResult
    {
        public int  cyberBonus;
        public int  psionicBonus;
        public int  physicalBonus;
        public bool canRerollPhysical;
        public bool obliteration;
        public int  roll;
    }

    public static int RollCyber()    => Random.Range(1, 7);
    public static int RollPsionic()  => Random.Range(1, 7);
    public static int RollPhysical() => Random.Range(1, 7);

    public static DieResult ResolveCyberDie(int roll) => roll switch
    {
        1 => new DieResult { cyberBonus = 1, canRerollPhysical = true, roll = roll },
        2 => new DieResult { cyberBonus = 2, roll = roll },
        3 => new DieResult { cyberBonus = 3, roll = roll },
        4 => new DieResult { cyberBonus = 4, roll = roll },
        5 => new DieResult { cyberBonus = 1, physicalBonus = 1, roll = roll },
        6 => new DieResult { cyberBonus = 1, obliteration = true, roll = roll },
        _ => new DieResult { roll = roll }
    };

    public static DieResult ResolvePsionicDie(int roll) => roll switch
    {
        1 => new DieResult { psionicBonus = 1, canRerollPhysical = true, roll = roll },
        2 => new DieResult { psionicBonus = 2, roll = roll },
        3 => new DieResult { psionicBonus = 3, roll = roll },
        4 => new DieResult { psionicBonus = 4, roll = roll },
        5 => new DieResult { psionicBonus = 1, physicalBonus = 1, roll = roll },
        6 => new DieResult { psionicBonus = 1, obliteration = true, roll = roll },
        _ => new DieResult { roll = roll }
    };

    public static DieResult ResolvePhysicalDie(int roll) => roll switch
    {
        1 => new DieResult { physicalBonus = 1, roll = roll },
        2 => new DieResult { physicalBonus = 2, roll = roll },
        3 => new DieResult { physicalBonus = 2, roll = roll },
        4 => new DieResult { physicalBonus = 3, roll = roll },
        5 => new DieResult { physicalBonus = 4, roll = roll },
        6 => new DieResult { physicalBonus = 1, obliteration = true, roll = roll },
        _ => new DieResult { roll = roll }
    };
}
