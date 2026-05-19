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

        CurrentPlayerIndex = 0; // Player 1 always goes first

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
        // Draw one resource card → always goes to hand (player plays it during Creation phase)
        var res = CurrentPlayer.DrawFromResourceDeck();
        if (res != null) CurrentPlayer.hand.Add(res);

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

    /// <summary>Play a card from hand. Creation Units are free; others pay creation unit costs.</summary>
    public bool TryPlayCard(CardInstance card)
    {
        if (CurrentPhase != TurnPhase.Creation) return false;
        if (!CurrentPlayer.hand.Contains(card))  return false;

        bool isCU = card.data.cardType == CardType.CreationUnit;
        if (!isCU)
        {
            if (!CurrentPlayer.CanAfford(card.data)) return false;
            CurrentPlayer.PayCost(card.data);
        }

        CurrentPlayer.hand.Remove(card);

        if (card.data.cardType == CardType.BattleUnit)
            CurrentPlayer.lab.Add(card);
        else if (isCU)
            CurrentPlayer.creationUnits.Add(card);
        else if (card.data.cardType == CardType.Power)
            CurrentPlayer.discardPile.Add(card);
        // Equipment: must be equipped directly onto a unit via TryEquipFromHand

        onGameStateChanged.Invoke();
        return true;
    }

    /// <summary>Play an Equipment card from hand directly onto a lab unit (pays cost + attaches).</summary>
    public bool TryEquipFromHand(CardInstance equipment, CardInstance target)
    {
        if (CurrentPhase != TurnPhase.Creation)                     return false;
        if (!CurrentPlayer.hand.Contains(equipment))                return false;
        if (equipment.data.cardType != CardType.Equipment)          return false;
        if (!CurrentPlayer.lab.Contains(target))                    return false;
        if (!CurrentPlayer.CanAfford(equipment.data))               return false;
        if (!EquipmentCompatible(equipment, target))                return false;
        if (target.equippedItem != null)                            return false;

        CurrentPlayer.PayCost(equipment.data);
        CurrentPlayer.hand.Remove(equipment);
        target.equippedItem = equipment;
        onGameStateChanged.Invoke();
        return true;
    }

    /// <summary>Stack a Cyborg with a non-Cyborg unit already in the lab.</summary>
    public bool TryStackUnits(CardInstance a, CardInstance b)
    {
        if (CurrentPhase != TurnPhase.Creation && CurrentPhase != TurnPhase.Battle) return false;
        var cyborg    = a.data.unitType == UnitType.Cyborg ? a : b.data.unitType == UnitType.Cyborg ? b : null;
        var nonCyborg = a.data.unitType != UnitType.Cyborg ? a : b.data.unitType != UnitType.Cyborg ? b : null;
        if (cyborg == null || nonCyborg == null || cyborg == nonCyborg) return false;
        if (cyborg.data.unitType != UnitType.Cyborg || nonCyborg.data.unitType == UnitType.Cyborg) return false;
        if (cyborg.stackedWith != null || nonCyborg.stackedWith != null) return false;
        if (!CurrentPlayer.lab.Contains(cyborg) || !CurrentPlayer.lab.Contains(nonCyborg)) return false;

        cyborg.stackedWith    = nonCyborg;
        nonCyborg.stackedWith = cyborg;
        onGameStateChanged.Invoke();
        return true;
    }

    /// <summary>Break a Cyborg+non-Cyborg stack — both units return to independent lab slots.</summary>
    public void UnstackUnit(CardInstance unit)
    {
        if (unit.stackedWith == null) return;
        unit.stackedWith.stackedWith = null;
        unit.stackedWith = null;
        onGameStateChanged.Invoke();
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
