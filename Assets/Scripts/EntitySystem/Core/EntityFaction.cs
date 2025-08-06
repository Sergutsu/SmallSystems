using System;

namespace GalacticVentures.EntitySystem.Core
{
    /// <summary>
    /// Defines the various factions that entities can belong to in the game
    /// </summary>
    [Serializable]
    public enum EntityFaction
    {
        None = 0,
        Player = 1,
        TradingGuild = 2,
        MilitaryAlliance = 3,
        PirateClans = 4,
        ScientificConsortium = 5,
        IndependentTraders = 6,
        CorporateConglomerate = 7,
        RebellionForces = 8,
        AlienSpecies = 9,
        NeutralStations = 10
    }
}