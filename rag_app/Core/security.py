import hmac
import hashlib
import time
import os
import bcrypt
from fastapi import HTTPException, Header

# Llave secreta para firmar tokens. Puede ser provista por entorno o usar un valor seguro por defecto.
SECRET_KEY = os.getenv("SECRET_KEY", "b4ck_un1ty_1a_s3cr3t_k3y_9955").encode('utf-8')
TOKEN_EXPIRATION_SECONDS = 7 * 24 * 3600  # Token válido por 7 días

# Mantener para evitar errores de importación en otros módulos, pero sin uso activo
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
    """Genera un token firmado criptográficamente (Stateless) que contiene el user_id y la marca de tiempo."""
    timestamp = int(time.time())
    payload = f"{user_id}:{timestamp}"
    signature = hmac.new(SECRET_KEY, payload.encode('utf-8'), hashlib.sha256).hexdigest()
    return f"{payload}:{signature}"

def verify_token(token: str) -> int:
    """Valida la firma y expiración del token. Retorna el user_id si es válido, o None en caso contrario."""
    try:
        parts = token.split(":")
        if len(parts) != 3:
            return None
        
        user_id_str, timestamp_str, signature = parts
        payload = f"{user_id_str}:{timestamp_str}"
        
        # Validar firma
        expected_signature = hmac.new(SECRET_KEY, payload.encode('utf-8'), hashlib.sha256).hexdigest()
        if not hmac.compare_digest(expected_signature, signature):
            return None
        
        # Validar expiración
        timestamp = int(timestamp_str)
        if time.time() - timestamp > TOKEN_EXPIRATION_SECONDS:
            return None
            
        return int(user_id_str)
    except Exception:
        return None

def get_current_user_id(authorization: str = Header(None)) -> int:
    """Extrae, verifica y devuelve el user_id indexado (Token Bearer Parser)."""
    if not authorization:
        raise HTTPException(status_code=401, detail="Header de autorización no proporcionado")
    
    clean_token = authorization.replace("Bearer ", "").strip()
    user_id = verify_token(clean_token)
    if user_id is None:
        raise HTTPException(status_code=401, detail="Sesión inválida o expirada")
    return user_id
