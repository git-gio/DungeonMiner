using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public static class ExtensionMethods
{
    public static Vector3Int RoundToInt(this Vector3 v)
    {
        return new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
    }
}

public class DungeonMiner : MonoBehaviour
{
    #region params
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
    #endregion params

    void Awake()
    {
        gridSettings = GameObject.FindObjectOfType<GridSettings>();
        InitializeGridAndChunks();
    }
    
    void Start()
    {
        SpawnAgentRandomly();
        rngDirection = possibleDirections[Random.Range(0, possibleDirections.Length)]; // Inizializza la direzione casuale
        PlaceRoom(agentPosition, 0, 0);
        DigCorridor(agentPosition, lastCorridorDirection, 0);
        UpdateChunkVisibility(agentPosition); // Aggiorna la visibilità in base alla posizione iniziale dell'agente

        StartCoroutine(Mine());
    }

    IEnumerator Mine()
    {
        while (true)
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
            }
            else
            {
                // Controlla se il tasto Esc è stato premuto
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    QuitGame();
                    break;
                }
            }

            yield return null;
        }
    }

    void QuitGame()
    {
        Application.Quit();

        // Se stai eseguendo il gioco nell'editor di Unity, questa riga assicura che l'editor si fermi.
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    #region GridStuff
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
    #endregion GridStuff

    #region VoxelStuff
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
    #endregion VoxelStuff

    void Die()
    {
        foreach (var chunk in chunks.Values)
        {
            foreach (GameObject voxelGO in chunk.voxels)
            {
                {
                    // Ottieni la posizione globale del voxel per accedere alla griglia e determinarne il tipo
                    Vector3Int globalPosition = voxelGO.gameObject.transform.position.RoundToInt();

                    if (grid[globalPosition.x, globalPosition.y, globalPosition.z].type != VoxelType.Default)
                    {
                        voxelGO.SetActive(true);
                    }
                    else
                    {
                        voxelGO.SetActive(false);
                    }
                }
            }
        }
    }

    #region AI

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

    Vector3Int ChooseRandomDirection()
    {
        while (rngDirection == lastCorridorDirection)
        {
            rngDirection = possibleDirections[Random.Range(0, possibleDirections.Length)];
        }
        return rngDirection;
    }

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
    #endregion AI
}
