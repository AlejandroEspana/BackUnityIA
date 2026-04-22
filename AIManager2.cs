using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AIManager2 : MonoBehaviour
{
    [Header("--- UI de Login ---")]
    public GameObject loginPanel;
    public InputField loginUsernameInput;
    public InputField loginPasswordInput;
    public Button loginButton;
    public Button switchToRegisterButton;
    public Text loginStatusText;

    [Header("--- UI de Registro ---")]
    public GameObject registerPanel;
    public InputField registerUsernameInput;
    public InputField registerPasswordInput;
    public Button registerSendButton;
    public Button switchToLoginButton;
    public Text registerStatusText;

    [Header("--- UI de Chat ---")]
    public GameObject chatPanel;
    public InputField chatInput;
    public Text chatOutputText;
    public Button chatSendButton;

    [Header("--- UI de Voz (Push to Talk) ---")]
    public AudioSource audioSource;
    public Button pushToTalkButton;
    public bool useSpacebarToTalk = true;
    
    private AudioClip recordingClip;
    private bool isRecording = false;
    private string microphoneDevice;

    [Header("API Config")]
    private readonly string apiBaseUrl = "http://127.0.0.1:8000";
    private string authToken = "";

    private void Start()
    {
        // Estado inicial
        if (loginPanel) loginPanel.SetActive(true);
        if (registerPanel) registerPanel.SetActive(false);
        if (chatPanel) chatPanel.SetActive(false);

        if (loginButton) loginButton.onClick.AddListener(OnLoginClicked);
        if (switchToRegisterButton) switchToRegisterButton.onClick.AddListener(OnSwitchToRegister);
        
        if (registerSendButton) registerSendButton.onClick.AddListener(OnRegisterClicked);
        if (switchToLoginButton) switchToLoginButton.onClick.AddListener(OnSwitchToLogin);

        if (chatSendButton) chatSendButton.onClick.AddListener(OnSendClicked);
    }

    private void Update()
    {
        if (useSpacebarToTalk && chatPanel != null && chatPanel.activeInHierarchy)
        {
            if (Input.GetKeyDown(KeyCode.Space)) StartRecording();
            if (Input.GetKeyUp(KeyCode.Space)) StopRecordingAndSend();
        }
    }

    public void OnSwitchToRegister()
    {
        if (loginPanel) loginPanel.SetActive(false);
        if (registerPanel) registerPanel.SetActive(true);
        if (registerStatusText) registerStatusText.text = "¡Crea tu nueva cuenta de Guardia!";
    }

    public void OnSwitchToLogin()
    {
        if (registerPanel) registerPanel.SetActive(false);
        if (loginPanel) loginPanel.SetActive(true);
    }

    // --- REGISTRO ---
    public void OnRegisterClicked()
    {
        string user = registerUsernameInput.text;
        string pass = registerPasswordInput.text;
        
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass)) return;
        
        registerSendButton.interactable = false;
        if (registerStatusText) registerStatusText.text = "Registrando en la base de datos...";

        StartCoroutine(RegisterCoroutine(user, pass));
    }

    private IEnumerator RegisterCoroutine(string username, string password)
    {
        RegisterRequest regData = new RegisterRequest { username = username, password = password };
        string json = JsonUtility.ToJson(regData);

        using (UnityWebRequest req = new UnityWebRequest(apiBaseUrl + "/register", "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            using (UploadHandlerRaw uh = new UploadHandlerRaw(body))
            using (DownloadHandlerBuffer dh = new DownloadHandlerBuffer())
            {
                req.uploadHandler = uh;
                req.downloadHandler = dh;
                req.SetRequestHeader("Content-Type", "application/json");

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Register failed: {req.error} | {req.responseCode}");
                    if (registerStatusText) registerStatusText.text = $"Error al registrar: El nombre de usuario quizá ya existe. (HTTP {req.responseCode})";
                    registerSendButton.interactable = true;
                }
                else
                {
                    if (registerStatusText) registerStatusText.text = "¡Registro exitoso! Ahora inicia sesión.";
                    registerSendButton.interactable = true;
                    // Cambio automático a Login tras un breve delay
                    yield return new WaitForSeconds(1.5f);
                    OnSwitchToLogin();
                }
            }
        }
    }

    // --- LOGIN ---
    public void OnLoginClicked()
    {
        string user = loginUsernameInput.text;
        string pass = loginPasswordInput.text;
        
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass)) return;
        
        loginButton.interactable = false;
        if (loginStatusText) loginStatusText.text = "Verificando Credenciales...";

        StartCoroutine(LoginCoroutine(user, pass));
    }

    private IEnumerator LoginCoroutine(string username, string password)
    {
        LoginRequest loginData = new LoginRequest { username = username, password = password };
        string json = JsonUtility.ToJson(loginData);

        using (UnityWebRequest req = new UnityWebRequest(apiBaseUrl + "/login", "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            using (UploadHandlerRaw uh = new UploadHandlerRaw(body))
            using (DownloadHandlerBuffer dh = new DownloadHandlerBuffer())
            {
                req.uploadHandler = uh;
                req.downloadHandler = dh;
                req.SetRequestHeader("Content-Type", "application/json");

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Login failed: {req.error} | {req.responseCode}");
                    if (loginStatusText) loginStatusText.text = "Usuario o contraseña inválidos.";
                    loginButton.interactable = true;
                }
                else
                {
                    TokenResponse res = JsonUtility.FromJson<TokenResponse>(req.downloadHandler.text);
                    if (res != null && !string.IsNullOrEmpty(res.access_token))
                    {
                        authToken = res.access_token;
                        if (loginStatusText) loginStatusText.text = "¡Acceso concedido!";
                        
                        // Ocultar login y mostrar chat
                        if (loginPanel) loginPanel.SetActive(false);
                        if (chatPanel) chatPanel.SetActive(true);
                        if (chatOutputText) chatOutputText.text = "Sesión iniciada. ¿En qué te puedo ayudar hoy?";
                    }
                }
            }
        }
    }

    // --- CHAT ---
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

        using (UnityWebRequest req = new UnityWebRequest(apiBaseUrl + "/chat", "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            using (UploadHandlerRaw uh = new UploadHandlerRaw(body))
            {
                req.uploadHandler = uh;
                req.downloadHandler = new DownloadHandlerAudioClip(apiBaseUrl + "/chat", AudioType.WAV);
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("Authorization", "Bearer " + authToken);
                req.timeout = 180; // Aumentado porque el TTS local puede tomar unos segundos

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Chat error: {req.error} | {req.responseCode}");
                    if (chatOutputText) chatOutputText.text = $"Falló la conexión. ¿Aún tienes la sesión activa? (HTTP {req.responseCode})";
                }
                else
                {
                    // 1. Obtener y reproducir Audio
                    AudioClip downloadedClip = ((DownloadHandlerAudioClip)req.downloadHandler).audioClip;
                    if (downloadedClip != null && audioSource != null)
                    {
                        audioSource.clip = downloadedClip;
                        audioSource.Play();
                    }

                    // 2. Obtener y mostrar Texto de la cabecera X-Response-Text
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

    // --- CHAT POR VOZ (Push to Talk) ---
    public void StartRecording()
    {
        if (Microphone.devices.Length == 0) {
            Debug.LogError("No hay micrófono detectado.");
            return;
        }
        microphoneDevice = Microphone.devices[0];
        // Grabamos hasta 15 segundos máximo en la frecuencia estándar 44100
        recordingClip = Microphone.Start(microphoneDevice, false, 15, 44100);
        isRecording = true;
        if (chatOutputText) chatOutputText.text = "Escuchando...🎙️ (suelta para enviar)";
    }

    public void StopRecordingAndSend()
    {
        if (!isRecording) return;
        Microphone.End(microphoneDevice);
        isRecording = false;
        if (chatOutputText) chatOutputText.text = "Procesando la voz con la IA en local... ⏳";
        
        byte[] wavBytes = EncodeToWAV(recordingClip);
        StartCoroutine(SendAudioCoroutine(wavBytes));
    }

    private IEnumerator SendAudioCoroutine(byte[] wavBytes)
    {
        System.Collections.Generic.List<IMultipartFormSection> formData = new System.Collections.Generic.List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection("audio_file", wavBytes, "voice.wav", "audio/wav"));

        using (UnityWebRequest req = UnityWebRequest.Post(apiBaseUrl + "/chat_audio", formData))
        {
            req.SetRequestHeader("Authorization", "Bearer " + authToken);
            req.downloadHandler = new DownloadHandlerAudioClip(apiBaseUrl + "/chat_audio", AudioType.WAV);
            req.timeout = 240; // Mayor timeout, procesar STT, LLM y TTS en local PC gasta tiempo
            
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

    private byte[] EncodeToWAV(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        using (var memoryStream = new System.IO.MemoryStream())
        using (var writer = new System.IO.BinaryWriter(memoryStream))
        {
            writer.Write(Encoding.UTF8.GetBytes("RIFF"));
            writer.Write(36 + samples.Length * 2);
            writer.Write(Encoding.UTF8.GetBytes("WAVE"));
            writer.Write(Encoding.UTF8.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1); // Formato PCM
            writer.Write((short)clip.channels);
            writer.Write(clip.frequency);
            writer.Write(clip.frequency * clip.channels * 2);
            writer.Write((short)(clip.channels * 2));
            writer.Write((short)16); // Bits por muestra
            writer.Write(Encoding.UTF8.GetBytes("data"));
            writer.Write(samples.Length * 2);

            foreach (var sample in samples)
            {
                // Limitar entre -1 y 1
                float s = sample;
                if (s > 1f) s = 1f;
                if (s < -1f) s = -1f;
                short intSample = (short)(s * 32767f);
                writer.Write(intSample);
            }

            return memoryStream.ToArray();
        }
    }

    // --- Modelos JSON ---
    [System.Serializable] private class RegisterRequest { public string username; public string password; }
    [System.Serializable] private class LoginRequest { public string username; public string password; }
    [System.Serializable] private class TokenResponse { public string access_token; }
    [System.Serializable] private class ChatRequest { public string message; }
    [System.Serializable] private class ChatResponse { public string response; }
}