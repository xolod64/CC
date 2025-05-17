using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using TMPro;
using System;
using System.Collections;

public enum GameState
{
    EnterName,
    WaitingForOthers,
    RoundInProgress,
    ShowResults,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public GameState currentState = GameState.EnterName;
    public ServerConnector serverConnector;
    public int timeLeft;
    public TMP_InputField[] droneInputs;
    public DroneInputManager validator;

    [Header("UI References")]
    public TMP_InputField nameInputField;
    public Button submitButton;
    public TextMeshProUGUI messageText;

    [Header("GameObjects References")]
    public Light2D Light;

    private string playerName;

    private void Start()
    {
        submitButton.onClick.AddListener(OnSubmitName);
        SetState(GameState.EnterName);

        foreach (var input in droneInputs)
        {
            input.onValueChanged.AddListener(_ => 
            {
                if (currentState == GameState.RoundInProgress)
                    validator.ValidateInputs();
            });
        }
    }

    public void SetState(GameState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case GameState.EnterName:
                messageText.text = "Enter Your Nickname:";
                nameInputField.gameObject.SetActive(true);
                submitButton.gameObject.SetActive(true);
                Light.enabled = false;
                break;

            case GameState.WaitingForOthers:
                messageText.text = $"Waiting for other players";
                Light.enabled = true;
                nameInputField.gameObject.SetActive(false);
                submitButton.gameObject.SetActive(false);
                serverConnector.StartCheckingGameStatus(OnGameStarted, SetTimeLeft);
                break;

            case GameState.RoundInProgress:
                messageText.text = $"Distribute drones across the planets";
                Light.enabled = true;
                break;

            case GameState.ShowResults:
                messageText.text = $"Waiting for other players";
                serverConnector.GetResults();
                Light.enabled = true;
                break;

            case GameState.GameOver:
                messageText.text = $"Game over";
                Light.enabled = true;
                break;
        }

        SetGameState(currentState);
    }

    private void OnSubmitName()
    {
        string input = nameInputField.text.Trim();

        if (string.IsNullOrEmpty(input))
        {
            messageText.text = "Нікнейм не може бути порожнім!";
            return;
        }

        playerName = input;

        serverConnector.RegisterPlayer(playerName, success =>
        {
            if (success)
            {
                Debug.Log("Гравця зареєстровано!");
                SetState(GameState.WaitingForOthers);
            }
            else
            {
                Debug.Log("Не вдалося зареєструватися.");
            }
        });
    }

    public void SetGameState(GameState newState)
    {
        bool interactable = newState == GameState.RoundInProgress;

        foreach (var input in droneInputs)
        {
            input.interactable = interactable;
        }

        if (!interactable)
            validator.ClearMessages();
        else
        {
            validator.inputKronus.textComponent.enabled = true;
            validator.inputLyrion.textComponent.enabled = true;
            validator.inputMystara.textComponent.enabled = true;
            validator.inputEclipsia.textComponent.enabled = true;
            validator.inputFiora.textComponent.enabled = true;
            validator.ValidateInputs();
        }
    }

    private void SetTimeLeft(int seconds)
    {
        timeLeft = seconds;
        Debug.Log("Залишилось часу: " + timeLeft);
    }

    private void OnGameStarted()
    {
        SetState(GameState.RoundInProgress);
    }
}