# <div align="center">Dungeon Miner</div>

This project is the assignment of the course "AI for videogames" at the University of Milan.

<div align="center">

| Assignment date | Deadline | Delivery date |
| :---: | :---: | :---: |
| 04-06-2024 | 27-06-2024 | 27-06-2024 |
</div>

<div align="center">

## Assignment text: 3D Dungeon Miner
    
</div>

### Goal
The goal of this project is to create an agent that can dig a dungeon inside a 3D volume.

### Setup
The project is an extension of the example shown in lecture A10 (slide 21), with a single agent digging corridors in different directions and placing rectangular rooms, avoiding intersections between existing corridors and rooms. The dungeon is created inside a 3D volume (a cube of dimension 100x101x100 meters, composed by voxels of dimension 1x1x1 meters). The volume should be seen as a set of vertically stacked square layers: the agent starts digging horizontally from the bottom layer (level 0), moving up vertically inside the volume when it can not create new corridors or rooms inside the current layer without intersections. The algorithm stops when the agent reaches the top of the cube (layer 101) and it is not able to create new rooms or corridors without intersections. 

The agent starts at layer 0, in a random position at 15 mt max from the center, and it startsby placing a room following the rules described in the Constraints section. It then chooses a random direction inside the horizontal layer (-X, +X, -Z or +Z, assuming Y to be the vertical world axis as in Unity), and it starts digging a corridor. The length of each corridor should follow the rules described in the Constraints section.

After digging the first corridor, the agent randomly chooses if:

* to place a room (45% probability). All the new rooms must satisfy the constraints described below. A room can be created only if it can fit the available space between the current agent position, and the sides of the current layer, or if the new room does not intersect other rooms and corridors. Otherwise, the agent will switch to the creation of a new corridor. If the creation of the corridor also fails, the agent will move 2 positions up inside the digging volume, and it starts digging inside a new horizontal layer.
* to go on digging a new corridor (45% probability). A new direction inside the current horizontal layer and a new length must be chosen randomly, satisfying the constraints described below. The corridor can be created only if there is sufficient space between the current agent position and the side of the current layer in that direction, or if the new corridor does not intersect other rooms and corridors. Otherwise, the agent will choose a different direction and length for the corridor. If, after 3 attempts, the agent is not able to create a corridor, it will switch to the creation of a new room. In any case, after a maximum of 3 consecutive corridors, the agent tries to place a new room. If the creation of the room also fails, the agent will move 2 positions up inside the digging volume, and it starts digging inside a new horizontal layer.
* to change the layer (10% probability). The agent moves 2 positions up inside the digging volume (i.e., it moves up 2 units on the positive Y axis, assuming Y to be the vertical world axis as in Unity) and it starts digging a new horizontal layer. It moves 2 positions up in order to have a “floor” layer of voxels between the different digging areas. On the new layer, the agent starts the digging process by creating a new room, and then it proceeds randomly as described before. The agent changes the layer also if it is not able to create a corridor or a room in the current layer without intersections.

Graphically:

* set the initial state of each voxel to middle gray color, with a 30%-50% transparency
* set the color of a dug voxel to white with no transparency
* set the color of the voxel corresponding to the agent position to blue with no transparency
* when the agent moves up inside the digging volume, set to red with no transparency the color of the voxel where the agent leaves the layer. Set to red also the color of the voxel in the “floor” layer. Set to green with no transparency the color of the voxel where the agent starts digging inside the new layer.

### Constraints

Each side of a rectangular room must be computed randomly in a range between 3 and 21 meters.

The length of a new corridor must be computed randomly in a range between 3 and 10 meters.

The new direction for a new corridor should be different from the last one used (i.e., the agent can not go backward along an already dug corridor or room).

<div align="center">
    
## Assignement Analysis

</div>

Summing up the assignment text, it is possible to identify the following key points, divided by category:

### Structure

* Every voxel's dimension's 1x1x1, with no specific material
* The dimensions of the overall cube inside of which the dungeon will be created are 100x101x100, which poses some performance issues, since creating the whole cube and rendering it would mean having to render 100\*101\*100 = 1010000, way too much to hope a "brute force" approach could achieve decent perfomances
* Voxels are can be categorized into 6 types, each with graphically distinct by its color:
    * Grey with a transparency computed randomly in the range [30%-50%] for each voxel (Default voxels)
    * Blue (Agent voxel)
    * White (Wall voxels, corridors and rooms)
    * Red (Ladder voxels, those where the agent has decided to move up)
    * Green (Start voxels, the starting point of every layer)

### Movement

* There is the need to setup a decision system, based on the following rules:
    * 45% of probability to create a room, if this fails, create a corridor, if this also fails, move up by two positions on the y axis
    * 45% of probability to create a new corridor, the agent shall attempt to create one 3 times (at maximum). If it fails 3 consecutive times it shall try to place a room. If this also fails, then the agent shall move up by two positions on the y axis
    * 10% of probability to just move up by 2 layers
* Whenever the agent starts digging a new layer it shall place a room as the first move
* The length of room sides shall be computed randomly in the range [3, 21] for each room
* The lenght of each corridor shall be computed randomly in the range [3, 10]
* The agent must start digging (by placing the first room, and immediately after, the first corridor) in layer 0 in a random position at 15 mt max from the center

<div align="center">
    
## Adopted Solution

</div>

### Overview

Let's start from two key concepts:
* Half the layers are only made of default voxels, while the other half will, very likely, not be dug enough to completely obscure the underling layers
* The "player" shall be able to observe the dungeon being built without having to move the camera inside the cube (which is very chaotic)

Since the transparency of default voxels is neither 0 nor 1, since they are the vast majority of voxels, and since we want to show the dungeon to the player while it's being dug by the agent, the cube is divided into chunks (groups of voxels) at startup, and only the chunk containing the agent is shown. 

This solution allows the player to actually see the construction of the dungeon in real-time, which would not be possible if the program showed all voxels, since default voxels would cover up all the solid ones up until the outer layers (in all 3 dimensions). 

When the agent finishes digging the whole dungeon is shown without any of the Default voxels, so that the player can see their creation.

Other possible solutions explored (but not actualized) include the quad-tree and the oc-tree.

### GridSettings class

    public class GridSettings : MonoBehaviour
    {
        public int chunkWidth = 100;
        public int chunkHeight = 5;
        public int chunkDepth = 100;
    }
    
This class is very simple, but still crucial for the chunk logic to work, it defines the `chunkWidth`, `chunkHeight`, and `chunkDepth`: the dimensions of each side of a chunk in meters (voxels). This fields are public so that the player can change them to experiment with the change in performance and visualization.

### DungeonMiner class

Here is **where the magic happens**. Most of what's needed for the project is implemented in the `DungeonMiner.cs` script, which primarly hosts the DungeonMiner class. 

#### What's a voxel?

Let's start from the definition of a Voxel.

    GridSettings gridSettings;

    private class Voxel
    {
        public GameObject gameObject;
        public VoxelType type;
    }

    public enum VoxelType
    {
        Default,
        Agent,
        Room,
        Corridor,
        Up,
        NewLayer
    }
    
As can be seen here `DungeonMiner` takes as input the `gridSettings` (used to create and manage the chunks) and defines a class `Voxel`. This class contains a parameter `gameObject` (the actual voxel in the scene) and a parameter `type`. This parameter is especially important because it will be used to define the type (and color) of each voxel, it is defined as an enumerator of the following types:

* `Default` for default voxels
* `Agent`, for the voxels corresponding to the position of the agent
* `Room`, for voxels where a room has been placed
* `Corridor`, for voxels where a corridor has been dug
* `Up`, for voxels where the agent has left the layer
* `NewLayer`, for voxels from which the the agent has started the layer

#### Variables 

The following are the variables used by the DungeonMiner class.

    public GameObject voxelPrefab;
    public int gridWidth = 100;
    public int gridHeight = 101;
    public int gridDepth = 100;
    private Voxel[,,] grid;

    private Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

    private Color color = Color.white;
    private float alpha = 1.0f;

    public Vector3Int agentPosition;
    private Vector3Int previousAgentPosition = new Vector3Int(-1, -1, -1); // Inizializza a un valore non valido
    private Vector3Int lastCorridorDirection = new Vector3Int(0, 0, 0);

    private static Vector3Int[] possibleDirections = new Vector3Int[] {
        new Vector3Int(1, 0, 0), // destra
        new Vector3Int(-1, 0, 0), // sinistra
        new Vector3Int(0, 0, 1), // avanti
        new Vector3Int(0, 0, -1) // indietro
    };
    private Vector3Int rngDirection;
    private Vector3Int roomStart;

    private bool lastMoveRoomPlaced = false;
    private bool lastMoveCorridorPlaced = false;
    private bool newLayer = true;

    private bool layer100IsFirstDecisionFailed = false;
    private bool layer100IsSecondDecisionFailed = false;

    private bool isGameOver = false;
    private bool alive = true;

    [SerializeField] Text text;

    private float actionTimer = 0f; // Timer per le azioni dell'agente
    private float actionCooldown = 0.1f; // Tempo di attesa tra le azioni dell'agente, in secondi
    
These variables are related to the internal data structures like the grid (the whole cube, made of voxels) and chunks, but others are also functional to the movement of the agent inside the cube and to the decisions the agent has to make. `Vector3Int` has been used to represent positions and directions in 3D. Since most of this variables are self-explanatory, the following will focus on the most important ones:

* `grid`: The data structure for the cube, a three-dimensional array of `Voxel` that represents the game space where each element can be a block of terrain, a corridor, a room, etc.
* `chunks`: Defined as a dictionary which maps the chunk's position (as `Vector3Int`) to the `Chunk` object itself.
* `previousAgentPosition`, `lastCorridorDirection`: Used to control the agent's decisions and positioning (e.g. for not going back on an already dug path)
* `lastMoveRoomPlaced`, `lastMoveCorridorPlaced`, `newLayer`: These three bools are used to keep track of the last move the agent has made, `newLayer` is initialized as true since the agent starts from an undug layer
* `layer100IsFirstDecisionFailed`, `layer100IsSecondDecisionFailed`: These two bools are used to decide when to stop digging, more on them later
* `isGameOver`, `alive`: used to control if and when to show the whole dungeon, and quit the game

### Methods

#### Awake

The first method we see is `Awake()`:

    void Awake()
    {
        gridSettings = GameObject.FindObjectOfType<GridSettings>();
        InitializeGridAndChunks();
    }
    
This method assigns the grid dimensions to the `gridSettings` variable, then it calls `InitializeGridAndChunks()` to populate the basic data structures.

    void InitializeGridAndChunks()
    {
        // Inizializza l'array grid
        grid = new Voxel[gridWidth, gridHeight, gridDepth];

        int totalChunksX = Mathf.CeilToInt((float)gridWidth / gridSettings.chunkWidth);
        int totalChunksY = Mathf.CeilToInt((float)gridHeight / gridSettings.chunkHeight);
        int totalChunksZ = Mathf.CeilToInt((float)gridDepth / gridSettings.chunkDepth);

        for (int x = 0; x < totalChunksX; x++)
        {
            for (int y = 0; y < totalChunksY; y++)
            {
                for (int z = 0; z < totalChunksZ; z++)
                {
                    Vector3Int chunkPosition = new Vector3Int(x, y, z);

                    // Calcola le dimensioni effettive del chunk per gestire il caso in cui la griglia non sia divisibile uniformemente
                    int chunkWidth = (x == totalChunksX - 1) ? gridWidth - (x * gridSettings.chunkWidth) : gridSettings.chunkWidth;
                    int chunkHeight = (y == totalChunksY - 1) ? gridHeight - (y * gridSettings.chunkHeight) : gridSettings.chunkHeight;
                    int chunkDepth = (z == totalChunksZ - 1) ? gridDepth - (z * gridSettings.chunkDepth) : gridSettings.chunkDepth;

                    Chunk newChunk = new Chunk(chunkPosition, chunkWidth, chunkHeight, chunkDepth);
                    chunks.Add(chunkPosition, newChunk);

                    for (int cx = 0; cx < chunkWidth; cx++) // Usa chunkWidth invece di gridSettings.chunkWidth
                    {
                        for (int cy = 0; cy < chunkHeight; cy++) // Usa chunkHeight invece di gridSettings.chunkHeight
                        {
                            for (int cz = 0; cz < chunkDepth; cz++) // Usa chunkDepth invece di gridSettings.chunkDepth
                            {
                                int globalX = x * gridSettings.chunkWidth + cx;
                                int globalY = y * gridSettings.chunkHeight + cy;
                                int globalZ = z * gridSettings.chunkDepth + cz;

                                if (globalX < gridWidth && globalY < gridHeight && globalZ < gridDepth)
                                {
                                    Vector3Int position = new Vector3Int(globalX, globalY, globalZ);
                                    GameObject voxelGameObject = Instantiate(voxelPrefab, position, Quaternion.identity);
                                    newChunk.voxels[cx, cy, cz] = voxelGameObject;
                                    voxelGameObject.SetActive(false);

                                    grid[globalX, globalY, globalZ] = new Voxel { gameObject = voxelGameObject, type = VoxelType.Default };
                                    UpdateVoxel(position, VoxelType.Default);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

The `InitializeGridAndChunks()` method is tasked with initializing a three-dimensional grid of voxels and dividing this grid into smaller "chunks" for more efficient management. This method plays a crucial role in the initial setup of the game, preparing the environment in which the agent will move. Here's a detailed breakdown of its functionality:

1. **Voxel Grid Initialization**: The method begins by creating the `grid`, using the specified dimensions (`gridWidth`, `gridHeight`, `gridDepth`)
2. **Chunk Number Calculation**: It calculates the total number of chunks into which the grid will be divided in each dimension (X, Y, Z). This is done by dividing the grid dimensions by the chunk dimensions specified in gridSettings (`chunkWidth`, `chunkHeight`, `chunkDepth`), rounding up to the nearest whole number
3. **Chunk Iteration**: The method iterates through each possible chunk position in the `grid`, determined by the previous calculations. For each chunk, it calculates its actual dimensions, which may vary for the last chunks in each dimension if the grid is not perfectly divisible by the chunk dimensions (consider the dimension on the y axis, 101, is a prime number)
4. **Chunk Creation and Storage**: For each chunk position, a new `Chunk` is created with the calculated dimensions. This chunk is then added to the `chunks` dictionary
5. **Populating Chunks with Voxels**: Within each chunk, the method iterates through every voxel position based on the chunk's dimensions. For each voxel, it calculates its global position in the grid and, if this position is valid, creates a new `GameObject` for the voxel, initializes it with a predefined type (`VoxelType.Default`), and adds it to both the current chunk (`newChunk`) and the global `grid`.
6. **Deactivating Voxel GameObjects**: After creation, the voxel GameObjects are deactivated.

#### UpdateVoxel

Before we go on, it's important to establish how we are going to change the type (and color) of each voxel, this role is fulfilled by the `UpdateVoxel()` method.

    void UpdateVoxel(Vector3Int position, VoxelType voxelType)
    {
        Vector3Int chunkIndex = new Vector3Int(position.x / gridSettings.chunkWidth, position.y / gridSettings.chunkHeight, position.z / gridSettings.chunkDepth);
        if (chunks.TryGetValue(chunkIndex, out Chunk chunk))
        {
            int localX = position.x % gridSettings.chunkWidth;
            int localY = position.y % gridSettings.chunkHeight;
            int localZ = position.z % gridSettings.chunkDepth;
            GameObject voxel = chunk.voxels[localX, localY, localZ];
            alpha = 1f; // Resetta la trasparenza a 100%
            if (voxel != null)
            {
                switch (voxelType)
                {
                    case VoxelType.Default:
                        color = new Color(0.5f, 0.5f, 0.5f); // Medium gray
                        alpha = Random.Range(0.3f, 0.5f); // Random transparency between 30% and 50%
                        break;
                    case VoxelType.Agent:
                        color = Color.blue;
                        break;
                    case VoxelType.Room:
                    case VoxelType.Corridor:
                        color = Color.white;
                        break;
                    case VoxelType.Up:
                        color = Color.red;
                        break;
                    case VoxelType.NewLayer:
                        color = Color.green;
                        break;
                }

                Renderer voxelRenderer = voxel.GetComponent<Renderer>();
                voxelRenderer.material.color = new Color(color.r, color.g, color.b, alpha);

                // Utilizza gli indici globali per aggiornare il tipo di voxel nella griglia
                grid[position.x, position.y, position.z].type = voxelType;
            }
        }
    }
    
This method is crucial for reflecting changes in the game world, such as the player's movement, the creation of rooms or corridors, and the transition to new layers. It's composed of 5 stages:

1. **Calculate Chunk Index**: The method starts by calculating the index of the chunk that contains the voxel to be updated. This is achieved by dividing the voxel's global position by the dimensions of a chunk as defined in `gridSettings`. The result is a `Vector3Int` that represents the index of the chunk within the `chunks` dictionary
2. **Retrieve Chunk**: Using `chunkIndex`, the method attempts to retrieve the corresponding Chunk object from the `chunks` dictionary. If the chunk is found, the method proceeds; otherwise, it does nothing further
3. **Calculate Local Voxel Position**: Inside the retrieved `chunk`, the method calculates the voxel's local position. This is done by taking the modulus of the voxel's global position with the chunk's dimensions. The local position is used to access the `voxel` within the chunk's voxel array
4. **Update Voxel Appearance**: The method then updates the voxel's appearance based on the specified `voxelType`. This involves setting the voxel's `color` and `alpha` (transparency value)
5. **Update Voxel Type in Grid**: Finally, the method updates the `type` of the voxel in the global `grid` array to reflect the change. This ensures that the voxel's logical type matches its visual appearance, maintaining consistency in the game's data representation

#### Start

    void Start()
    {
        SpawnAgentRandomly();
        rngDirection = possibleDirections[Random.Range(0, possibleDirections.Length)]; // Inizializza la direzione casuale
        PlaceRoom(agentPosition, 0, 0);
        DigCorridor(agentPosition, lastCorridorDirection, 0);
        UpdateChunkVisibility(agentPosition); // Aggiorna la visibilità in base alla posizione iniziale dell'agente
    }

The `Start` method is comprised of 5 stages:

    void SpawnAgentRandomly()
    {
        // Calcola il centro della griglia
        Vector3Int center = new Vector3Int(gridWidth / 2, 0, gridDepth / 2);
        // Genera una posizione casuale entro 15 metri (unità) dal centro, all'interno del livello 0
        int randomX = Random.Range(center.x - 15, center.x + 15);
        int randomZ = Random.Range(center.z - 15, center.z + 15);
        // Imposta la posizione dell'agente
        agentPosition = new Vector3Int(randomX, 0, randomZ);
        previousAgentPosition = agentPosition;
        UpdateVoxel(agentPosition, VoxelType.Agent); // Aggiorna il voxel alla posizione dell'agente
    }

1. `SpawnAgentRandomly()` places the agent at a random position within a specified range, in this case 15 meters (hard-coded in the function because it's always the same) around the center of the grid on the ground level (`y`=0) of the game environment. Here's a step-by-step explanation of how it works:
    1. **Calculate Grid Center**: The method calculates the `center` of the grid by dividing the grid's width and depth by two. This center point serves as a reference for determining the random spawn area for the agent. The `y` coordinate is set to 0, placing the agent on the ground level
    2. **Generate Random Position**: It generates a random X and Z position (`randomX` and `randomZ`) within a 15-unit radius from the center
    3. **Set Agent Position**: The method sets `agentPosition` to the randomly generated coordinates. It also updates previousAgentPosition to this new position, and it updates the voxel at the agent's new position to the `VoxelType.Agent` type 
2. **Random Direction Computation** chooses a random direction `rngDirection` beetween the four that the agent can use 

        Vector3Int ChooseRandomDirection()
        {
            while (rngDirection == lastCorridorDirection)
            {
                rngDirection = possibleDirections[Random.Range(0, possibleDirections.Length)];
            }
            return rngDirection;
        }
4. **Room Placement**: `PlaceRoom()` places the first room, since it's the first room there's no need for a series of checks. This method will be discussed in more detail later
5. **Corridor Escavation**:  `DigCorridor()` digs the first corridor, similarly to the last phase, since this is the first corridor there's no need for a series of checks. This method will also be discussed in more detail later
6. **Showing the correct chunk**: The `UpdateChunkVisibility` method in the `DungeonMiner.cs` file is designed to manage the visibility of chunks in the game world based on the agent's position. This method is crucial for optimizing performance by ensuring that only the chunk containing the agent is active (visible) at any given time, while all other chunks are deactivated (hidden). Here's how it works:
    1. **Calculate Agent Chunk Index**: The method begins by calculating the index of the chunk in which the agent is currently located. This is done by dividing the agent's position by the dimensions of a chunk (`gridSettings.chunkWidth`, `gridSettings.chunkHeight`, `gridSettings.chunkDepth`). The result is a `Vector3Int` representing the index of the agent's chunk within the chunks dictionary
    2. **Iterate Through All Chunks**: The method then iterates through each entry in the `chunkws` dictionary. Each entry contains a `Vector3Int` key representing the chunk's index and a `Chunk` value representing the chunk itself
    3. **Determine Chunk Visibility**: For each chunk, the method checks if its index matches the agent's chunk index calculated earlier. If the indices match, it means the chunk is the one currently containing the agent, and therefore, it should be active (visible)
    4. **Set Chunk Visibility**: Based on the determination in the previous step, the method sets the active state of all voxel `GameObjects` within the `chunk`. If the `chunk` is the agent's chunk, all voxel `GameObjects` in it are activated (`SetActive(true)`), making the chunk visible. For all other chunks, voxel `GameObjects` are deactivated (`SetActive(false)`), making these chunks invisible



            void UpdateChunkVisibility(Vector3Int playerPosition)
            {
                Vector3Int playerChunkIndex = new Vector3Int(playerPosition.x / gridSettings.chunkWidth, playerPosition.y / gridSettings.chunkHeight, playerPosition.z / gridSettings.chunkDepth);

                foreach (KeyValuePair<Vector3Int, Chunk> entry in chunks)
                {
                    Vector3Int chunkIndex = entry.Key;
                    Chunk chunk = entry.Value;

                    bool shouldBeActive = chunkIndex.Equals(playerChunkIndex);

                    foreach (GameObject voxelGameObject in chunk.voxels)
                    {
                        voxelGameObject.SetActive(shouldBeActive);
                    }
                }
            }

After the `Start()` method, the game grid looks like this (dimensions for the room and corridor change):
<div align="center">
    
<img src="https://hackmd.io/_uploads/BkpT0-iIA.png">

</div>

#### Update

The `Update()` method is used manage what the game should be doing at any given time. Here it is:

    void Update()
    {
        if (!isGameOver)
        {
            // Timer per controllare la frequenza delle azioni dell'agente
            if (actionTimer <= 0f)
            {
                MakeDecision(); // Fai prendere una decisione all'agente su cosa fare
                actionTimer = actionCooldown; // Resetta il timer
                UpdateChunkVisibility(agentPosition); // Aggiorna la visibilità in base alla posizione dell'agente
            }
            else
            {
                actionTimer -= Time.deltaTime; // Decrementa il timer
            }
        } 
        else if (isGameOver && alive)
        {
            Die();
            alive = false;
            text.enabled = true;
        } else
        {
            // Controlla se il tasto Esc è stato premuto
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                QuitGame();
            }
        }
    }
    
To let the player see the construction of the dungeon a timer is used so that a move can only be made by the agent if enough time has elapsed. The star of the show here is the `MakeDecision()` method, which will be described in the next paragraph, but basically manages the decisions the agent takes. After the move has concluded the `UpdateChunkVisibility()` method  is run to make sure the right chunk is shown.

If the game is considered over (`isGameOver` is true), the `Update()` method will execute the `Die()` method, which stops the agent and shows the player the entire dungeon built, and will be discussed later. 

If the Die() method has already been executed and the player presses the `Esc` key, the game quits, using the `QuitGame()` method.

    void QuitGame()
    {
        Application.Quit();

        // Se stai eseguendo il gioco nell'editor di Unity, questa riga assicura che l'editor si fermi.
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

#### MakeDecision

The `MakeDecision` method in the `DungeonMiner.cs` file is a core part of the game logic for a dungeon mining or exploration game. It determines the next action of the agent based on random chance (values are the same as in the assignment for all layers but the last one). 

    void MakeDecision()
    {
        float decision = Random.Range(0f, 1f); // Genera un numero casuale tra 0 e 1

        if (agentPosition.y == 100)
        {
            if (decision < 0.5f || layer100IsSecondDecisionFailed)
            {
                if (!TryPlaceRoom())
                {
                    if (!TryDigCorridor(1))
                    {
                        if (layer100IsSecondDecisionFailed)
                        {
                            isGameOver = true;
                        } else
                        {
                            layer100IsFirstDecisionFailed = true;
                        }
                    } else { layer100IsFirstDecisionFailed = false;}
                } else { layer100IsFirstDecisionFailed = false; }
            }
            else if (decision <= 1f || layer100IsFirstDecisionFailed)
            {
                if (!TryDigCorridor(3))
                {
                    if (!TryPlaceRoom())
                    {
                        if (layer100IsFirstDecisionFailed)
                        {
                            isGameOver = true;
                        } else
                        {
                            layer100IsSecondDecisionFailed = true;
                        }
                    } else { layer100IsSecondDecisionFailed = false; }
                } else { layer100IsSecondDecisionFailed = false; }
            }
        }
        else
        {
            if (decision < 0.45f) // 45% probabilità di piazzare una stanza
            {
                if (!TryPlaceRoom())
                {
                    if (!TryDigCorridor(1)) // Prova a scavare un corridoio, con 1 tentativi
                    {
                        MoveUpAndStartNewLayer(); // Se anche il corridoio fallisce, spostati su e inizia un nuovo strato
                    }
                }
            }
            else if (decision < 0.9f) // 45% probabilità di scavare un nuovo corridoio
            {
                if (!TryDigCorridor(3)) // Prova a scavare un corridoio, con 3 tentativi
                {
                    if (!TryPlaceRoom()) // Se il corridoio fallisce, prova a piazzare una stanza
                    {
                        MoveUpAndStartNewLayer(); // Se anche la stanza fallisce, spostati su e inizia un nuovo strato
                    }
                }
            }
            else // 10% probabilità di cambiare strato
            {
                MoveUpAndStartNewLayer();
            }
        }
    }

The method is designed to implement the logic described in the assignment, the names of these methods are pretty self-explanatory, so I won't go over the logic too much, but here's a breakdown of how it works:

1. **Random Decision Generation**: The method starts by generating a random float between 0 and 1. This random value is used to decide the next action based on predefined probabilities
2. **Special Logic for Layer 101**: If the agent is on layer 101, the method follows a special decision-making process. This process includes additional checks (`layer100IsFirstDecisionFailed` and `layer100IsSecondDecisionFailed`) to handle failures in placing rooms or digging corridors, leading to the state of game over if the agent cannot place rooms or dig corridors anymore
3. **General Logic for Other Layers**: For layers other than 101, the method uses the random decision to choose between placing a room, digging a corridor, or moving up to a new layer based on different probabilities.

#### Placing a Room

To place a room the game uses one main function: `TryPlaceRoom()`, which performs different checks and, in turn, calls `PlaceRoom()`, which handles the actual placement of the room.

##### TryPlaceRoom

    bool TryPlaceRoom()
    {
        // Genera le dimensioni della stanza
        int roomWidth = Random.Range(3, 21);
        int roomDepth = Random.Range(3, 21);
        // Calcola la posizione iniziale della stanza basata sulla posizione corrente dell'agente
        if (lastMoveCorridorPlaced)
        {
            if (lastCorridorDirection.x != 0)
            {
                Vector3Int deltaW = new Vector3Int((lastCorridorDirection.x * (roomWidth / 2) + 1), 0, 0);
                roomStart = agentPosition + deltaW; // Inizia la stanza di fronte all'agente
            } else if (lastCorridorDirection.z != 0)
            {
                Vector3Int deltaD = new Vector3Int(0, 0, (lastCorridorDirection.z * (roomDepth / 2) + 1));
                roomStart = agentPosition + deltaD; // Inizia la stanza di fronte all'agente
            }
            
            // Verifica le intersezioni
            for (int x = roomStart.x - roomWidth / 2; x < roomStart.x + roomWidth / 2; x++)
            {
                for (int z = roomStart.z - roomDepth / 2; z < roomStart.z + roomDepth / 2; z++)
                {
                    Vector3Int pos = new Vector3Int(x, agentPosition.y, z);
                    if (!IsPositionValid(pos) || grid[x, agentPosition.y, z].type == VoxelType.Room || grid[x, agentPosition.y, z].type == VoxelType.Corridor)
                    {
                        // Se trova un'intersezione, restituisce false
                        return false;
                    }
                }
            }
        } else if (lastMoveRoomPlaced)
        {
            lastMoveCorridorPlaced = false;
            return false; // Se l'ultima mossa è stata una stanza, restituisce false
        } else 
        {
            roomStart = agentPosition; // Inizia la stanza di fronte all'agente
        }
        
        // Piazza la stanza
        UpdateVoxel(agentPosition, VoxelType.Corridor); // Aggiorna il voxel alla posizione dell'agente
        PlaceRoom(roomStart, roomWidth, roomDepth); // Usa roomStart come centro della stanza

        return true; // La stanza è stata piazzata con successo
    }
    
The `TryPlaceRoom()` method attempts to place a room in the dungeon based on the current position of the agent and the last movement direction. It follows these steps:

1. **Generate Room Dimensions**: It randomly generates the width and depth of the room to be placed, ensuring that each room can have a unique size within a predefined range
2.	**Calculate Initial Room Position**: Based on the agent's current position and the last movement direction (if the last move placed a corridor), it calculates the starting position for the room. This ensures that rooms are logically connected to corridors, enhancing the dungeon's layout
3.	**Check for Intersections**: Before placing the room, it checks the proposed area for any intersections with existing rooms or corridors. This is done by iterating over the grid positions where the room would be placed and checking if those positions are already occupied. If any part of the room intersects with existing structures, the method returns false, indicating that the room cannot be placed. To determine if the voxel is a valid for creating a room, the method `IsPositionValid()` is employed, here's how it works:
    
    1. **Check Grid Bounds**: The method first checks if the given position (a `Vector3Int` representing `x`, `y`, and `z` coordinates) is within the grid's boundaries. It does this by comparing the position's coordinates against the grid's width (`gridWidth`), height (`gridHeight`), and depth (`gridDepth`). If the position is outside these bounds (e.g., `position.x < 0` or `position.x >= gridWidth`), the method returns `false`, indicating the position is invalid.
    2. **Check Voxel Type at Position**: If the position is within the grid's bounds, the method then checks the type of the voxel at that position. This is done by accessing the `grid` array with the position's coordinates and retrieving the type of the `Voxel` at that location.
    3.	`Determine Validity Based on Voxel Type`: The method considers a position valid if the voxel at that position is of type `VoxelType.Default`, `VoxelType.Agent`, or `VoxelType.NewLayer`. These types indicate that the position is either unoccupied, occupied by the agent itself, or a marker for starting a new layer in the game, respectively. If the voxel's type is one of these, the method returns true, signifying the position is valid for movement or action.

            bool IsPositionValid(Vector3Int position)
            {
                // Controlla se la posizione è dentro i limiti della griglia
                if (position.x < 0 || position.x >= gridWidth || position.y < 0 || position.y >= gridHeight || position.z < 0 || position.z >= gridDepth)
                {
                    return false;
                }
                // Permette all'agente di muoversi liberamente senza essere considerato un ostacolo
                VoxelType voxelTypeAtPosition = grid[position.x, position.y, position.z].type;
                return voxelTypeAtPosition == VoxelType.Default || voxelTypeAtPosition == VoxelType.Agent || voxelTypeAtPosition == VoxelType.NewLayer;
            }
    
4.	**Place the Room**: If there are no intersections, it updates the voxel at the agent's position to a corridor type and then calls the `PlaceRoom()` method to actually create the room. (see the **PlaceRoom()** method for details)
5.	**Update State**: Finally, it updates the game state to reflect that a room has been successfully placed, and it returns true
    
##### PlaceRoom
    
    void PlaceRoom(Vector3Int center, int width, int depth)
    {   
        if (width == 0 || depth == 0)
        {
            // Genera le dimensioni della stanza
            width = Random.Range(3, 21);
            depth = Random.Range(3, 21);
        }
        
        // Calcola gli estremi della stanza per assicurarti che sia all'interno della griglia
        int minX = Mathf.Max(center.x - width / 2, 0);
        int maxX = Mathf.Min(center.x + width / 2, gridWidth - 1);
        int minZ = Mathf.Max(center.z - depth / 2, 0);
        int maxZ = Mathf.Min(center.z + depth / 2, gridDepth - 1);

        for (int x = minX; x <= maxX; x++)
        {
            for (int z =minZ; z <= maxZ; z++)
            {
                Vector3Int pos = new Vector3Int(x, center.y, z);
                UpdateVoxel(pos, VoxelType.Room);
            }
        }
        agentPosition = center; // Imposta la posizione dell'agente al centro della stanza
        UpdateVoxel(agentPosition, VoxelType.Agent); // Aggiorna il voxel alla posizione dell'agente
        lastMoveRoomPlaced = true;
        lastMoveCorridorPlaced = false;
    }

The `PlaceRoom()` method is responsible for actually creating the room in the grid. It takes the `center` position, width, and depth of the room as parameters and follows these steps:

1. **Validate Dimensions**: If the width or depth is 0 (which should only happen at startup), it randomly generates these dimensions
2.	**Calculate Room Bounds**: It calculates the minimum and maximum X and Z (`minX`, `maxX`, `minZ`, `maxZ`) coordinates for the room to ensure that the room fits within the grid boundaries. This step prevents rooms from extending outside the playable area
3.	**Fill the Room**: It iterates over the calculated bounds and updates each voxel within this area to the `VoxelType.Room`, effectively filling the space to create the room
4.	**Update Agent Position**: It sets the agent's position to the `center` of the newly created room, positioning the agent logically within the room
5.	**Update Game State**: It updates the game state to reflect that the last move placed a room and not a corridor

#### Digging a Corridor

To dig a corridor the game uses one main function: `TryDigCorridor()`, which sets up the parameters and controls the number of attempts and, in turn, calls `DigCorridor()`, which handles the actual digging of the corridor.
    
##### TryDigCorridor

    bool TryDigCorridor(int attempts)
    {
        while (attempts > 0)
        {
            attempts--; // Decrementa il numero di tentativi rimasti
            Vector3Int direction = ChooseRandomDirection(); // Sceglie una direzione casuale
            int corridorLength = Random.Range(3, 10); // Sceglie una lunghezza casuale per il corridoio
            Vector3Int currentPos = agentPosition + direction; // Calcola la posizione corrente come la posizione dell'agente più la direzione

            // Se può scavare, usa la funzione DigCorridor per scavare il corridoio
            if (DigCorridor(agentPosition, direction, corridorLength)) {
                return true;
            }
        }

        return false; // Restituisce false se non è stato possibile scavare un corridoio dopo tutti i tentativi

    }
    
The `TryDigCorridor()` method attempts to dig a corridor a specified number of times (attempts). It follows these steps:

1.	**Loop Through Attempts**: It loops through the number of attempts given. For each `attempt`, it decrements the attempts counter.
2.	**Choose Direction and Length**: It chooses a random direction by calling `ChooseRandomDirection()` and a random corridor length between 3 and 10 units.
3.	**Calculate Current Position**: It calculates the current position as the agent's position plus the chosen direction.
4.	**Attempt to Dig a Corridor**: It calls `DigCorridor()` with the agent's position, the chosen direction, and the corridor length. If `DigCorridor()` returns true, indicating the corridor was successfully dug, `TryDigCorridor()` also returns true.
5.	**Failure to Dig**: If all attempts fail (i.e., `DigCorridor()` returns false for each `attempt`), `TryDigCorridor()` returns false, indicating it was not possible to dig a corridor.

##### DigCorridor

    bool DigCorridor(Vector3Int start, Vector3Int direction, int corridorLength)
    {
        if (corridorLength == 0 || lastCorridorDirection == new Vector3Int(0, 0, 0))
        {
            direction = ChooseRandomDirection(); // Sceglie una direzione casuale
            corridorLength = Random.Range(3, 10); // Sceglie una lunghezza casuale per il corridoio
        }

        lastCorridorDirection = direction; // Aggiorna l'ultima direzione del corridoio
        Vector3Int currentPos = start;

        // Determina se l'agente si trova in una stanza e, in tal caso, inizia il corridoio dal bordo della stanza
        if (lastMoveRoomPlaced)
        {
            if (newLayer)
            {
                UpdateVoxel(agentPosition, VoxelType.NewLayer); // Aggiorna il voxel alla posizione dell'agente
                newLayer = false;
            } else { 
                UpdateVoxel(agentPosition, VoxelType.Room); // Aggiorna il voxel alla posizione dell'agente
            }
            currentPos = FindRoomEdge(start, direction);
        }

        // Check if all voxels of the corridor are valid
        bool allVoxelsValid = true;
        List<Vector3Int> corridorPositions = new List<Vector3Int>();
        for (int i = 0; i < corridorLength; i++)
        {
            Vector3Int newPos = currentPos + direction * i;
            if (!IsPositionValid(newPos))
            {
                allVoxelsValid = false;
                break;
            }
            corridorPositions.Add(newPos);
        }

        if (allVoxelsValid)
        {
            // All voxels of the corridor are valid, update the voxels
            foreach (Vector3Int pos in corridorPositions)
            {
                UpdateVoxel(pos, VoxelType.Corridor);
            }

            // Update the agent position at the end of the corridor
            agentPosition = currentPos + direction * (corridorLength - 1);
            UpdateVoxel(agentPosition, VoxelType.Agent); // Aggiorna il voxel 
            lastMoveRoomPlaced = false;
            lastMoveCorridorPlaced = true;
            return true; // Restituisce true se il corridoio è stato scavato con successo
        }
        else
        {
            return false; // Restituisce false se il corridoio non può essere scavato
        }
    }
    
The `DigCorridor()` method actually digs the corridor if possible. It performs several checks and updates the grid accordingly:

1.	**Initial Checks**: If this is the first corridor being dug (indicated by `lastCorridorDirection` being `(0, 0, 0)`, and `corridorLenght` being `0`), it chooses a new random direction and corridor length
2.	**Update Last Corridor Direction**: It updates `lastCorridorDirection` with the chosen direction
3.	**Determine Starting Position**: If the last move placed a room (`lastMoveRoomPlaced` is `true`), it updates the voxel at the agent's position to either `VoxelType.NewLayer` or `VoxelType.Room`, depending on whether a new layer was started. It then calculates the starting position for the corridor at the edge of the room in the chosen direction, to do so, it employes the method `FindRoomEdge()`, which works like this:
    
    1. **Initialize Edge Position**: The method starts with the `start` position, which is a `Vector3Int` representing the `x`, `y`, and `z` coordinates from where the search for the room's edge begins
    2.	**Search for Room Edge**: It enters a loop that continues as long as the current `edgePosition` is within a room, indicated by the voxel at `edgePosition` being of type `VoxelType.Room`, `VoxelType.Agent`, or `VoxelType.NewLayer`. These voxel types are considered part of the room for the purpose of this method
    3.	**Move in Specified Direction**: Within the loop, `edgePosition` is incremented by the `direction` vector. This effectively moves the search position in the specified direction, step by step, checking each voxel to see if it's still within the room
    4.	**Grid Bounds Check**: After each increment, the method checks if the new `edgePosition` has gone out of the grid's bounds. This is important to prevent accessing elements outside the array, which would result in an error. The bounds are defined by `gridWidth`, `gridHeight`, and `gridDepth`
    5.	**Return Start on Bounds Exceeded**: If the search exceeds the grid's bounds, the method immediately returns the `start` position. This acts as a fail-safe, ensuring that the method returns a valid position within the grid even if it doesn't find an actual edge of the room, meaning rooms that would exit the grid are still built, but the part that would go out of the grid is not
    6.	**Return Edge Position**: Once the loop finds a voxel that is not part of the room (i.e., the voxel type is not `VoxelType.Room`, `VoxelType.Agent`, or `VoxelType.NewLayer`), it exits the loop. The method then returns the current `edgePosition`, which is just outside the room's edge in the specified direction

            Vector3Int FindRoomEdge(Vector3Int start, Vector3Int direction)
            {
                Vector3Int edgePosition = start;
                // Cerca il bordo della stanza muovendosi nella direzione opposta
                while (grid[edgePosition.x, edgePosition.y, edgePosition.z].type == VoxelType.Room || grid[edgePosition.x, edgePosition.y, edgePosition.z].type == VoxelType.Agent || grid[edgePosition.x, edgePosition.y, edgePosition.z].type == VoxelType.NewLayer)
                {
                    edgePosition += direction;
                    // Previene il superamento dei limiti della griglia
                    if (edgePosition.x < 0 || edgePosition.x >= gridWidth || edgePosition.y < 0 || edgePosition.y >= gridHeight || edgePosition.z < 0 || edgePosition.z >= gridDepth)
                    {
                        return start; // Se raggiunge il limite, ritorna la posizione di partenza
                    }
                }
                return edgePosition; // Ritorna la posizione appena fuori dalla stanza
            }
    
5.	**Validate Corridor Voxels**: It checks if all voxels along the proposed corridor path are valid (i.e., within the grid bounds and not occupied by obstacles) by calling `IsPositionValid()` for each voxel position. It collects these positions in `corridorPositions`
6.	**Update Voxels if Valid**: If all voxels in the proposed corridor are valid, it updates each voxel along the corridor path to `VoxelType.Corridor` using `UpdateVoxel()`
7.	**Update Agent Position**: It updates the agent's position to the end of the corridor and marks the voxel at this new position with `VoxelType.Agent`
8.	**Update Move Flags**: It sets `lastMoveRoomPlaced` to `false` and `lastMoveCorridorPlaced` to `true`, indicating that the last move successfully placed a corridor
9.	**Return Success or Failure**: It returns `true` if the corridor was successfully dug, or `false` if it was not possible to dig the corridor due to invalid voxel positions

#### MoveUpAndStartNewLayer

The `MoveUpAndStartNewLayer()` method is designed to transition the game's agent to a new layer within the dungeon grid.

    void MoveUpAndStartNewLayer()
    {
        // Sposta l'agente di 2 posizioni su (nell'asse Y) per iniziare un nuovo strato.
        // Assicurati di aggiornare la posizione dell'agente e di iniziare il processo di scavo
        // creando una nuova stanza.

        UpdateVoxel(agentPosition, VoxelType.Up); // Aggiorna il voxel alla posizione dell'agente
        
        agentPosition.y += 2;
        
        lastMoveRoomPlaced = false;
        lastMoveCorridorPlaced = false;
        newLayer = true;
        TryPlaceRoom();
        UpdateVoxel(agentPosition, VoxelType.NewLayer); // Aggiorna il voxel alla nuova posizione dell'agente
    }

Here's a step-by-step explanation of `MoveUpAndStartNewLayer()` functionality:

1.	**Update Current Voxel**: The method starts by updating the voxel at the agent's current position to `VoxelType.Up`. This signifies that the agent is moving up from this position. The `UpdateVoxel` method is called with the agent's current position and the `VoxelType.Up` to make this update
2.	**Move Agent Up**: The agent's position is then moved up by 2 units along the `y` axis. This effectively transitions the agent to a new layer above the current one. The increase by 2 units ensures that there is a clear separation between layers, which can be used as floors, as specified in the assignment
3.	**Reset Movement Flags**: The method resets the `lastMoveRoomPlaced` and `lastMoveCorridorPlaced` flags to `false`. The newLayer flag is set to true to signify that the agent is now on a new layer
4.	**Attempt to Place a Room**: With the agent now positioned on the new layer, the method attempts to place a room at this new location by calling `TryPlaceRoom()`. This is the first action taken on the new layer, aiming to start the layer with a room for the agent to explore
5.	**Update New Position Voxel**: Finally, the method updates the voxel at the agent's new position (after moving up) to `VoxelType.NewLayer`. This update signifies that this position marks the beginning of a new layer within the dungeon. The `UpdateVoxel()` method is called again with the updated agent position and the `VoxelType.NewLayer`

#### Time to Die!

If the agent reaches layer 101 and is not able to place any other room nor corridor, the game detects it's time to die (end all processes), and calls the `Die()` method. 

**But we are not going out without a fight!**

The `Die()` method is designed to handle the scenario when the game's agent finishes digging. This method goes through all the voxels in the game's grid and updates their visibility based on their `type`. The goal is to show the whole dungeon created. Here's a breakdown of its functionality:

1.	**Iterate Through Chunks**: The method starts by iterating through all the chunks in the chunks dictionary. Each chunk contains a collection of voxel `GameObjects` (`voxelGO`) that represent the visual elements of the dungeon.
2.	**Iterate Through `Voxel` `GameObjects`**: For each chunk, it iterates through all the voxel `GameObjects` contained within it.
3.	**Determine Global Position**: For each voxel `GameObject`, it calculates the global position of the voxel within the grid. This is done by converting the voxel `GameObject`'s position to a `Vector3Int` using the `RoundToInt()` extension method on the `Vector3` position

        public static class ExtensionMethods
        {
            public static Vector3Int RoundToInt(this Vector3 v)
            {
                return new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
            }
        }
    
5.	**Check `Voxel` Type and Set Visibility**: The method then checks the `type` of the voxel at the calculated global position within the grid array. If the voxel's `type` is not `VoxelType.Default`, it sets the voxel `GameObject` to active (`voxelGO.SetActive(true)`), making it visible. If the voxel's `type` is `VoxelType.Default`, it sets the voxel `GameObject` to inactive (`voxelGO.SetActive(false)`), making it invisible.

<div align="center">

## Summary
    
</div>

Finally, all the classes, methods and parameters have been analysed, so it is worth having a recap of the whole project, summing up how the requirements have been satisfied. In the problem analysis chapter, some requirements were listed, here is how they have been addressed in this solution:

<div align="center">
<table>
  <tr>
    <th>Requirement</th>
    <th>Solution</th>
  </tr>
  <tr>
    <td><span style="color:red">Every voxel's dimension's 1x1x1, with no specific material</span></td>
    <td><span style="color:green">Prefab built accordingly</span></td>
  </tr>
  <tr>
    <td><span style="color:red">The dimensions of the overall cube inside of which the dungeon will be created are 100x101x100, which poses some performance issues, since creating the whole cube and rendering it would mean having to render 100\*101\*100 = 1010000, way too much to hope a "brute force" approach could achieve decent perfomances</span></td>
    <td><span style="color:green">Chunks and Die() method to manage only few voxels at a time and letting the player see the dungeon</span></td>
  </tr>
  <tr>
    <td><span style="color:red">Voxels are can be categorized into 6 types, each with graphically distinct by its color</span></td>
    <td><span style="color:green">VoxelType enumerator and UpdateVoxel() method to manage the different types and colors</span></td>
  </tr>
  <tr>
    <td><span style="color:red">There is the need to setup a decision system, based on the following rules</span></td>
    <td><span style="color:green">MakeDecision() method and its sub-methods for implementing the required logic</span></td>
  </tr>
  <tr>
    <td><span style="color:red">Whenever the agent starts digging a new layer it shall place a room as the first move</span></td>
    <td><span style="color:green">MoveUpNewLayer() method and TryplaceRoom() method</span></td>
  </tr>
  <tr>
    <td><span style="color:red">The length of room sides shall be computed randomly in the range [3, 21] for each room and the lenght of each corridor shall be computed randomly in the range [3, 10]</span></td>
    <td><span style="color:green">Sizes actually computed randomly</span></td>
  </tr>
  <tr>
    <td><span style="color:red">The agent must start digging (by placing the first room, and immediately after, the first corridor) in layer 0 in a random position at 15 mt max from the center</span></td>
    <td><span style="color:green">Implemented throughSpawnAgentAtRandomLocatio() method</span></td>
  </tr>
</table>
