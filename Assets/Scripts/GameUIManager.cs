using Mono.Cecil.Cil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static GamePlayManager;
using static GameAudioManager;
using System;

public class ImageFillManager
{
    private Image image;
    private float fillLastTime;
    private float fillTargetTime;
    private float fillStartAmount;
    private float fillEndAmount;
    public ImageFillManager(Image image_, float fillTargetTime_, float fillStartAmount_, float fillEndAmount_ = (float)0)
    {
        image = image_;
        fillTargetTime = fillTargetTime_;
        fillStartAmount = fillStartAmount_;
        fillEndAmount = Math.Max(fillEndAmount_, fillStartAmount_);
        fillLastTime = (float)0;
        Update(0);
    }
    public bool Finished()
    {
        return fillLastTime >= fillTargetTime;
    }
    public void Update(float deltaTime)
    {
        fillLastTime += deltaTime;
        if (image)
        {
            float fillCurrentAmout = fillStartAmount + (fillEndAmount - fillStartAmount) * Math.Min(fillLastTime, fillTargetTime) / fillTargetTime;
            image.fillAmount = fillCurrentAmout;
        }
    }
}

public class GameUIManager : MonoBehaviour
{
    [SerializeField] private GameObject circleCheckPrefab;     //电脑棋子
    [SerializeField] private GameObject crossCheckPrefab;    //玩家棋子
    [SerializeField] private Transform checkersContainer;

    [SerializeField] private GameObject player1UseAICheckBox;
    [SerializeField] private GameObject player2UseAICheckBox;

    [SerializeField] private GameObject player1Wins;
    [SerializeField] private GameObject player2Wins;
    [SerializeField] private GameObject playerDraws;

    [SerializeField] private GamePlayManager gamePlayManager;
    [SerializeField] private GameAudioManager gameAudioManager;

    [SerializeField] private GameObject gameStartText;
    //[SerializeField] private Image player2UseAICheckMark;
    // Start is called before the first frame update
    private Image player1UseAICheckMark;
    private Image player2UseAICheckMark;


    private List<ImageFillManager> imageFillManagers = new List<ImageFillManager>();
    GameObject[,] checkObjects = new GameObject[3, 3];

    private void CreateImageFill(Image image, float fillTargetTime, float fillStartAmount, float fillEndAmount = (float)1)
    {
        imageFillManagers.Add(new ImageFillManager(image, fillTargetTime, fillStartAmount, fillEndAmount));
    }
    public void InitManager()
    {
        player1UseAICheckMark = player1UseAICheckBox.transform.GetChild(0).GetComponent<Image>();
        player2UseAICheckMark = player2UseAICheckBox.transform.GetChild(0).GetComponent<Image>();

        Button player1UseAIButton = player1UseAICheckBox.GetComponent<Button>();
        Button player2UseAIButton = player2UseAICheckBox.GetComponent<Button>();

        if (player1UseAIButton)
        {
            player1UseAIButton.onClick.AddListener(() => gamePlayManager.ReversePlayerTypes(GAME_PLAYER.Player1));
        }
        if (player2UseAIButton)
        {
            player2UseAIButton.onClick.AddListener(() => gamePlayManager.ReversePlayerTypes(GAME_PLAYER.Player2));
        }

        Button gameStartButton = gameStartText.GetComponent<Button>();
        gameStartButton.onClick.AddListener(() => gamePlayManager.ReStartGame());

        ClearCheckboard();
        imageFillManagers.Clear();
    }

    public void OnPlaceCheck(GAME_PLAYER player, int x, int y, Vector2 pos)
    {
        GameObject showObj = Instantiate(player == GAME_PLAYER.Player1 ? circleCheckPrefab : crossCheckPrefab, checkersContainer);
        showObj.GetComponent<RectTransform>().anchoredPosition = pos;
        if (player == GAME_PLAYER.Player1)
        {
            gameAudioManager.PlaySE(AUDIO_TYPE.Circle);
            Image image = showObj.GetComponent<Image>();
            CreateImageFill(image, (float)0.3, 0, 1);
        }
        else
        {
            gameAudioManager.PlaySE(AUDIO_TYPE.Cross1);
            GameObject gameObject1 = showObj.transform.GetChild(0).gameObject;
            GameObject gameObject2 = showObj.transform.GetChild(1).gameObject;

            gameObject1.SetActive(true);
            Image image = gameObject1.GetComponent<Image>();
            CreateImageFill(image, (float)0.15, 0, 1);

            gameObject2.SetActive(false);
            StartCoroutine(ShowSubCross(gameObject2));
        }
        checkObjects[x, y] = showObj;
    }
    WaitForSeconds waitForSubCross = new WaitForSeconds(0.2f);
    IEnumerator ShowSubCross(GameObject cross)
    {
        yield return waitForSubCross;
        gameAudioManager.PlaySE(AUDIO_TYPE.Cross2);
        cross.SetActive(true);
        Image image = cross.GetComponent<Image>();
        CreateImageFill(image, (float)0.15, 0, 1);
    }
    public void SetPlayerTypesUI(GAME_PLAYER player, PLAYER_TYPE playerType)
    {
        if (player == GAME_PLAYER.Player1)
        {
            if (player1UseAICheckMark)
            {
                player1UseAICheckMark.enabled = playerType == PLAYER_TYPE.Computer;
            }
        }
        else if (player == GAME_PLAYER.Player2)
        {
            if (player2UseAICheckMark)
            {
                player2UseAICheckMark.enabled = playerType == PLAYER_TYPE.Computer;
            }
        }
    }

    public void UpdateScore(GAME_PLAYER gamePlayer, int score)
    {
        if (gamePlayer == GAME_PLAYER.Player1)
        {
            Text text = player1Wins.GetComponent<Text>();
            if (text != null)
            {
                text.text = $"胜利次数 {score}";
            }
        }
        else if (gamePlayer == GAME_PLAYER.Player2)
        {
            Text text = player2Wins.GetComponent<Text>();
            if (text != null)
            {
                text.text = $"胜利次数 {score}";
            }
        }
    }
    public void UpdateDrawScore(int score)
    {
        Text text = playerDraws.GetComponent<Text>();
        if (text)
        {
            text.text = $"平局次数 {score}";
        }
    }

    public void ChangeCheckColor(bool[,] winChecks, Color color)
    {
        for (int i = 0; i <= 2; i++)
        {
            for (int j = 0; j <= 2; j++)
            {
                if (winChecks[i, j])
                {
                    GameObject obj = checkObjects[i, j];
                    if (obj != null)
                    {
                        Image image = obj.GetComponent<Image>();
                        if (image)
                        {
                            image.color = color;
                        }
                        for (int k = 0; k < obj.transform.childCount; k++)
                        {
                            GameObject child = obj.transform.GetChild(k).gameObject;
                            Image childImage = child.GetComponent<Image>();
                            if (childImage)
                            {
                                childImage.color = color;
                            }
                        }
                    }
                }

            }
        }
    }
    public void ClearCheckboard()
    {
        for (int i = 0; i <= 2; i++)
        {
            for (int j = 0; j <= 2; j++)
            {
                GameObject obj = checkObjects[i, j];
                if (obj != null)
                {
                    Destroy(obj.gameObject);
                }
                checkObjects[i, j] = null;
            }
        }
        // Checkboard 上的填充事件也清空吧
        imageFillManagers.Clear();
    }
    public void SetGameStage(GAME_STAGE gameStage)
    {
        Text text = gameStartText.GetComponent<Text>();
        Button button = gameStartText.GetComponent<Button>();
        if (gameStage == GAME_STAGE.NotStarted)
        {
            if (button)
            {
                button.interactable = false;
            }
            if (text)
            {
                text.color = Color.gray;
                text.text = "玩家壹先走";
            }
        }
        else if (gameStage == GAME_STAGE.Playing)
        {
            if (button)
            {
                button.interactable = true;
            }
            if (text)
            {
                text.color = Color.white;
                text.text = "重新游戏";
            }
        }
        else if (gameStage == GAME_STAGE.GameOverWin)
        {
            if (button)
            {
                button.interactable = true;
            }
            if (text)
            {
                text.color = Color.white;
                string playerText = gamePlayManager.gameWinner == GAME_PLAYER.Player1 ? "壹" : "贰";
                text.text = $"再来一局";
            }
        }
        else if (gameStage == GAME_STAGE.GameOverDraw)
        {
            if (button)
            {
                button.interactable = true;
            }
            if (text)
            {
                text.color = Color.white;
                text.text = "打平了! 再来一局";
            }
        }
    }

    void Start()
    {
        UnityEngine.Debug.Log("GameUI Active");
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = imageFillManagers.Count - 1; i >= 0; i--)
        {
            ImageFillManager manager = imageFillManagers[i];
            manager.Update(Time.deltaTime);
            if (manager.Finished())
            {
                imageFillManagers.Remove(manager);
            }
        }
    }
}
