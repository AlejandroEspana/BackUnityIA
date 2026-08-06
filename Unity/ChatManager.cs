using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class ChatManager : MonoBehaviour
{
    [Header("--- UI de Chat ---")]
    public TMP_InputField chatInput;
    public TMP_Text chatOutputText;
    public Button chatSendButton;
    public AudioSource audioSource;

    private void Start()
    {
        if (chatSendButton) chatSendButton.onClick.AddListener(OnSendClicked);
    }

    public void OnSendClicked()
    {
        string userText = chatInput.text;
        if (string.IsNullOrWhiteSpace(userText)) return;

        chatSendButton.interactable = false;
        if (chatOutputText) chatOutputText.text = "El guardián está consultando sus memorias...";
        chatInput.text = "";

        StartCoroutine(ChatCoroutine(userText));
    }

    private IEnumerator ChatCoroutine(string message)
    {
        ChatRequest chatData = new ChatRequest { message = message };
        string json = JsonUtility.ToJson(chatData);

        using (UnityWebRequest req = new UnityWebRequest(ApiManager.Instance.apiBaseUrl + "/chat", "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            using (UploadHandlerRaw uh = new UploadHandlerRaw(body))
            {
                req.uploadHandler = uh;
                req.downloadHandler = new DownloadHandlerAudioClip(ApiManager.Instance.apiBaseUrl + "/chat", AudioType.WAV);
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("Authorization", "Bearer " + ApiManager.Instance.authToken);
                req.SetRequestHeader("X-Project-ID", ApiManager.Instance.projectId);
                req.timeout = 180;

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Chat error: {req.error} | {req.responseCode}");
                    if (chatOutputText) chatOutputText.text = $"Falló la conexión. ¿Aún tienes la sesión activa? (HTTP {req.responseCode})";
                }
                else
                {
                    AudioClip downloadedClip = ((DownloadHandlerAudioClip)req.downloadHandler).audioClip;
                    if (downloadedClip != null && audioSource != null)
                    {
                        audioSource.clip = downloadedClip;
                        audioSource.Play();
                    }

                    string encodedText = req.GetResponseHeader("X-Response-Text");
                    if (!string.IsNullOrEmpty(encodedText) && chatOutputText != null)
                    {
                        chatOutputText.text = UnityWebRequest.UnEscapeURL(encodedText);
                    }
                    else
                    {
                        if (chatOutputText) chatOutputText.text = "Error al recibir transcripción de texto.";
                    }
                }
            }
        }
        chatSendButton.interactable = true;
    }

    public void SendAudio(byte[] wavBytes)
    {
        StartCoroutine(SendAudioCoroutine(wavBytes));
    }

    private IEnumerator SendAudioCoroutine(byte[] wavBytes)
    {
        System.Collections.Generic.List<IMultipartFormSection> formData = new System.Collections.Generic.List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection("audio_file", wavBytes, "voice.wav", "audio/wav"));

        using (UnityWebRequest req = UnityWebRequest.Post(ApiManager.Instance.apiBaseUrl + "/chat_audio", formData))
        {
            req.SetRequestHeader("Authorization", "Bearer " + ApiManager.Instance.authToken);
            req.SetRequestHeader("X-Project-ID", ApiManager.Instance.projectId);
            req.downloadHandler = new DownloadHandlerAudioClip(ApiManager.Instance.apiBaseUrl + "/chat_audio", AudioType.WAV);
            req.timeout = 240;
            
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Audio Chat error: {req.error}");
                if (chatOutputText) chatOutputText.text = "Error al comunicarse con el Dios Destructor.";
            }
            else
            {
                AudioClip downloadedClip = ((DownloadHandlerAudioClip)req.downloadHandler).audioClip;
                if (downloadedClip != null && audioSource != null)
                {
                    audioSource.clip = downloadedClip;
                    audioSource.Play();
                }

                string encodedText = req.GetResponseHeader("X-Response-Text");
                if (!string.IsNullOrEmpty(encodedText) && chatOutputText != null)
                {
                    chatOutputText.text = UnityWebRequest.UnEscapeURL(encodedText);
                }
                else
                {
                    if (chatOutputText) chatOutputText.text = "El Dios Destructor ha respondido (audio emitido).";
                }
            }
        }
    }
}
