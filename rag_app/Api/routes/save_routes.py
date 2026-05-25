from fastapi import APIRouter, Depends
from Services.save_service import SaveService
from Schemas.save_schema import SaveRequest, SaveResponse
from Core.security import get_current_user_id

router = APIRouter()
save_service = SaveService()

@router.post("/save", response_model=dict)
def save_game(request: SaveRequest, current_user_id: int = Depends(get_current_user_id)):
    return save_service.save_game(current_user_id, request)

@router.get("/save", response_model=SaveResponse)
def load_game(current_user_id: int = Depends(get_current_user_id)):
    return save_service.load_game(current_user_id)
