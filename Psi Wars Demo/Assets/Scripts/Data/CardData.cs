using UnityEngine;

public enum CardType { CreationUnit, BattleUnit, Equipment, Power }
public enum UnitType { None, Creature, Robot, Cyborg }
public enum CreationUnitType { None, DigitalSplicing, BioAcceleration, Neurogenesis, MaterialAnimation }

[System.Serializable]
public class CardData
{
    public string id;
    public string cardName;
    public CardType cardType;
    public UnitType unitType;
    public CreationUnitType creationUnitType;

    // Battle stats (use -1 to indicate X = roll die to determine)
    public int cyberStrength;
    public int psionicStrength;
    public int physicalStrength;

    // Deplete costs (turn creation units grayscale)
    public int costDigitalSplicing;
    public int costNeurogenesis;
    public int costBioAcceleration;
    public int costMaterialAnimation;
    // White cost: permanently destroy this many un-depleted CUs of a matching type
    public int costDestroy;

    // Equipment stat bonuses
    public int equipCyberBonus;
    public int equipPsionicBonus;
    public int equipPhysicalBonus;

    // Which unit types can use this equipment
    public bool equipForCreature;
    public bool equipForRobot;
    public bool equipForCyborg;

    public string description;

    // Loaded from Resources/CardArt/<id>.png at runtime
    [System.NonSerialized] public Sprite artwork;
}
