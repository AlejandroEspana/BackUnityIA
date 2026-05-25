using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AuthManager : MonoBehaviour
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

    [Header("--- Siguiente Panel ---")]
    public GameObject chatPanel;
    public Text chatOutputText;

    private void Start()
    {
        if (loginPanel) loginPanel.SetActive(true);
        if (registerPanel) registerPanel.SetActive(false);
        if (chatPanel) chatPanel.SetActive(false);

        if (loginButton) loginButton.onClick.AddListener(OnLoginClicked);
        if (switchToRegisterButton) switchToRegisterButton.onClick.AddListener(OnSwitchToRegister);
        
        if (registerSendButton) registerSendButton.onClick.AddListener(OnRegisterClicked);
        if (switchToLoginButton) switchToLoginButton.onClick.AddListener(OnSwitchToLogin);
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

        using (UnityWebRequest req = new UnityWebRequest(ApiManager.Instance.apiBaseUrl + "/register", "POST"))
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
                    if (registerStatusText) registerStatusText.text = $"Error al registrar. (HTTP {req.responseCode})";
                    registerSendButton.interactable = true;
                }
                else
                {
                    if (registerStatusText) registerStatusText.text = "¡Registro exitoso! Ahora inicia sesión.";
                    registerSendButton.interactable = true;
                    yield return new WaitForSeconds(1.5f);
                    OnSwitchToLogin();
                }
            }
        }
    }

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

        using (UnityWebRequest req = new UnityWebRequest(ApiManager.Instance.apiBaseUrl + "/login", "POST"))
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
                        ApiManager.Instance.authToken = res.access_token;
                        if (loginStatusText) loginStatusText.text = "¡Acceso concedido!";
                        
                        if (loginPanel) loginPanel.SetActive(false);
                        if (chatPanel) chatPanel.SetActive(true);
                        if (chatOutputText) chatOutputText.text = "Sesión iniciada. ¿En qué te puedo ayudar hoy?";
                    }
                }
            }
        }
    }
}
