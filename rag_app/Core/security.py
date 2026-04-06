import secrets
import bcrypt
from fastapi import HTTPException, Header

# Cache for active sessions: { token: user_id_integer }
ACTIVE_SESSIONS = {}

def get_password_hash(password: str) -> str:
    """Genera un Salt aleatorio y encripta irreversiblemente la contraseña."""
    pwd_bytes = password.encode('utf-8')
    salt = bcrypt.gensalt()
    return bcrypt.hashpw(pwd_bytes, salt).decode('utf-8')

def verify_password(plain_password: str, hashed_password: str) -> bool:
    """Extrae el Salt del Hash y verifica si coincide con el texto plano."""
    pwd_bytes = plain_password.encode('utf-8')
    hash_bytes = hashed_password.encode('utf-8')
    return bcrypt.checkpw(pwd_bytes, hash_bytes)

def create_access_token(user_id: int) -> str:
    """Generates a secure random token and maps it to the integer user_id."""
    token = secrets.token_hex(32)
    ACTIVE_SESSIONS[token] = user_id
    return token

def get_current_user_id(authorization: str = Header(None)) -> int:
    """Extrae y devuelve el user_id indexado (Token Bearer Parser)."""
    if not authorization:
        raise HTTPException(status_code=401, detail="Header de autorización no proporcionado")
    
    clean_token = authorization.replace("Bearer ", "").strip()
    user_id = ACTIVE_SESSIONS.get(clean_token)
    if not user_id:
        raise HTTPException(status_code=401, detail="Sesión inválida o expirada")
    return user_id
