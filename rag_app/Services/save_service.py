import base64
from fastapi import HTTPException
from Repositories.save_repository import SaveRepository
from Schemas.save_schema import SaveRequest, SaveResponse

class SaveService:
    def __init__(self):
        self.save_repo = SaveRepository()

    def save_game(self, user_id: int, request: SaveRequest) -> dict:
        try:
            # Decodificar de Base64 a bytes
            binary_data = base64.b64decode(request.save_data_base64)
            self.save_repo.upsert_save(user_id, binary_data)
            return {"message": "Partida guardada exitosamente"}
        except Exception as e:
            raise HTTPException(status_code=400, detail=f"Error al guardar: {str(e)}")

    def load_game(self, user_id: int) -> SaveResponse:
        try:
            binary_data = self.save_repo.get_save(user_id)
            if not binary_data:
                raise HTTPException(status_code=404, detail="No se encontró partida guardada para este usuario")
            
            # Codificar de bytes a Base64
            base64_str = base64.b64encode(binary_data).decode('utf-8')
            return SaveResponse(save_data_base64=base64_str, message="Partida cargada exitosamente")
        except HTTPException:
            raise
        except Exception as e:
            raise HTTPException(status_code=500, detail=f"Error al cargar: {str(e)}")
