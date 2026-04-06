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
            using (DownloadHandlerBuffer dh = new DownloadHandlerBuffer())
            {
                req.uploadHandler = uh;
                req.downloadHandler = dh;
                req.SetRequestHeader("Content-Type", "application/json");
                
                req.SetRequestHeader("Authorization", "Bearer " + authToken);
                req.timeout = 120;

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Chat error: {req.error} | {req.responseCode}");
                    if (req.downloadHandler != null) Debug.LogError(req.downloadHandler.text);
                    if (chatOutputText) chatOutputText.text = $"Falló la conexión. ¿Aún tienes la sesión activa? (HTTP {req.responseCode})";
                }
                else
                {
                    ChatResponse res = JsonUtility.FromJson<ChatResponse>(req.downloadHandler.text);
                    if (res != null && !string.IsNullOrEmpty(res.response))
                    {
                        if (chatOutputText) chatOutputText.text = res.response.Trim();
                    }
                    else
                    {
                        if (chatOutputText) chatOutputText.text = "Respuesta vacía del servidor.";
                    }
                }
            }
        }
        chatSendButton.interactable = true;
    }

    // --- Modelos JSON ---
    [System.Serializable] private class RegisterRequest { public string username; public string password; }
    [System.Serializable] private class LoginRequest { public string username; public string password; }
    [System.Serializable] private class TokenResponse { public string access_token; }
    [System.Serializable] private class ChatRequest { public string message; }
    [System.Serializable] private class ChatResponse { public string response; }
}