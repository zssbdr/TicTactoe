using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static GameAudioManager;

public class GamePlayManager : MonoBehaviour
{
    [SerializeField] private GameUIManager gameUIManager;
    [SerializeField] private GameAudioManager gameAudioManager;
    [SerializeField] private Transform buttonsContainer;

    private Vector2[,] buttonsPositions = new Vector2[3, 3];    //棋子预设位置

    public const char CheckNone = ' ';
    public const char CheckEmpty = ' ';
    public const char CheckCircle = 'o';
    public const char CheckCross = 'x';

    private char[,] checksStatus = new char[3, 3];
    private bool[,] winChecks = new bool[3, 3];

    private int[] playerWinTimes = new int[2];
    private int drawTimes = 0;

    public enum GAME_STAGE
    {
        NotStarted = 0,
        Playing = 1,
        GameOverWin = 2,
        GameOverDraw = 3,
    }

    public GAME_STAGE gameStage = GAME_STAGE.NotStarted;

    public enum GAME_PLAYER
    {
        Player1 = 0,
        Player2 = 1,
    }

    public GAME_PLAYER gamePlayer = GAME_PLAYER.Player1;

    public enum PLAYER_TYPE
    {
        Human = 0,
        Computer = 1,
    }

    public PLAYER_TYPE[] playerTypes = new PLAYER_TYPE[2];

    public GAME_PLAYER gameWinner;

    public void SetPlayerTypes(GAME_PLAYER player, PLAYER_TYPE playerType)
    {
        playerTypes[(int)player] = playerType;
        gameUIManager.SetPlayerTypesUI(player, playerType);
    }
    public void ReversePlayerTypes(GAME_PLAYER player)
    {
        PLAYER_TYPE playerType = playerTypes[(int)player];
        PLAYER_TYPE targetPlayerType = (PLAYER_TYPE)(1 - (int)playerType);
        SetPlayerTypes(player, targetPlayerType);
        AICanPlaceMark = false;
        StartCoroutine(AICanPlace());
    }

    public void ReStartGame()
    {
        gameStage = GAME_STAGE.NotStarted;
        gamePlayer = GAME_PLAYER.Player1;
        for (int i = 0; i <= 2; i++)
        {
            for (int j = 0; j <= 2; j++)
            {
                checksStatus[i, j] = CheckEmpty;
                winChecks[i, j] = false;
            }
        }
        gameUIManager.ClearCheckboard();
        SetGameStage(gameStage);
        StartCoroutine(AICanPlace());
    }

    // Start is called before the first frame update
    private void Start()
    {
        UnityEngine.Debug.Log("GamePlay Active");
        InitManagerr();
    }

    private void InitManagerr()
    {
        gameAudioManager.InitManager();
        gameUIManager.InitManager();

        var btns = buttonsContainer.GetComponentsInChildren<Button>();
        for (int i = 0; i < btns.Length; i++)
        {
            int x = i / 3;
            int y = i % 3;
            btns[i].onClick.AddListener(() => OnPlayerPlay(x, y));

            RectTransform rect = btns[i].GetComponent<RectTransform>();
            buttonsPositions[x, y] = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y);
        }
        SetPlayerTypes(GAME_PLAYER.Player1, PLAYER_TYPE.Human);
        SetPlayerTypes(GAME_PLAYER.Player2, PLAYER_TYPE.Human);
        ReStartGame();
        playerWinTimes[0] = 0;
        playerWinTimes[1] = 0;
        drawTimes = 0;
    }

    // 玩家点击棋盘
    private void OnPlayerPlay(int x, int y)
    {
        // AI 工作的时候不能放置棋子
        if (playerTypes[(int)gamePlayer] != PLAYER_TYPE.Human)
        {
            return;
        }
        OnPlaceCheck(x, y);
    }

    private bool CanPlace(int x, int y)
    {
        return checksStatus[x, y] == CheckEmpty;
    }


    int[,,] directions = {
        { {0, 0}, {0, 1}, {0, 2} },
        { {1, 0}, {1, 1}, {1, 2} },
        { {2, 0}, {2, 1}, {2, 2} },

        { {0, 0}, {1, 0}, {2, 0} },
        { {0, 1}, {1, 1}, {2, 1} },
        { {0, 2}, {1, 2}, {2, 2} },

        { {0, 0}, {1, 1}, {2, 2} },
        { {0, 2}, {1, 1}, {2, 0} },
    };

    public bool IsGameOver(int x, int y, char c)
    {
        if (!CanPlace(x, y))
        {
            return false;
        }
        char before = checksStatus[x, y];
        checksStatus[x, y] = c;
        bool ret = IsGameOver(c, true);
        checksStatus[x, y] = before;
        return ret;
    }
    private bool IsGameOver(char checkChar, bool justCheck = false)
    {
        bool isGameOver = false;
        for (int i = 0; i < 8; i++)
        {
            int count = 0;
            for (int j = 0; j <= 2; j++)
            {
                if (checksStatus[directions[i, j, 0], directions[i, j, 1]] == checkChar)
                {
                    count++;
                }
            }
            if (count == 3)
            {
                isGameOver = true;
                if (justCheck != true)
                    for (int j = 0; j <= 2; j++)
                    {
                        winChecks[directions[i, j, 0], directions[i, j, 1]] = true;
                    }
            }
        }
        return isGameOver;
    }
    private void ChangePlayer()
    {
        gamePlayer = (GAME_PLAYER)(1 - (int)gamePlayer);
        if (playerTypes[(int)gamePlayer] == PLAYER_TYPE.Computer)
        {
            AICanPlaceMark = false;
            StartCoroutine(AICanPlace());
        }
    }
    bool AICanPlaceMark;
    WaitForSeconds waitForAIPlace = new WaitForSeconds(0.5f);
    IEnumerator AICanPlace()
    {
        yield return waitForAIPlace;
        AICanPlaceMark = true;
    }
    private void AIMove()
    {
        AICanPlaceMark = false;
        List<Tuple<int, int>> necessaryTargets = new List<Tuple<int, int>>();
        List<Tuple<int, int>> leadToWinTargets = new List<Tuple<int, int>>();
        List<Tuple<int, int>> casualTargets = new List<Tuple<int, int>>();
        for (int i = 0; i <= 2; i++)
        {
            for (int j = 0; j <= 2; j++)
            {
                if (checksStatus[i, j] == CheckEmpty)
                {
                    // 如果自己能赢，就去赢
                    if (IsGameOver(i, j, gamePlayer == GAME_PLAYER.Player1 ? CheckCircle : CheckCross))
                    {
                        leadToWinTargets.Add(Tuple.Create(i, j));
                    }
                    // 对手放这个棋子就赢了的话，那就堵
                    else if (IsGameOver(i, j, gamePlayer == GAME_PLAYER.Player1 ? CheckCross : CheckCircle))
                    {
                        necessaryTargets.Add(Tuple.Create(i, j));
                    }
                    // 否则就随便走走
                    else
                    {
                        casualTargets.Add(Tuple.Create(i, j));
                    }
                }
            }
        }
        if (leadToWinTargets.Count != 0)
        {
            int index = UnityEngine.Random.Range(0, leadToWinTargets.Count);
            OnPlaceCheck(leadToWinTargets[index].Item1, leadToWinTargets[index].Item2);
        }
        else if (necessaryTargets.Count != 0)
        {
            int index = UnityEngine.Random.Range(0, necessaryTargets.Count);
            OnPlaceCheck(necessaryTargets[index].Item1, necessaryTargets[index].Item2);
        }
        else if (casualTargets.Count != 0)
        {
            int index = UnityEngine.Random.Range(0, casualTargets.Count);
            OnPlaceCheck(casualTargets[index].Item1, casualTargets[index].Item2);
        }
    }

    private void WinGame()
    {
        gameUIManager.ChangeCheckColor(winChecks, Color.red);

        playerWinTimes[(int)gamePlayer]++;
        gameUIManager.UpdateScore(gamePlayer, playerWinTimes[(int)gamePlayer]);
        gameWinner = gamePlayer;
        SetGameStage(GAME_STAGE.GameOverWin);
    }
    // 放置棋子

    private bool IsGameDraw()
    {
        for (int i = 0; i <= 2; i++)
        {
            for (int j = 0; j <= 2; j++)
            {
                if (checksStatus[i, j] == CheckEmpty)
                {
                    return false;
                }
            }
        }
        return true;
    }
    private void DrawGame()
    {
        gameUIManager.ChangeCheckColor(winChecks, Color.red);
        drawTimes++;
        gameUIManager.UpdateDrawScore(drawTimes);
        SetGameStage(GAME_STAGE.GameOverDraw);
    }
    private void OnPlaceCheck(int x, int y)
    {
        if (!CanPlace(x, y))
        {
            return;
        }
        if (gameStage == GAME_STAGE.GameOverWin || gameStage == GAME_STAGE.GameOverDraw)
        {
            return;
        }
        if (gamePlayer == GAME_PLAYER.Player1)
        {
            checksStatus[x, y] = CheckCircle;
        }
        else
        {
            checksStatus[x, y] = CheckCross;
        }
        gameUIManager.OnPlaceCheck(gamePlayer, x, y, buttonsPositions[x, y]);
        if (gameStage == GAME_STAGE.NotStarted)
        {
            SetGameStage(GAME_STAGE.Playing);
        }
        if (IsGameOver(checksStatus[x, y]))
        {
            WinGame();
            return;
        }
        if (IsGameDraw())
        {
            DrawGame();
            return;
        }
        ChangePlayer();
    }
    public void SetGameStage(GAME_STAGE gameStage_)
    {
        gameStage = gameStage_;
        gameUIManager.SetGameStage(gameStage);
    }

    private bool checkAIMove()
    {
        if (gameStage == GAME_STAGE.GameOverWin || gameStage == GAME_STAGE.GameOverDraw)
        {
            return false;
        }
        if (playerTypes[(int)gamePlayer] == PLAYER_TYPE.Computer)
        {
            if (AICanPlaceMark)
            {
                return true;
            }
        }
        return false;
    }

    // Update is called once per frame
    private void Update()
    {
        if (checkAIMove())
        {
            AIMove();
        }
    }
}