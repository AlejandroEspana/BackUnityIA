using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class QuizManager : MonoBehaviour
{
    [System.Serializable]
    public class QuestionData
    {
        public string questionText;
        public string[] options;
        public int correctOptionIndex;
        public string category;
        public string difficulty;
    }

    [System.Serializable]
    public class QuizPanelUI
    {
        public GameObject panelGameObject;
        public TMP_Text questionLabel;
        public Button[] optionButtons;
        public TMP_Text[] optionLabels;
        public TMP_Text feedbackLabel;
        public Button requestFeedbackButton;
    }

    [Header("--- Listado de Preguntas (Datos) ---")]
    public QuestionData[] questions;

    [Header("--- Listado de Paneles (UI) ---")]
    public QuizPanelUI[] quizPanels;

    [Header("--- Botones de Navegación ---")]
    public Button nextButton;
    public Button prevButton;

    [Header("--- Colores de Retroalimentación ---")]
    public Color normalColor = Color.white;
    public Color correctColor = Color.green;
    public Color incorrectColor = Color.red;

    [Header("--- Audio ---")]
    public AudioSource audioSource;

    private int currentPanelIndex = 0;
    private int[] selectedAnswers; // Guarda el índice elegido por pregunta, o -1 si no se ha contestado.

    private void Start()
    {
        // Validar correspondencia entre datos y UI
        if (questions == null || quizPanels == null || questions.Length != quizPanels.Length)
        {
            Debug.LogError("Error: La cantidad de preguntas y paneles de UI no coincide.");
            return;
        }

        selectedAnswers = new int[questions.Length];
        for (int i = 0; i < selectedAnswers.Length; i++)
        {
            selectedAnswers[i] = -1; // Inicializar como no contestadas
        }

        // Configurar botones de navegación
        if (nextButton) nextButton.onClick.AddListener(OnNextClicked);
        if (prevButton) prevButton.onClick.AddListener(OnPrevClicked);

        // Configurar listeners de las opciones e IA en cada panel
        for (int p = 0; p < quizPanels.Length; p++)
        {
            int panelIndex = p; // Captura para expresiones lambda
            
            // Configurar botones de opciones del panel
            for (int o = 0; o < quizPanels[panelIndex].optionButtons.Length; o++)
            {
                int optionIndex = o;
                quizPanels[panelIndex].optionButtons[o].onClick.AddListener(() => OnOptionSelected(panelIndex, optionIndex));
            }

            // Configurar botón de feedback de IA del panel
            if (quizPanels[panelIndex].requestFeedbackButton)
            {
                quizPanels[panelIndex].requestFeedbackButton.onClick.AddListener(() => OnRequestFeedbackClicked(panelIndex));
                quizPanels[panelIndex].requestFeedbackButton.interactable = false;
            }
        }

        // Mostrar primer panel
        ShowPanel(0);
    }

    /// <summary>
    /// Cambia el panel activo de la pregunta y actualiza los estados de la UI.
    /// </summary>
    public void ShowPanel(int index)
    {
        if (index < 0 || index >= quizPanels.Length) return;

        currentPanelIndex = index;

        // Activar el panel actual y desactivar los demás
        for (int i = 0; i < quizPanels.Length; i++)
        {
            if (quizPanels[i].panelGameObject)
            {
                quizPanels[i].panelGameObject.SetActive(i == currentPanelIndex);
            }
        }

        // Renderizar el contenido de la pregunta actual
        QuestionData question = questions[currentPanelIndex];
        QuizPanelUI ui = quizPanels[currentPanelIndex];

        if (ui.questionLabel) ui.questionLabel.text = question.questionText;

        // Llenar etiquetas de opciones
        for (int o = 0; o < ui.optionButtons.Length; o++)
        {
            if (o < question.options.Length)
            {
                ui.optionButtons[o].gameObject.SetActive(true);
                if (ui.optionLabels[o]) ui.optionLabels[o].text = question.options[o];
            }
            else
            {
                ui.optionButtons[o].gameObject.SetActive(false);
            }
        }

        // Verificar si la pregunta ya fue contestada previamente
        int answeredIdx = selectedAnswers[currentPanelIndex];
        if (answeredIdx != -1)
        {
            // PREGUNTA YA CONTESTADA: Bloquear controles y marcar selección previa
            if (ui.feedbackLabel)
            {
                bool wasCorrect = (answeredIdx == question.correctOptionIndex);
                ui.feedbackLabel.text = wasCorrect 
                    ? "<color=green>[Respondido]: ¡Correcto!</color> Puedes solicitar la explicación de la IA nuevamente." 
                    : "<color=red>[Respondido]: Incorrecto.</color> La explicación de la IA está disponible.";
            }

            for (int o = 0; o < ui.optionButtons.Length; o++)
            {
                ui.optionButtons[o].interactable = false;
                Image img = ui.optionButtons[o].GetComponent<Image>();
                
                if (o == question.correctOptionIndex)
                {
                    if (img) img.color = correctColor;
                }
                else if (o == answeredIdx)
                {
                    if (img) img.color = incorrectColor;
                }
                else
                {
                    if (img) img.color = normalColor;
                }
            }

            if (ui.requestFeedbackButton) ui.requestFeedbackButton.interactable = true;
        }
        else
        {
            // PREGUNTA NO CONTESTADA: Resetear colores y controles
            if (ui.feedbackLabel) ui.feedbackLabel.text = "Selecciona una respuesta para continuar.";
            
            for (int o = 0; o < ui.optionButtons.Length; o++)
            {
                ui.optionButtons[o].interactable = true;
                Image img = ui.optionButtons[o].GetComponent<Image>();
                if (img) img.color = normalColor;
            }

            if (ui.requestFeedbackButton) ui.requestFeedbackButton.interactable = false;
        }

        UpdateNavigationButtons();
    }

    private void OnOptionSelected(int panelIndex, int optionIndex)
    {
        if (selectedAnswers[panelIndex] != -1) return; // Ya respondida

        selectedAnswers[panelIndex] = optionIndex;
        QuestionData question = questions[panelIndex];
        QuizPanelUI ui = quizPanels[panelIndex];

        // Bloquear todas las opciones en este panel
        for (int i = 0; i < ui.optionButtons.Length; i++)
        {
            ui.optionButtons[i].interactable = false;
            Image img = ui.optionButtons[i].GetComponent<Image>();

            // Resaltar acierto en verde y fallo en rojo
            if (i == question.correctOptionIndex)
            {
                if (img) img.color = correctColor;
            }
            else if (i == optionIndex)
            {
                if (img) img.color = incorrectColor;
            }
        }

        // Dar retroalimentación corta local
        bool isCorrect = (optionIndex == question.correctOptionIndex);
        if (ui.feedbackLabel)
        {
            ui.feedbackLabel.text = isCorrect 
                ? "<color=green>¡Correcto!</color> ¿Quieres saber por qué? Solicita la explicación de la IA." 
                : "<color=red>Incorrecto.</color> La IA puede darte una explicación pedagógica ahora.";
        }

        // Registrar analítica silenciosa del resultado en el backend
        string details = $"Pregunta: '{question.questionText}' | Seleccionó: '{question.options[optionIndex]}' | Correcto: {isCorrect} | Tema: {question.category}";
        StartCoroutine(LogAnalyticsCoroutine("quiz_answer", details));

        // Habilitar botón para solicitar retroalimentación detallada
        if (ui.requestFeedbackButton) ui.requestFeedbackButton.interactable = true;

        UpdateNavigationButtons();
    }

    private void OnNextClicked()
    {
        if (currentPanelIndex < quizPanels.Length - 1)
        {
            ShowPanel(currentPanelIndex + 1);
        }
    }

    private void OnPrevClicked()
    {
        if (currentPanelIndex > 0)
        {
            ShowPanel(currentPanelIndex - 1);
        }
    }

    private void UpdateNavigationButtons()
    {
        if (prevButton) prevButton.interactable = (currentPanelIndex > 0);
        if (nextButton) nextButton.interactable = (currentPanelIndex < quizPanels.Length - 1);
    }

    private void OnRequestFeedbackClicked(int panelIndex)
    {
        QuizPanelUI ui = quizPanels[panelIndex];
        if (ui.requestFeedbackButton) ui.requestFeedbackButton.interactable = false;
        if (ui.feedbackLabel) ui.feedbackLabel.text = "El tutor virtual está preparando tu explicación personalizada... ⏳";

        StartCoroutine(RequestFeedbackCoroutine(panelIndex));
    }

    private IEnumerator RequestFeedbackCoroutine(int panelIndex)
    {
        QuestionData question = questions[panelIndex];
        QuizPanelUI ui = quizPanels[panelIndex];
        int answeredIdx = selectedAnswers[panelIndex];

        QuizFeedbackRequest requestData = new QuizFeedbackRequest
        {
            question_text = question.questionText,
            options = question.options,
            correct_option = question.options[question.correctOptionIndex],
            selected_option = question.options[answeredIdx],
            category = question.category,
            difficulty = question.difficulty
        };

        string json = JsonUtility.ToJson(requestData);

        using (UnityWebRequest req = new UnityWebRequest(ApiManager.Instance.apiBaseUrl + "/quiz/feedback", "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            using (UploadHandlerRaw uh = new UploadHandlerRaw(body))
            {
                req.uploadHandler = uh;
                req.downloadHandler = new DownloadHandlerAudioClip(ApiManager.Instance.apiBaseUrl + "/quiz/feedback", AudioType.WAV);
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("Authorization", "Bearer " + ApiManager.Instance.authToken);
                req.SetRequestHeader("X-Project-ID", ApiManager.Instance.projectId);
                req.timeout = 180;

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Quiz feedback error: {req.error} | {req.responseCode}");
                    if (ui.feedbackLabel) ui.feedbackLabel.text = $"Error al obtener explicación de IA (HTTP {req.responseCode})";
                }
                else
                {
                    // 1. Reproducir el audio de voz sintética explicativa
                    AudioClip downloadedClip = ((DownloadHandlerAudioClip)req.downloadHandler).audioClip;
                    if (downloadedClip != null && audioSource != null)
                    {
                        audioSource.clip = downloadedClip;
                        audioSource.Play();
                    }

                    // 2. Extraer el texto de la cabecera personalizada para subtítulos
                    string encodedText = req.GetResponseHeader("X-Response-Text");
                    if (!string.IsNullOrEmpty(encodedText) && ui.feedbackLabel != null)
                    {
                        ui.feedbackLabel.text = UnityWebRequest.UnEscapeURL(encodedText);
                    }
                    else
                    {
                        if (ui.feedbackLabel) ui.feedbackLabel.text = "Explicación recibida (audio emitido).";
                    }
                }
            }
        }
        
        if (ui.requestFeedbackButton) ui.requestFeedbackButton.interactable = true;
    }

    private IEnumerator LogAnalyticsCoroutine(string activityType, string details)
    {
        string json = $"{{{\"activity_type\":\"{activityType}\",\"details\":\"{details}\"}}}";

        using (UnityWebRequest req = new UnityWebRequest(ApiManager.Instance.apiBaseUrl + "/analytics/log", "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            using (UploadHandlerRaw uh = new UploadHandlerRaw(body))
            using (DownloadHandlerBuffer dh = new DownloadHandlerBuffer())
            {
                req.uploadHandler = uh;
                req.downloadHandler = dh;
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("Authorization", "Bearer " + ApiManager.Instance.authToken);
                req.SetRequestHeader("X-Project-ID", ApiManager.Instance.projectId);

                yield return req.SendWebRequest();
            }
        }
    }
}
