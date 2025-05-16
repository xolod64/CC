using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ServerConnector : MonoBehaviour
{
    private string register = "https://f646-93-170-117-28.ngrok-free.app/game_server/register.php";
    private string lobby = "https://f646-93-170-117-28.ngrok-free.app/game_server/start_game.php";
    private string move = "https://f646-93-170-117-28.ngrok-free.app/game_server/submit_move.php";

    public void RegisterPlayer(string username, Action<bool> onComplete)
    {
        StartCoroutine(RegisterCoroutine(username, onComplete));
    }

    private IEnumerator RegisterCoroutine(string username, Action<bool> onComplete)
    {
        string jsonData = JsonUtility.ToJson(new PlayerData { username = username });
        byte[] postData = System.Text.Encoding.UTF8.GetBytes(jsonData);

        UnityWebRequest request = new UnityWebRequest(register, "POST");
        request.uploadHandler = new UploadHandlerRaw(postData);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;
            Debug.Log("Відповідь сервера: " + responseText);

            // Парсимо відповідь у клас ErrorResponse
            ErrorResponse errorResponse = JsonUtility.FromJson<ErrorResponse>(responseText);

            // Якщо поле error не порожнє, вважаємо, що сталася серверна помилка
            if (!string.IsNullOrEmpty(errorResponse.error))
            {
                Debug.LogError("Помилка від сервера: " + errorResponse.error);
                onComplete?.Invoke(false);
            }
            else
            {
                onComplete?.Invoke(true);
            }
        }
        else
        {
            Debug.LogError("Помилка при підключенні до сервера: " + request.error);
            onComplete?.Invoke(false);
        }
    }

    public void CheckStatus()
    {
        StartCoroutine(GetStatusCoroutine());
    }

    private IEnumerator GetStatusCoroutine()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(lobby))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Debug.Log("Відповідь сервера: " + json);

                // Розбираємо JSON
                GameStatusResponse response = JsonUtility.FromJson<GameStatusResponse>(json);

                if (response != null)
                {
                    Debug.Log("Status: " + response.status);

                    // Тут можна щось робити з цим статусом, наприклад:
                    if (response.status == "waiting")
                    {
                        Debug.Log("Гра ще не почалася");
                    }
                    else if (response.status == "started")
                    {
                        Debug.Log("Гра почалася");
                    }
                }
                else
                {
                    Debug.LogError("Не вдалося розпарсити JSON");
                }
            }
            else
            {
                Debug.LogError("Помилка запиту: " + request.error);
            }
        }
    }

    public void StartCheckingGameStatus(Action onGameStarted, Action<int> onTimeLeftUpdate)
    {
        StartCoroutine(CheckGameStatusLoop(onGameStarted, onTimeLeftUpdate));
    }

    private IEnumerator CheckGameStatusLoop(Action onGameStarted, Action<int> onTimeLeftUpdate)
    {
        while (true)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(lobby))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("HTTP-помилка: " + www.error);
                }
                else
                {
                    string json = www.downloadHandler.text;
                    GameStatusResponse response = JsonUtility.FromJson<GameStatusResponse>(json);

                    if (response.status == "waiting")
                    {
                        onTimeLeftUpdate?.Invoke(response.time_left);
                    }
                    else if (response.status == "started")
                    {
                        onGameStarted?.Invoke();
                        yield break;
                    }
                }
            }

            yield return new WaitForSeconds(1f);
        }
    }

    public void SendDroneDistribution(int playerId, int kronus, int lyrion, int mystara, int eclipsia, int fiora)
    {
        WWWForm form = new WWWForm();
        form.AddField("player_id", playerId);
        form.AddField("kronus", kronus);
        form.AddField("lyrion", lyrion);
        form.AddField("mystara", mystara);
        form.AddField("eclipsia", eclipsia);
        form.AddField("fiora", fiora);

        StartCoroutine(PostFormRequest(move, form));
    }

    private IEnumerator PostFormRequest(string url, WWWForm form)
    {
        UnityWebRequest request = UnityWebRequest.Post(url, form);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Успішно надіслано: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Помилка запиту: " + request.error);
        }
    }

    [Serializable]
    private class ErrorResponse
    {
        public string error;
    }

    [System.Serializable]
    public class PlayerData
    {
        public string username;
    }

    [Serializable]
    private class GameStatusResponse
    {
        public string status;
        public int time_left;
        public string message;
        public int player_count;
        public int elapsed_time;
        public string error;
    }

    [System.Serializable]
    public class DroneData
    {
        public int Kronus;
        public int Lyrion;
        public int Mystara;
        public int Eclipsia;
        public int Fiora;
    }
}