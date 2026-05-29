from fastapi import APIRouter, HTTPException, Depends, UploadFile, File, Header
from fastapi.responses import FileResponse
import urllib.parse
import os
from Schemas.chat_schema import ChatRequest, ChatResponse
from Core.security import get_current_user_id
from Services.rag_service import RAGService
from Repositories.memory_repository import MemoryRepository
from Repositories.analytics_repository import AnalyticsRepository
from Services.audio_service import AudioService
from Services.audio_service_fallback import FallbackAudioService
from Core.config import ROOT_DIR

router = APIRouter()

rag_engine = RAGService()
memory_repo = MemoryRepository()
analytics_repo = AnalyticsRepository()

try:
    audio_engine = AudioService()
except Exception as e:
    print(f"\nFailed to initialize AudioService: {e}")
    try:
        audio_engine = FallbackAudioService()
    except Exception as e_fallback:
        print(f"Failed to initialize FallbackAudioService: {e_fallback}")
        audio_engine = None

@router.post("/reload_docs", response_model=ChatResponse)
def reload_docs(
    user_id: int = Depends(get_current_user_id),
    project_id: str = Header("default", alias="X-Project-ID")
):
    """
    Recarga y sincroniza explícitamente los documentos del proyecto especificado.
    """
    try:
        rag_engine.load_or_rebuild(project_id=project_id, force_rebuild=True)
        return ChatResponse(response=f"Documentos recargados explícitamente para el proyecto '{project_id}'.")
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.post("/chat")
async def chat(
    request: ChatRequest, 
    user_id: int = Depends(get_current_user_id),
    project_id: str = Header("default", alias="X-Project-ID")
):
    if not audio_engine:
        raise HTTPException(status_code=500, detail="Audio service not available")
    try:
        # 1. Obtener historial semántico de conversación
        history_text = memory_repo.get_recent_history(user_id)
        
        # 2. Obtener respuesta del RAG aislado del proyecto
        response_text = rag_engine.chat(request.message, history_text, project_id=project_id)
        
        # 3. Guardar en historial de chat SQLite
        memory_repo.add_message(user_id, "user", request.message)
        memory_repo.add_message(user_id, "assistant", response_text)
        
        # 4. Registrar de forma automática la analítica para retroalimentación
        try:
            analytics_repo.log_activity(
                user_id=user_id,
                project_id=project_id,
                activity_type="chat_question",
                details=f"Pregunta: '{request.message}' | Respuesta: '{response_text}'"
            )
        except Exception as e_log:
            print(f"[ANALYTICS ERROR] Falló el registro automático en chat: {e_log}")
        
        # 5. Generar Audio TTS
        tmp_dir = os.path.join(ROOT_DIR, "tmp")
        os.makedirs(tmp_dir, exist_ok=True)
        output_path = os.path.join(tmp_dir, f"output_text_{user_id}.wav")
        audio_engine.text_to_speech(response_text, output_path)
        
        # Codificamos el texto para mandarlo como header de control
        encoded_text = urllib.parse.quote(response_text)
        headers = {"Access-Control-Expose-Headers": "X-Response-Text", "X-Response-Text": encoded_text}
        
        return FileResponse(path=output_path, media_type="audio/wav", filename="response.wav", headers=headers)
    except Exception as e:
        print(f"Error interno RAG: {e}")
        raise HTTPException(status_code=500, detail="Error de procesamiento de Chat")

@router.post("/chat_audio")
async def chat_audio(
    audio_file: UploadFile = File(...), 
    user_id: int = Depends(get_current_user_id),
    project_id: str = Header("default", alias="X-Project-ID")
):
    if not audio_engine:
        raise HTTPException(status_code=500, detail="Audio service not available")
    try:
        # 1. Guardar temporalmente el WAV subido por Unity
        tmp_dir = os.path.join(ROOT_DIR, "tmp")
        os.makedirs(tmp_dir, exist_ok=True)
        input_path = os.path.join(tmp_dir, f"input_{user_id}.wav")
        with open(input_path, "wb") as f:
            f.write(await audio_file.read())
            
        # 2. Transcripción de Audio a Texto (STT)
        user_text = audio_engine.speech_to_text(input_path)
        print(f"\n🗣️ [STT LOG] Proyecto '{project_id}' envió un audio. Transcripción: '{user_text}'\n", flush=True)
        if not user_text.strip():
            user_text = "No te he entendido. Por favor, repítelo con claridad."
            
        # 3. Consultar historial semántico de conversación
        history_text = memory_repo.get_recent_history(user_id)
        
        # 4. Obtener respuesta del RAG aislado del proyecto
        response_text = rag_engine.chat(user_text, history_text, project_id=project_id)
        
        # 5. Guardar en historial de chat SQLite
        memory_repo.add_message(user_id, "user", user_text)
        memory_repo.add_message(user_id, "assistant", response_text)
        
        # 6. Registrar de forma automática la analítica para retroalimentación
        try:
            analytics_repo.log_activity(
                user_id=user_id,
                project_id=project_id,
                activity_type="chat_question",
                details=f"Pregunta Voz: '{user_text}' | Respuesta: '{response_text}'"
            )
        except Exception as e_log:
            print(f"[ANALYTICS ERROR] Falló el registro de audio: {e_log}")
        
        # 7. Convertir respuesta a audio (TTS)
        output_path = os.path.join(tmp_dir, f"output_{user_id}.wav")
        audio_engine.text_to_speech(response_text, output_path)
        
        # Codificar texto para el header de control en Unity
        encoded_text = urllib.parse.quote(response_text)
        headers = {"Access-Control-Expose-Headers": "X-Response-Text", "X-Response-Text": encoded_text}
        
        return FileResponse(path=output_path, media_type="audio/wav", filename="response.wav", headers=headers)
    except Exception as e:
        print(f"Error interno RAG Audio: {e}")
        raise HTTPException(status_code=500, detail="Error de procesamiento de Audio Chat")
