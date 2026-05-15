using System.Collections.Generic;
using UnityEngine;

public static class CardDatabase
{
    private static List<CardData> _all;

    public static List<CardData> All
    {
        get { if (_all == null) Build(); return _all; }
    }

    public static CardData Get(string id) => All.Find(c => c.id == id);

    // ── Demo deck builders ────────────────────────────────────────────────────

    public static List<CardData> DemoBattleDeck()
    {
        // 3 copies of each unit (9 units × 3 = 27) + 4 equipment pairs = 35 total
        // Trimmed to 25 for demo speed
        string[] ids = {
            "BU_JPA","BU_JPA","BU_JPA",
            "BU_TS", "BU_TS",
            "BU_WR", "BU_WR",
            "BU_HB", "BU_HB","BU_HB",
            "BU_XP", "BU_XP",
            "BU_BM", "BU_BM","BU_BM",
            "BU_LP", "BU_LP",
            "BU_AD", "BU_AD",
            "EQ_KA","EQ_KA",
            "EQ_CO","EQ_PB","EQ_PA","EQ_PA"
        };
        var deck = new List<CardData>();
        foreach (var id in ids) { var c = Get(id); if (c != null) deck.Add(c); }
        return deck;
    }

    public static List<CardData> DemoResourceDeck()
    {
        var deck = new List<CardData>();
        // 6 of each type = 24 cards
        for (int i = 0; i < 6; i++)
        {
            deck.Add(Get("CU_DS"));
            deck.Add(Get("CU_BA"));
            deck.Add(Get("CU_NG"));
            deck.Add(Get("CU_MA"));
        }
        return deck;
    }

    // ── Card definitions ──────────────────────────────────────────────────────

    private static void Build()
    {
        _all = new List<CardData>();

        // === CREATION UNITS ===
        Add(new CardData { id = "CU_DS", cardName = "Digital Splicing",   cardType = CardType.CreationUnit, creationUnitType = CreationUnitType.DigitalSplicing,   description = "Digital Splicing Creation Unit" });
        Add(new CardData { id = "CU_BA", cardName = "Bio-Acceleration",   cardType = CardType.CreationUnit, creationUnitType = CreationUnitType.BioAcceleration,   description = "Bio-Acceleration Creation Unit" });
        Add(new CardData { id = "CU_NG", cardName = "Neurogenesis",       cardType = CardType.CreationUnit, creationUnitType = CreationUnitType.Neurogenesis,       description = "Neurogenesis Creation Unit" });
        Add(new CardData { id = "CU_MA", cardName = "Material Animation", cardType = CardType.CreationUnit, creationUnitType = CreationUnitType.MaterialAnimation,  description = "Material Animation Creation Unit" });

        // === BATTLE UNITS – CREATURES ===
        Add(new CardData {
            id = "BU_JPA", cardName = "Jingwa Psi Avenger",
            cardType = CardType.BattleUnit, unitType = UnitType.Creature,
            cyberStrength = 0, psionicStrength = 3, physicalStrength = 1,
            costNeurogenesis = 2, costBioAcceleration = 1,
            description = "With Psionic win, Opponent reveals one Creature or Cyborg from hand."
        });
        Add(new CardData {
            id = "BU_TS", cardName = "Tangarey Scythe",
            cardType = CardType.BattleUnit, unitType = UnitType.Creature,
            cyberStrength = 0, psionicStrength = 1, physicalStrength = 3,
            costBioAcceleration = 1, costNeurogenesis = 1,
            description = "Creature battle unit."
        });
        Add(new CardData {
            id = "BU_WR", cardName = "Warend Reaper",
            cardType = CardType.BattleUnit, unitType = UnitType.Creature,
            cyberStrength = 0, psionicStrength = 1, physicalStrength = 4,
            costNeurogenesis = 1, costBioAcceleration = 1, costDestroy = 1,
            description = "May roll Red die: 3,4 or Obliterate = +1 Physical; 1 or 2 = -1 Physical."
        });
        Add(new CardData {
            id = "BU_LS", cardName = "Life Snatcher",
            cardType = CardType.BattleUnit, unitType = UnitType.Creature,
            cyberStrength = 0, psionicStrength = 2, physicalStrength = 2,
            costNeurogenesis = 2,
            description = "Creature battle unit."
        });

        // === BATTLE UNITS – ROBOTS ===
        Add(new CardData {
            id = "BU_HB", cardName = "Hydra Bot",
            cardType = CardType.BattleUnit, unitType = UnitType.Robot,
            cyberStrength = 3, psionicStrength = 0, physicalStrength = 2,
            costDigitalSplicing = 2,
            description = "Robot battle unit."
        });
        Add(new CardData {
            id = "BU_XP", cardName = "Xaboy Piercer",
            cardType = CardType.BattleUnit, unitType = UnitType.Robot,
            cyberStrength = 1, psionicStrength = 0, physicalStrength = 4,
            costDigitalSplicing = 1, costMaterialAnimation = 1,
            description = "Robot battle unit."
        });
        Add(new CardData {
            id = "BU_LQ", cardName = "Laquidor Stalker",
            cardType = CardType.BattleUnit, unitType = UnitType.Robot,
            cyberStrength = 2, psionicStrength = 0, physicalStrength = 3,
            costDigitalSplicing = 1, costMaterialAnimation = 1,
            description = "Robot battle unit."
        });

        // === BATTLE UNITS – CYBORGS ===
        Add(new CardData {
            id = "BU_BM", cardName = "Blade Master",
            cardType = CardType.BattleUnit, unitType = UnitType.Cyborg,
            cyberStrength = 2, psionicStrength = 1, physicalStrength = 2,
            costDigitalSplicing = 1, costNeurogenesis = 1,
            description = "Cyborg battle unit."
        });
        Add(new CardData {
            id = "BU_LP", cardName = "Lodner Predator",
            cardType = CardType.BattleUnit, unitType = UnitType.Cyborg,
            cyberStrength = 2, psionicStrength = 3, physicalStrength = 2,
            costDigitalSplicing = 1, costNeurogenesis = 2,
            description = "May roll for attack. If Obliterate, destroy top card of Opponent's Resource Deck."
        });
        Add(new CardData {
            id = "BU_AD", cardName = "Acrobeac Destroyer",
            cardType = CardType.BattleUnit, unitType = UnitType.Cyborg,
            cyberStrength = 1, psionicStrength = 2, physicalStrength = 3,
            costDigitalSplicing = 1, costNeurogenesis = 1, costBioAcceleration = 1,
            description = "If Psionic win, may find next Cyborg in deck. Shuffle and place on top."
        });
        Add(new CardData {
            id = "BU_ZM", cardName = "Zomber Mercenary",
            cardType = CardType.BattleUnit, unitType = UnitType.Cyborg,
            cyberStrength = 1, psionicStrength = 1, physicalStrength = 3,
            costBioAcceleration = 1, costNeurogenesis = 1,
            description = "Cyborg battle unit."
        });

        // === EQUIPMENT ===
        Add(new CardData {
            id = "EQ_KA", cardName = "Kranthard Armour",
            cardType = CardType.Equipment,
            cyberStrength = 0, psionicStrength = 0, physicalStrength = 2,
            costMaterialAnimation = 1,
            equipPhysicalBonus = 2,
            equipForCreature = true, equipForRobot = true, equipForCyborg = true,
            description = "+2 Physical for any Battle Unit."
        });
        Add(new CardData {
            id = "EQ_CO", cardName = "Cyber Onslaught",
            cardType = CardType.Equipment,
            cyberStrength = 1, psionicStrength = 0, physicalStrength = 0,
            costDigitalSplicing = 1,
            equipCyberBonus = 1,
            equipForRobot = true, equipForCyborg = true,
            description = "+1 Cyber for Robots or Cyborgs."
        });
        Add(new CardData {
            id = "EQ_PB", cardName = "Psi Bolster",
            cardType = CardType.Equipment,
            cyberStrength = 0, psionicStrength = 1, physicalStrength = 0,
            costNeurogenesis = 1,
            equipPsionicBonus = 1,
            equipForCreature = true, equipForCyborg = true,
            description = "+1 Psionic for Creatures or Cyborgs."
        });
        Add(new CardData {
            id = "EQ_PA", cardName = "Physical Augmentation",
            cardType = CardType.Equipment,
            cyberStrength = 0, psionicStrength = 0, physicalStrength = 1,
            costBioAcceleration = 1,
            equipPhysicalBonus = 1,
            equipForCreature = true, equipForRobot = true, equipForCyborg = true,
            description = "+1 Physical for any Battle Unit."
        });

        LoadArtwork();
    }

    private static void Add(CardData c) => _all.Add(c);

    // Load sprites from Resources/CardArt/<id>
    private static void LoadArtwork()
    {
        foreach (var card in _all)
        {
            var sprite = Resources.Load<Sprite>($"CardArt/{card.id}");
            if (sprite != null) card.artwork = sprite;
        }
    }
}
