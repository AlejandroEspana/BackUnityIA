using UnityEngine;

public class ApiManager : MonoBehaviour
{
    public static ApiManager Instance { get; private set; }

    [Header("API Config")]
    public string apiBaseUrl = "http://127.0.0.1:8000";
    public string projectId = "default_project";
    
    [HideInInspector]
    public string authToken = "";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public bool IsAuthenticated()
    {
        return !string.IsNullOrEmpty(authToken);
    }
}
