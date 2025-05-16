using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ServerConnector : MonoBehaviour
{
    private string register = "https://f646-93-170-117-28.ngrok-free.app/game_server/register.php";
    private string lobby = "https://f646-93-170-117-28.ngrok-free.app/game_server/start_game.php";
    private string move = "https://f646-93-170-117-28.ngrok-free.app/game_server/submit_move.php";

    public int ID;

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

            // Парсимо поле error
            ErrorResponse errorResponse = JsonUtility.FromJson<ErrorResponse>(responseText);

            if (!string.IsNullOrEmpty(errorResponse.error))
            {
                Debug.LogError("Помилка від сервера: " + errorResponse.error);
                onComplete?.Invoke(false);
            }
            else
            {
                // 🔍 Знаходимо player_id за допомогою регулярного виразу
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(responseText, "\"player_id\"\\s*:\\s*(\\d+)");
                if (match.Success)
                {
                    int playerId = int.Parse(match.Groups[1].Value);
                    PlayerPrefs.SetInt("player_id", playerId);
                    PlayerPrefs.Save();

                    ID = playerId;
                    onComplete?.Invoke(true);
                }
                else
                {
                    Debug.LogError("player_id не знайдено у відповіді.");
                    onComplete?.Invoke(false);
                }
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
            string urlWithTimestamp = lobby + "?t=" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            using (UnityWebRequest www = UnityWebRequest.Get(urlWithTimestamp))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("HTTP-помилка: " + www.error);
                }
                else
                {
                    string json = www.downloadHandler.text;
                    Debug.Log("CheckGameStatusLoop JSON: " + json);

                    GameStatusResponse response = null;
                    bool parseSuccess = false;

                    try
                    {
                        response = JsonUtility.FromJson<GameStatusResponse>(json);
                        parseSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Помилка парсингу JSON: " + ex.Message);
                    }

                    if (!parseSuccess)
                    {
                        yield return new WaitForSeconds(1f);
                        continue;
                    }

                    if (response.status == "waiting")
                    {
                        if (response.time_left.HasValue)
                            onTimeLeftUpdate?.Invoke(response.time_left.Value);
                    }
                    else if (response.status == "started")
                    {
                        Debug.Log("Гра почалася!");
                        onGameStarted?.Invoke();
                        yield break;
                    }
                    else
                    {
                        Debug.LogWarning("Невідомий статус гри: " + response.status);
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
    public class GameStatusResponse
    {
        public string status;
        public int? time_left; // nullable, бо іноді його немає
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