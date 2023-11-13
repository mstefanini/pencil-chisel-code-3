using System;
using System.Collections.Generic;
using System.Linq;
using com.trashpandaboy.core;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameManager : Manager<GameManager>
{   
    [SerializeField] private int playersAmount = 0;
    [SerializeField] private GameObject characterPrefab;
    [SerializeField] private Color[] characterColors;
    [SerializeField] private Vector3[] spawnPositions;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float cameraMovement;
    [SerializeField] private float triggerAtMagnitude;

    private Quaternion _cameraInitRotation;
    private Vector3 _cameraInitPosition;
    private float _cameraYZRatio;
    private bool _cameraMoved = false;
    private bool isGameInProgress = false;
    private float gameTimer = 0;
    private List<Character> playerCharacters = new List<Character>();
    private MoveUp oil;
    private SpawnObjectOnSpace spawner;
    private AudioSource[] _soundAudioSources;
    private AudioSource[] _musicAudioSources;

    private void Start()
    {
        var cameraTransform = mainCamera.transform;
        _cameraInitRotation = cameraTransform.rotation;
        _cameraInitPosition = cameraTransform.position;
        _cameraYZRatio = Math.Abs(_cameraInitPosition.y / _cameraInitPosition.z);

        spawner = FindObjectOfType<SpawnObjectOnSpace>();
        _soundAudioSources = transform.Find("Sound").GetComponents<AudioSource>();
        _musicAudioSources = transform.Find("Music").GetComponents<AudioSource>();
    }

    void OnEnable()
    {
        // SceneManager.sceneLoaded += OnSceneLoaded;
        UIManager.OnClicked += OnButtonClicked;
        UIManager.OnPlayerSelected += PlayerSelected;
    }

    /*
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("OnSceneLoaded: " + scene.name);
        Debug.Log(mode);

        if(scene.name == "Level")
        {
            StartMatch();
        }
    }
    */

    private void Update()
    {
        if (isGameInProgress)
        {
            gameTimer += Time.deltaTime;
            UIManager.Instance.OnGameUpdate(gameTimer);

            if (playerCharacters.Count == 1)
            {
                mainCamera.transform.LookAt(playerCharacters[0].transform);
            }
            else
            {
                var origin = new Vector3();
                var movement = new Vector3(0, cameraMovement * _cameraYZRatio, -cameraMovement);
                var maxMagnitude =
                    playerCharacters
                        .Select(player => player.transform.position - origin)
                        .Select(distance => distance.magnitude)
                        .Prepend(0f)
                        .Max();
                if (maxMagnitude > triggerAtMagnitude && !_cameraMoved)
                {
                    _cameraMoved = true;
                    mainCamera.transform.position += movement;
                }
                else if (maxMagnitude <= triggerAtMagnitude)
                {
                    mainCamera.transform.position = _cameraInitPosition;
                    _cameraMoved = false;
                }
            }
        }
        else
        {
            mainCamera.transform.rotation = _cameraInitRotation;
        }
    }

    void PlayerSelected(int amountOfPlayers)
    {
        playersAmount = amountOfPlayers;
    }

    void OnButtonClicked(string buttonName)
    {
        switch (buttonName)
        {
            case "Play":
                // SceneManager.LoadScene("Level");
                StartMatch();
                break;

            case "Continue":
                ResetMatch();
                break;
        }
    }

    void StartMatch()
    {
        if (playersAmount > 4)
        {
            playersAmount = 4;
        }
        else if (playersAmount < 1)
        {
            playersAmount = 1;
        }

        for (int i = 0; i < playersAmount; i++)
        {
            Character newPlayer = Instantiate(characterPrefab, spawnPositions[i], Quaternion.identity)
                .GetComponent<Character>();
            newPlayer.Initialize(i, characterColors[i]);
            playerCharacters.Add(newPlayer);
        }

        oil = FindObjectOfType<MoveUp>();
        oil.StartMovement();

        spawner.StartSpawning();
        
        foreach (AudioSource music in _musicAudioSources)
            music.Play();

        gameTimer = 0;
        isGameInProgress = true;
        UIManager.Instance.StartMatch();
    }

    void EndMatch()
    {
        oil.StopMovement();
        spawner.StopSpawning();

        int winner = -1;
        if (playerCharacters.Count > 0)
        {
            winner = playerCharacters[0].PlayerID;
        }

        UIManager.Instance.EndMatch(winner);

        isGameInProgress = false;
    }

    void ResetMatch()
    {
        playersAmount = 0;
        oil.ResetPosition();
        spawner.ClearSpawning();

        for (int i = playerCharacters.Count - 1; i >= 0; i--)
        {
            GameObject playerGO = playerCharacters[i].gameObject;
            playerCharacters.RemoveAt(i);
            Destroy(playerGO);
        }

        foreach (AudioSource music in _musicAudioSources)
            music.Stop();
        
        UIManager.Instance.Reset();
    }

    public void OnCharacterDied(int playerID)
    {
        _soundAudioSources[Random.Range(0, _soundAudioSources.Length)].Play();
        
        if (isGameInProgress)
        {
            for (int i = playerCharacters.Count - 1; i >= 0; i--)
            {
                if (playerCharacters[i].PlayerID != playerID)
                    continue;

                GameObject playerGO = playerCharacters[i].gameObject;
                playerCharacters.RemoveAt(i);
                Destroy(playerGO);
            }

            if (playerCharacters.Count <= 1)
            {
                EndMatch();
            }
        }
    }
}