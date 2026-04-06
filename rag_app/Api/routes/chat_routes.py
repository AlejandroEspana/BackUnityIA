from fastapi import APIRouter, HTTPException, Depends
from Schemas.chat_schema import ChatRequest, ChatResponse
from Core.security import get_current_user_id
from Services.rag_service import RAGService
from Repositories.memory_repository import MemoryRepository

router = APIRouter()

rag_engine = RAGService()
memory_repo = MemoryRepository()

@router.post("/reload_docs", response_model=ChatResponse)
def reload_docs(user_id: int = Depends(get_current_user_id)):
    try:
        rag_engine.load_or_rebuild(force_rebuild=True)
        return ChatResponse(response="Documentos recargados explícitamente.")
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.post("/chat", response_model=ChatResponse)
def chat(request: ChatRequest, user_id: int = Depends(get_current_user_id)):
    try:
        history_text = memory_repo.get_recent_history(user_id)
        response_text = rag_engine.chat(request.message, history_text)
        
        # Insertando trazos acoplados explícitamente al ID del Usuario SQLite
        memory_repo.add_message(user_id, "user", request.message)
        memory_repo.add_message(user_id, "assistant", response_text)
        
        return ChatResponse(response=response_text)
    except Exception as e:
        print(f"Error interno RAG: {e}")
        raise HTTPException(status_code=500, detail="Error de procesamiento de Chat")
