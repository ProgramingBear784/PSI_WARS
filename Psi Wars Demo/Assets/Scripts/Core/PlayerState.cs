using System.Collections.Generic;
using UnityEngine;

public class PlayerState
{
    public string playerName;
    public int labHP;
    public int maxHP;

    public List<CardInstance> battleDeck    = new List<CardInstance>();
    public List<CardInstance> resourceDeck  = new List<CardInstance>();
    public List<CardInstance> hand          = new List<CardInstance>();
    public List<CardInstance> lab           = new List<CardInstance>(); // Active battle units + unattached equipment
    public List<CardInstance> creationUnits = new List<CardInstance>(); // Creation units in play
    public List<CardInstance> discardPile   = new List<CardInstance>();

    public PlayerState(string name, int startHP)
    {
        playerName = name;
        labHP = maxHP = startHP;
    }

    public void SetupDecks(List<CardData> battleCards, List<CardData> resourceCards)
    {
        battleDeck.Clear();
        resourceDeck.Clear();
        foreach (var c in battleCards)   battleDeck.Add(new CardInstance(c));
        foreach (var c in resourceCards) resourceDeck.Add(new CardInstance(c));
        Shuffle(battleDeck);
        Shuffle(resourceDeck);
    }

    // ── Deck operations ───────────────────────────────────────────────────────

    public CardInstance DrawFromBattleDeck()
    {
        if (battleDeck.Count == 0) return null;
        int last = battleDeck.Count - 1;
        var card = battleDeck[last];
        battleDeck.RemoveAt(last);
        return card;
    }

    public CardInstance DrawFromResourceDeck()
    {
        if (resourceDeck.Count == 0) return null;
        int last = resourceDeck.Count - 1;
        var card = resourceDeck[last];
        resourceDeck.RemoveAt(last);
        return card;
    }

    // ── Resource helpers ──────────────────────────────────────────────────────

    public int AvailableCUs(CreationUnitType type) =>
        creationUnits.FindAll(c => c.data.creationUnitType == type && !c.isDepleted).Count;

    public bool CanAfford(CardData card)
    {
        if (AvailableCUs(CreationUnitType.DigitalSplicing)   < card.costDigitalSplicing)   return false;
        if (AvailableCUs(CreationUnitType.Neurogenesis)       < card.costNeurogenesis)       return false;
        if (AvailableCUs(CreationUnitType.BioAcceleration)    < card.costBioAcceleration)    return false;
        if (AvailableCUs(CreationUnitType.MaterialAnimation)  < card.costMaterialAnimation)  return false;

        if (card.costDestroy > 0)
        {
            // After depletion, we need additional un-depleted CUs of a matching type to destroy
            int dsAfter  = AvailableCUs(CreationUnitType.DigitalSplicing)  - card.costDigitalSplicing;
            int ngAfter  = AvailableCUs(CreationUnitType.Neurogenesis)     - card.costNeurogenesis;
            int baAfter  = AvailableCUs(CreationUnitType.BioAcceleration)  - card.costBioAcceleration;
            int maAfter  = AvailableCUs(CreationUnitType.MaterialAnimation)- card.costMaterialAnimation;

            int destroyable = 0;
            if (card.costDigitalSplicing  > 0) destroyable += dsAfter;
            if (card.costNeurogenesis     > 0) destroyable += ngAfter;
            if (card.costBioAcceleration  > 0) destroyable += baAfter;
            if (card.costMaterialAnimation> 0) destroyable += maAfter;
            if (destroyable < card.costDestroy) return false;
        }
        return true;
    }

    public void PayCost(CardData card)
    {
        DepleteCUs(CreationUnitType.DigitalSplicing,  card.costDigitalSplicing);
        DepleteCUs(CreationUnitType.Neurogenesis,      card.costNeurogenesis);
        DepleteCUs(CreationUnitType.BioAcceleration,   card.costBioAcceleration);
        DepleteCUs(CreationUnitType.MaterialAnimation, card.costMaterialAnimation);

        // Destroy cost: remove additional un-depleted CUs (auto-select first available)
        int toDestroy = card.costDestroy;
        var typesToDestroy = new List<CreationUnitType>();
        if (card.costDigitalSplicing  > 0) typesToDestroy.Add(CreationUnitType.DigitalSplicing);
        if (card.costNeurogenesis     > 0) typesToDestroy.Add(CreationUnitType.Neurogenesis);
        if (card.costBioAcceleration  > 0) typesToDestroy.Add(CreationUnitType.BioAcceleration);
        if (card.costMaterialAnimation> 0) typesToDestroy.Add(CreationUnitType.MaterialAnimation);

        for (int i = creationUnits.Count - 1; i >= 0 && toDestroy > 0; i--)
        {
            var cu = creationUnits[i];
            if (!cu.isDepleted && typesToDestroy.Contains(cu.data.creationUnitType))
            {
                discardPile.Add(cu);
                creationUnits.RemoveAt(i);
                toDestroy--;
            }
        }
    }

    private void DepleteCUs(CreationUnitType type, int count)
    {
        int done = 0;
        foreach (var cu in creationUnits)
        {
            if (done >= count) break;
            if (cu.data.creationUnitType == type && !cu.isDepleted) { cu.isDepleted = true; done++; }
        }
    }

    // ── Phase helpers ─────────────────────────────────────────────────────────

    public void ReplenishAll()
    {
        foreach (var cu in creationUnits) cu.isDepleted = false;
        foreach (var bu in lab)           { bu.isDisoriented = false; }
    }

    public void DestroyUnit(CardInstance unit)
    {
        // Move unit and any attached equipment to discard
        if (unit.equippedItem != null) discardPile.Add(unit.equippedItem);
        discardPile.Add(unit);
        lab.Remove(unit);
    }

    // ── Win condition ─────────────────────────────────────────────────────────

    public bool HasLost =>
        labHP <= 0 ||
        (battleDeck.Count == 0 &&
         hand.FindAll(c => c.data.cardType == CardType.BattleUnit).Count == 0 &&
         lab.FindAll(c  => c.data.cardType == CardType.BattleUnit).Count == 0);

    // ── Utility ───────────────────────────────────────────────────────────────

    private static void Shuffle(List<CardInstance> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
