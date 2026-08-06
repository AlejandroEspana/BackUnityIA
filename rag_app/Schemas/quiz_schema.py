from pydantic import BaseModel
from typing import List, Optional

class QuizFeedbackRequest(BaseModel):
    question_text: str
    options: List[str]
    correct_option: str
    selected_option: str
    category: Optional[str] = "POO"
    difficulty: Optional[str] = "Medio"
