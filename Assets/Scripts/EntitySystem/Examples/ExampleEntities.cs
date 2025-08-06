using UnityEngine;
using GalacticVentures.EntitySystem.Core;

namespace GalacticVentures.EntitySystem.Examples
{
    /// <summary>
    /// Example ship entity for Galactic Ventures
    /// </summary>
    public class Ship : GameEntity
    {
        [SerializeField] private string _shipName = "Unknown Ship";
        [SerializeField] private ShipClass _shipClass = ShipClass.Frigate;

        public string ShipName => _shipName;
        public ShipClass ShipClass => _shipClass;

        protected override void InitializeEntity()
        {
            base.InitializeEntity();
            
            // Ships default to Independent Traders faction if not set
            if (_faction == EntityFaction.None)
            {
                _faction = EntityFaction.IndependentTraders;
            }

            EntitySystemLogger.LogInfo("Ship", $"Ship {_shipName} initialized with class {_shipClass}");
        }

        /// <summary>
        /// Set ship configuration
        /// </summary>
        public void ConfigureShip(string name, ShipClass shipClass, EntityFaction faction = EntityFaction.IndependentTraders)
        {
            _shipName = name;
            _shipClass = shipClass;
            SetFaction(faction);
        }

        /// <summary>
        /// Get ship display name for UI
        /// </summary>
        public string GetDisplayName()
        {
            return $"{_shipName} ({_shipClass})";
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_shipName))
            {
                _shipName = $"Ship_{System.Guid.NewGuid().ToString("N")[..8]}";
            }
        }
#endif
    }

    /// <summary>
    /// Example space station entity
    /// </summary>
    public class SpaceStation : GameEntity
    {
        [SerializeField] private string _stationName = "Unknown Station";
        [SerializeField] private StationType _stationType = StationType.TradingPost;
        [SerializeField] private int _dockingBays = 4;

        public string StationName => _stationName;
        public StationType StationType => _stationType;
        public int DockingBays => _dockingBays;

        protected override void InitializeEntity()
        {
            base.InitializeEntity();
            
            // Stations default to Neutral faction
            if (_faction == EntityFaction.None)
            {
                _faction = EntityFaction.NeutralStations;
            }

            EntitySystemLogger.LogInfo("SpaceStation", $"Station {_stationName} initialized as {_stationType}");
        }

        /// <summary>
        /// Configure station properties
        /// </summary>
        public void ConfigureStation(string name, StationType type, int dockingBays, EntityFaction faction = EntityFaction.NeutralStations)
        {
            _stationName = name;
            _stationType = type;
            _dockingBays = dockingBays;
            SetFaction(faction);
        }

        /// <summary>
        /// Check if station can dock ships
        /// </summary>
        public bool CanDockShips()
        {
            return _dockingBays > 0 && (_stationType == StationType.TradingPost || _stationType == StationType.Starport);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_stationName))
            {
                _stationName = $"Station_{System.Guid.NewGuid().ToString("N")[..8]}";
            }
            _dockingBays = Mathf.Max(0, _dockingBays);
        }
#endif
    }

    /// <summary>
    /// Example planet entity
    /// </summary>
    public class Planet : GameEntity
    {
        [SerializeField] private string _planetName = "Unknown Planet";
        [SerializeField] private PlanetType _planetType = PlanetType.Terrestrial;
        [SerializeField] private long _population = 0;
        [SerializeField] private float _gravity = 1.0f;

        public string PlanetName => _planetName;
        public PlanetType PlanetType => _planetType;
        public long Population => _population;
        public float Gravity => _gravity;

        protected override void InitializeEntity()
        {
            base.InitializeEntity();
            
            // Planets can belong to various factions
            if (_faction == EntityFaction.None)
            {
                _faction = EntityFaction.IndependentTraders; // Independent by default
            }

            EntitySystemLogger.LogInfo("Planet", $"Planet {_planetName} initialized with population {_population:N0}");
        }

        /// <summary>
        /// Configure planet properties
        /// </summary>
        public void ConfigurePlanet(string name, PlanetType type, long population, float gravity, EntityFaction faction)
        {
            _planetName = name;
            _planetType = type;
            _population = population;
            _gravity = gravity;
            SetFaction(faction);
        }

        /// <summary>
        /// Check if planet is habitable
        /// </summary>
        public bool IsHabitable()
        {
            return _planetType == PlanetType.Terrestrial && _gravity > 0.5f && _gravity < 2.0f;
        }

        /// <summary>
        /// Get planet classification for display
        /// </summary>
        public string GetClassification()
        {
            var classification = _planetType.ToString();
            if (IsHabitable())
            {
                classification += " (Habitable)";
            }
            return classification;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_planetName))
            {
                _planetName = $"Planet_{System.Guid.NewGuid().ToString("N")[..8]}";
            }
            _population = Mathf.Max(0, _population);
            _gravity = Mathf.Max(0, _gravity);
        }
#endif
    }

    // Supporting enums
    public enum ShipClass
    {
        Fighter,
        Corvette,
        Frigate,
        Destroyer,
        Cruiser,
        Battleship,
        Carrier,
        Dreadnought
    }

    public enum StationType
    {
        TradingPost,
        MiningStation,
        ResearchFacility,
        MilitaryBase,
        Starport,
        RefuelingStation
    }

    public enum PlanetType
    {
        Terrestrial,
        GasGiant,
        IceWorld,
        DesertWorld,
        OceanWorld,
        VolcanicWorld,
        Asteroid
    }
}