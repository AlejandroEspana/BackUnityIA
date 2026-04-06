from fastapi import APIRouter, HTTPException
from Schemas.auth_schema import LoginRequest, RegisterRequest, TokenResponse
from Services.auth_service import authenticate_user, register_new_user

router = APIRouter()

@router.post("/login", response_model=TokenResponse)
def login(request: LoginRequest):
    """Endpoint de Login, delegando resolución de Hashes al Servicio."""
    token = authenticate_user(request.username, request.password)
    if not token:
        raise HTTPException(status_code=401, detail="Usuario o contraseña incorrectos")
    return TokenResponse(access_token=token)

@router.post("/register")
def register(request: RegisterRequest):
    """Permite el autoconsumo creando una cuenta vinculada a un UserID integer seguro."""
    result = register_new_user(request.username, request.password)
    if "error" in result:
        raise HTTPException(status_code=400, detail=result["error"])
    return {"message": "Registro completado con éxito.", "user_id": result["id"]}
