using System;

[Serializable]
public class RegisterRequest { public string username; public string password; }

[Serializable]
public class LoginRequest { public string username; public string password; }

[Serializable]
public class TokenResponse { public string access_token; }

[Serializable]
public class ChatRequest { public string message; }

[Serializable]
public class ChatResponse { public string response; }

[Serializable]
public class SaveRequest { public string save_data_base64; }

[Serializable]
public class SaveResponse { public string save_data_base64; public string message; }

[Serializable]
public class QuizFeedbackRequest
{
    public string question_text;
    public string[] options;
    public string correct_option;
    public string selected_option;
    public string category;
    public string difficulty;
}

