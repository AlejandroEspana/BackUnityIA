using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SaveGameManager : MonoBehaviour
{
    public Button saveGameButton;
    public Button loadGameButton;
    public Text chatOutputText;

    private void Start()
    {
        if (saveGameButton) saveGameButton.onClick.AddListener(SaveGame);
        if (loadGameButton) loadGameButton.onClick.AddListener(LoadGame);
    }

    public void SaveGame()
    {
        if (!ApiManager.Instance.IsAuthenticated()) return;

        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Vector3 playerPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        string worldState = "{\"day\": 1, \"weather\": \"clear\"}";
        string lastMsg = chatOutputText != null ? chatOutputText.text : "";

        SaveData data = new SaveData(currentScene, playerPos, worldState, lastMsg);
        byte[] binaryData = SaveSystem.Serialize(data);
        string base64Data = System.Convert.ToBase64String(binaryData);

        StartCoroutine(SaveCoroutine(base64Data));
    }

    private IEnumerator SaveCoroutine(string base64Data)
    {
        SaveRequest reqData = new SaveRequest { save_data_base64 = base64Data };
        string json = JsonUtility.ToJson(reqData);

        using (UnityWebRequest req = new UnityWebRequest(ApiManager.Instance.apiBaseUrl + "/save", "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            using (UploadHandlerRaw uh = new UploadHandlerRaw(body))
            using (DownloadHandlerBuffer dh = new DownloadHandlerBuffer())
            {
                req.uploadHandler = uh;
                req.downloadHandler = dh;
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("Authorization", "Bearer " + ApiManager.Instance.authToken);

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Save failed: {req.error}");
                    if (chatOutputText) chatOutputText.text = "Error al guardar la partida.";
                }
                else
                {
                    Debug.Log("Partida guardada con éxito.");
                    if (chatOutputText) chatOutputText.text += "\n[Sistema]: Partida guardada.";
                }
            }
        }
    }

    public void LoadGame()
    {
        if (!ApiManager.Instance.IsAuthenticated()) return;
        StartCoroutine(LoadCoroutine());
    }

    private IEnumerator LoadCoroutine()
    {
        using (UnityWebRequest req = UnityWebRequest.Get(ApiManager.Instance.apiBaseUrl + "/save"))
        {
            req.SetRequestHeader("Authorization", "Bearer " + ApiManager.Instance.authToken);

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Load failed: {req.error}");
                if (chatOutputText) chatOutputText.text = "Error al cargar la partida. ¿Quizás no tienes ninguna?";
            }
            else
            {
                SaveResponse res = JsonUtility.FromJson<SaveResponse>(req.downloadHandler.text);
                if (res != null && !string.IsNullOrEmpty(res.save_data_base64))
                {
                    byte[] binaryData = System.Convert.FromBase64String(res.save_data_base64);
                    SaveData data = SaveSystem.Deserialize(binaryData);

                    if (data != null)
                    {
                        Debug.Log($"Cargando escena: {data.SceneName}");
                        if (Camera.main != null) Camera.main.transform.position = data.GetPosition();
                        
                        if (chatOutputText) chatOutputText.text = data.LastConversation;
                        Debug.Log($"World State: {data.WorldStateJson}");
                        if (chatOutputText) chatOutputText.text += "\n[Sistema]: Partida cargada con éxito.";
                    }
                }
            }
        }
    }
}
