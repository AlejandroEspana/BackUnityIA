from fastapi import APIRouter, HTTPException, Depends, UploadFile, File
from fastapi.responses import FileResponse
import urllib.parse
from Schemas.chat_schema import ChatRequest, ChatResponse
from Core.security import get_current_user_id
from Services.rag_service import RAGService
from Repositories.memory_repository import MemoryRepository
from Services.audio_service import AudioService
import os

router = APIRouter()

rag_engine = RAGService()
memory_repo = MemoryRepository()
try:
    audio_engine = AudioService()
except Exception as e:
    print("Failed to init AudioService", e)
    audio_engine = None

@router.post("/reload_docs", response_model=ChatResponse)
def reload_docs(user_id: int = Depends(get_current_user_id)):
    try:
        rag_engine.load_or_rebuild(force_rebuild=True)
        return ChatResponse(response="Documentos recargados explícitamente.")
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.post("/chat")
async def chat(request: ChatRequest, user_id: int = Depends(get_current_user_id)):
    if not audio_engine:
        raise HTTPException(status_code=500, detail="Audio service not available")
    try:
        history_text = memory_repo.get_recent_history(user_id)
        response_text = rag_engine.chat(request.message, history_text)
        
        memory_repo.add_message(user_id, "user", request.message)
        memory_repo.add_message(user_id, "assistant", response_text)
        
        # 3. TTS
        tmp_dir = "/app/tmp"
        os.makedirs(tmp_dir, exist_ok=True)
        output_path = os.path.join(tmp_dir, f"output_text_{user_id}.wav")
        audio_engine.text_to_speech(response_text, output_path)
        
        # Codificamos el texto para mandarlo como header
        encoded_text = urllib.parse.quote(response_text)
        headers = {"Access-Control-Expose-Headers": "X-Response-Text", "X-Response-Text": encoded_text}
        
        return FileResponse(path=output_path, media_type="audio/wav", filename="response.wav", headers=headers)
    except Exception as e:
        print(f"Error interno RAG: {e}")
        raise HTTPException(status_code=500, detail="Error de procesamiento de Chat")
@router.post("/chat_audio")
async def chat_audio(audio_file: UploadFile = File(...), user_id: int = Depends(get_current_user_id)):
    if not audio_engine:
        raise HTTPException(status_code=500, detail="Audio service not available")
    try:
        tmp_dir = "/app/tmp"
        os.makedirs(tmp_dir, exist_ok=True)
        input_path = os.path.join(tmp_dir, f"input_{user_id}.wav")
        with open(input_path, "wb") as f:
            f.write(await audio_file.read())
            
        user_text = audio_engine.speech_to_text(input_path)
        print(f"\n🗣️ [STT LOG] Unity envió un audio. Texto reconocido: '{user_text}'\n", flush=True)
        if not user_text.strip():
            user_text = "No te he entendido. Repítelo fuerte para el Dios Destructor."
            
        history_text = memory_repo.get_recent_history(user_id)
        response_text = rag_engine.chat(user_text, history_text)
        
        memory_repo.add_message(user_id, "user", user_text)
        memory_repo.add_message(user_id, "assistant", response_text)
        
        output_path = os.path.join(tmp_dir, f"output_{user_id}.wav")
        audio_engine.text_to_speech(response_text, output_path)
        
        encoded_text = urllib.parse.quote(response_text)
        headers = {"Access-Control-Expose-Headers": "X-Response-Text", "X-Response-Text": encoded_text}
        
        return FileResponse(path=output_path, media_type="audio/wav", filename="response.wav", headers=headers)
    except Exception as e:
        print(f"Error interno RAG Audio: {e}")
        raise HTTPException(status_code=500, detail="Error de procesamiento de Audio Chat")
