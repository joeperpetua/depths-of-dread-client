using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using bottlenoselabs.C2CS.Runtime;
using Dojo;
using Dojo.Starknet;
using Dojo.Torii;
using TMPro;
using UnityEngine;
using static EncodingService;

public class DojoWorker : MonoBehaviour
{
    [SerializeField] WorldManager worldManager;
    [SerializeField] WorldManagerData dojoConfig;
    [SerializeField] DojoWorkerData dojoWorkerData;
    public Actions actions;
    public JsonRpcClient provider;
    private Account account;
    private GameObject playerEntity;
    private GameObject gameEntity;

    void Start()
    {
        worldManager.synchronizationMaster.OnEntitySpawned.AddListener(HandleSpawn);
        worldManager.synchronizationMaster.OnModelUpdated.AddListener(HandleUpdate);
    }

    public async void SimulateControllerConnection(string username)
    {
        provider = new JsonRpcClient(dojoConfig.rpcUrl);
        account = new Account(
            provider,
            new SigningKey(dojoWorkerData.masterPrivateKey),
            new FieldElement(dojoWorkerData.masterAddress)
        );

        var txnHash = await CreatePlayer(username);
        await provider.WaitForTransaction(txnHash);
    }

    public void SimulateControllerDisconnection()
    {
        provider = new JsonRpcClient(dojoConfig.rpcUrl);
        account = null;
        playerEntity = null;

        UIManager.instance.HandleDisconnection();
    }

    public async Task<FieldElement> CreatePlayer(string username)
    {
        BigInteger encodedUsername = ASCIIToBigInt(username);
        return await actions.create_player(account, new FieldElement(encodedUsername));
    }

    public async void CreateGame()
    {
        if (account == null)
        {
            SimulateControllerConnection("test_username");
            return;
        }

        var txnHash = await actions.create_game(account);
        await provider.WaitForTransaction(txnHash);

        if (gameEntity != null)
        {
            ScreenManager.instance.SetActiveScreen("GameOverlay");
        }
        else
        {
            Debug.LogWarning("Game entity is null");
        }
    }

    public async void EndGame()
    {
        await actions.end_game(account);
        UIManager.instance.HandleExitGame();
    }

    public async void Move(int direction)
    {
        Direction dir = (Direction)Direction.FromIndex(typeof(Direction), direction);
        await actions.move(account, dir);
    }

    void HandleSpawn(GameObject spownedEntity)
    {
        if (account == null) { return; }
        SyncLocalEntities();
    }

    void HandleUpdate(ModelInstance updatedModel)
    {
        // Player Entity Handlers
        SyncLocalEntities();
        if (playerEntity != null)
        {
            switch (updatedModel.GetType().Name)
            {
                case "depths_of_dread_PlayerData":
                    OnPlayerDataUpdate();
                    break;
                case "depths_of_dread_PlayerState":
                    OnPlayerStateUpdate();
                    break;
                case "depths_of_dread_PlayerPowerUps":
                    OnPlayerPowerUpsUpdate();
                    break;
                default:
                    break;
            }
        }

        // When running SyncLocalEntities() for the first time, PlayerState is not yet defined, making game entity null.
        // We need to run entity sync again to initialize game entity with latest PlayerState.game_id value.

        // Game Entity Handlers
        SyncLocalEntities();
        if (gameEntity != null)
        {
            switch (updatedModel.GetType().Name)
            {
                case "depths_of_dread_GameData":
                    OnGameDataUpdate();
                    break;
                case "depths_of_dread_GameFloor":
                    OnGameFloorUpdate();
                    break;
                case "depths_of_dread_GameCoins":
                    OnGameCoinsUpdate();
                    break;
                case "depths_of_dread_GameObstacles":
                    OnGameObstaclesUpdate();
                    break;
                default:
                    break;
            }
        }
    }

    void SyncLocalEntities()
    {
        var playerKey = account == null ? null : GetPoseidonHash(account.Address);
        var pEntity = GameObject.Find(playerKey);

        // and if the entity matches the current player hashed key
        if (pEntity != null && pEntity != playerEntity)
        {
            playerEntity = pEntity;
            OnPlayerDataUpdate();
            OnPlayerStateUpdate();
            OnPlayerPowerUpsUpdate();

            Debug.Log($"Synced playerEntity {playerEntity}");
        }

        var playerState = playerEntity == null ? null : playerEntity.GetComponent<depths_of_dread_PlayerState>();
        var gameKey = playerState == null ? null : GetPoseidonHash(new FieldElement(playerState.game_id));
        var gEntity = GameObject.Find(gameKey);

        if (gEntity != null && gEntity != gameEntity)
        {
            gameEntity = gEntity;
            OnGameDataUpdate();
            OnGameFloorUpdate();
            OnGameCoinsUpdate();

            Debug.Log($"Synced gameEntity {gameEntity}");
        }
    }

    void OnPlayerDataUpdate()
    {
        if (playerEntity == null) { Debug.Log("Player entity is null"); return; }

        var playerData = playerEntity.GetComponent<depths_of_dread_PlayerData>();
        if (playerData == null) { return; }

        string usernameHex = playerData.username.Hex();
        UIManager.instance.HandleConnection(HexToASCII(usernameHex));

        Debug.Log($"Updated player data");
    }

    void OnPlayerStateUpdate()
    {
        var playerState = playerEntity.GetComponent<depths_of_dread_PlayerState>();
        var playerData = playerEntity.GetComponent<depths_of_dread_PlayerData>();
        if (playerState == null || playerData == null) { return; }

        // Redirect to Game screen if player has an ongoing game
        if (playerState.game_id != 0 && ScreenManager.instance.currentScreen != "GameOverlay")
        {
            ScreenManager.instance.SetActiveScreen("GameOverlay");
        }

        if (playerState.game_id == 0 && ScreenManager.instance.currentScreen == "GameOverlay") {
            // Gameover is triggered
            gameEntity = null;
            UIManager.instance.HandleGameover();
            return;
        }

        // Update UI only if we are in Game screen
        if (ScreenManager.instance.currentScreen != "GameOverlay")
        {
            return;
        }

        UIManager.instance.HandleStateUpdate(playerData, playerState);
        Debug.Log($"Updated player state");
    }

    void OnPlayerPowerUpsUpdate()
    {
        Debug.Log($"Updated player powerups");
    }

    void OnGameDataUpdate()
    {

        Debug.Log($"Updated game data");
    }

    void OnGameFloorUpdate()
    {
        var gameFloor = gameEntity.GetComponent<depths_of_dread_GameFloor>();
        var playerState = playerEntity.GetComponent<depths_of_dread_PlayerState>();
        
        if (gameFloor == null) { Debug.Log("Game floor is null"); return; }
        if (gameFloor.game_id != playerState.game_id) { throw new Exception("Game floor ID does not match with playerState ID"); }

        Debug.Log($"Going to render floor for ID {gameFloor.game_id}, size {gameFloor.size.x + 1}x{gameFloor.size.y + 1}");
        UIManager.instance.RenderGameGrid(gameFloor);
        Debug.Log($"Updated game floor");
    }

    void OnGameCoinsUpdate()
    {
        Debug.Log($"Updated game coins");
    }

    void OnGameObstaclesUpdate()
    {
        Debug.Log($"Updated game obstacles");
    }
}
