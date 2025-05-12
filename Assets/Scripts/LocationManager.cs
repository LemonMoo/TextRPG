using System.Collections.Generic;
using UnityEngine;

public class LocationManager : MonoBehaviour
{
    public Dictionary<string, Location> AllLocations { get; private set; }
    public Location CurrentLocation { get; private set; }
    public Player PlayerCharacter; // Assign your Player GameObject here in the Inspector

    void Awake()
    {
        AllLocations = new Dictionary<string, Location>(System.StringComparer.OrdinalIgnoreCase);
        InitializeLocations();

        if (PlayerCharacter == null)
        {
            PlayerCharacter = FindFirstObjectByType<Player>();
        }

        if (PlayerCharacter == null)
        {
            Debug.LogError("LocationManager: Player character not found!", this);
        }

        if (AllLocations.Count > 0)
        {
            if (AllLocations.ContainsKey("town_square")) // Default start
            {
                // SetCurrentLocation will also trigger the first encounter check
                SetCurrentLocation("town_square");
            }
            else
            {
                var firstLocationEnumerator = AllLocations.Values.GetEnumerator();
                if (firstLocationEnumerator.MoveNext())
                {
                    SetCurrentLocation(firstLocationEnumerator.Current.LocationID);
                }
                else
                {
                    Debug.LogError("No locations defined in LocationManager's InitializeLocations method!", this);
                }
            }
        }
        else
        {
            Debug.LogError("No locations defined in LocationManager! Check InitializeLocations method.", this);
        }
    }

    void InitializeLocations()
    {
        // --- Town Area ---
        Location townSquare = new Location("town_square", "Town Square", "You are in the bustling town square. Cobblestone paths lead in several directions. A fountain gurgles peacefully in the center.");
        Location northRoad = new Location("north_road", "North Road", "A dusty road leading north out of town. Fields stretch to the east and west. The air grows cooler to the north.");
        Location generalStore = new Location("general_store", "General Store", "A cozy shop filled with various goods. A counter stands at the back. A sign reads 'Open'.");

        townSquare.AddExit("north", "north_road");
        townSquare.AddExit("east", "general_store");
        northRoad.AddExit("south", "town_square");
        generalStore.AddExit("west", "town_square");

        AddLocationToManager(townSquare);
        AddLocationToManager(northRoad);
        AddLocationToManager(generalStore);

        // --- Forest Area ---
        // Define encounter chances and possible enemies for these locations
        Location forestEntrance = new Location("forest_entrance", "Forest Entrance", "The road gives way to a dark and ancient forest. A narrow, overgrown path winds north into the gloomy woods. You hear the distant caw of a crow.", 0.3f, 2); // 30% chance, max 2 enemies
        forestEntrance.AddPossibleEnemy(EnemyType.Wolf);
        forestEntrance.AddPossibleEnemy(EnemyType.Goblin);

        Location deepWoods = new Location("deep_woods", "Deep Woods", "You are deep within the woods. Sunlight barely penetrates the thick canopy above. Strange sounds echo around you. Paths lead east and west, and the way back south.", 0.5f, 3); // 50% chance, max 3 enemies
        deepWoods.AddPossibleEnemy(EnemyType.ForestSpider);
        deepWoods.AddPossibleEnemy(EnemyType.Wolf);
        deepWoods.AddPossibleEnemy(EnemyType.Bandit);


        Location ancientGrove = new Location("ancient_grove", "Ancient Grove", "You've stumbled into a serene grove. A circle of moss-covered stones stands silently in the center. The air feels strangely calm here. A path leads west out of the grove.", 0.1f, 1); // Low chance, maybe a unique enemy later
        ancientGrove.AddPossibleEnemy(EnemyType.Goblin); // Placeholder

        Location forestClearing = new Location("forest_clearing", "Forest Clearing", "A small, sun-dappled clearing. Wildflowers grow in patches. A narrow path continues east, and another leads south back into the deeper woods.", 0.2f, 2);
        forestClearing.AddPossibleEnemy(EnemyType.Goblin);
        forestClearing.AddPossibleEnemy(EnemyType.ForestSpider);

        // Connect Forest to Town
        northRoad.AddExit("north", "forest_entrance");
        forestEntrance.AddExit("south", "north_road");

        // Forest Connections
        forestEntrance.AddExit("north", "deep_woods");

        deepWoods.AddExit("south", "forest_entrance");
        deepWoods.AddExit("east", "forest_clearing");
        deepWoods.AddExit("west", "ancient_grove");

        ancientGrove.AddExit("east", "deep_woods");

        forestClearing.AddExit("west", "deep_woods");

        AddLocationToManager(forestEntrance);
        AddLocationToManager(deepWoods);
        AddLocationToManager(ancientGrove);
        AddLocationToManager(forestClearing);
    }

    private void AddLocationToManager(Location loc)
    {
        if (loc == null || string.IsNullOrEmpty(loc.LocationID))
        {
            Debug.LogError("Attempted to add a null or invalid location to LocationManager.", this);
            return;
        }
        if (!AllLocations.ContainsKey(loc.LocationID.ToLower()))
        {
            AllLocations.Add(loc.LocationID.ToLower(), loc);
        }
        else
        {
            Debug.LogWarning($"Location with ID '{loc.LocationID}' already exists in LocationManager. Not adding duplicate.", this);
        }
    }

    public bool SetCurrentLocation(string locationID)
    {
        if (string.IsNullOrEmpty(locationID))
        {
            Debug.LogError("SetCurrentLocation: Provided locationID is null or empty.", this);
            return false;
        }
        if (AllLocations.TryGetValue(locationID.ToLower(), out Location newLocation))
        {
            CurrentLocation = newLocation;
            Debug.Log($"Player internally moved to: {CurrentLocation.Name} (ID: {CurrentLocation.LocationID})", this);

            // --- Trigger Encounter Check ---
            if (GameManager.Instance != null) // Check GameManager instance exists
            {
                Debug.Log($"LocationManager: Checking conditions for encounter trigger. GameManager.CurrentState: {GameManager.Instance.CurrentState}", this);
                if (GameManager.Instance.CurrentState == GameState.Playing)
                {
                    Debug.Log($"LocationManager: Conditions met. Calling GameManager.Instance.TryToTriggerEncounter for {newLocation.Name}", this);
                    GameManager.Instance.TryToTriggerEncounter(newLocation);
                }
                else
                {
                    Debug.Log($"LocationManager: Conditions NOT met for encounter. CurrentState is not Playing.", this);
                }
            }
            else
            {
                Debug.LogWarning("LocationManager: GameManager.Instance is null. Cannot trigger encounter check.", this);
            }
            return true;
        }
        else
        {
            Debug.LogError($"Cannot set current location: Location ID '{locationID}' not found in AllLocations dictionary.", this);
            return false;
        }
    }

    public bool TryMovePlayer(string direction)
    {
        if (CurrentLocation == null)
        {
            Debug.LogError("TryMovePlayer: Cannot move player, CurrentLocation is not set.", this);
            return false;
        }
        if (string.IsNullOrEmpty(direction))
        {
            Debug.LogWarning("TryMovePlayer: Direction is null or empty.", this);
            return false;
        }

        if (CurrentLocation.Exits.TryGetValue(direction.ToLower(), out string destinationID))
        {
            if (SetCurrentLocation(destinationID))
            {
                return true;
            }
            else
            {
                Debug.LogError($"TryMovePlayer: Failed to set location to ID '{destinationID}' even though exit exists. Check if '{destinationID}' was added to AllLocations.", this);
                return false;
            }
        }
        else
        {
            return false;
        }
    }
}
