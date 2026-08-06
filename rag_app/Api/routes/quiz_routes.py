from fastapi import APIRouter, Depends, Header, HTTPException
from fastapi.responses import FileResponse
import urllib.parse
import os
from Schemas.quiz_schema import QuizFeedbackRequest
from Core.security import get_current_user_id
from Core.config import ROOT_DIR
from Api.routes.chat_routes import rag_engine, audio_engine, analytics_repo

router = APIRouter()

@router.post("/feedback")
async def get_quiz_feedback(
    request: QuizFeedbackRequest,
    user_id: int = Depends(get_current_user_id),
    project_id: str = Header("default", alias="X-Project-ID")
):
    if not audio_engine:
        raise HTTPException(status_code=500, detail="Audio service not available")
        
    try:
        # 1. Generar la retroalimentación a través de RAG + LLM con el contexto de AlgoLab/CUPI2
        feedback_text = rag_engine.generate_quiz_feedback(
            question_text=request.question_text,
            options=request.options,
            correct_option=request.correct_option,
            selected_option=request.selected_option,
            category=request.category,
            difficulty=request.difficulty,
            project_id=project_id
        )

        # 2. Registrar la analítica del estudiante
        is_correct = (request.selected_option == request.correct_option)
        try:
            analytics_repo.log_activity(
                user_id=user_id,
                project_id=project_id,
                activity_type="quiz_answer",
                details=f"Pregunta: '{request.question_text}' | Seleccionó: '{request.selected_option}' | Correcta: '{request.correct_option}' | Acertó: {is_correct} | Categoría: {request.category} | Dificultad: {request.difficulty}"
            )
        except Exception as e_log:
            print(f"[ANALYTICS ERROR] Falló el registro automático en quiz: {e_log}")

        # 3. Convertir la retroalimentación a audio WAV (TTS)
        tmp_dir = os.path.join(ROOT_DIR, "tmp")
        os.makedirs(tmp_dir, exist_ok=True)
        output_path = os.path.join(tmp_dir, f"quiz_feedback_{user_id}.wav")
        audio_engine.text_to_speech(feedback_text, output_path)

        # 4. Codificar el texto para el header X-Response-Text (compatible con Unity)
        encoded_text = urllib.parse.quote(feedback_text)
        headers = {
            "Access-Control-Expose-Headers": "X-Response-Text",
            "X-Response-Text": encoded_text
        }

        return FileResponse(
            path=output_path,
            media_type="audio/wav",
            filename="feedback.wav",
            headers=headers
        )
        
    except Exception as e:
        print(f"Error generando feedback de quiz: {e}")
        raise HTTPException(status_code=500, detail="Error de procesamiento de feedback de Quiz")
