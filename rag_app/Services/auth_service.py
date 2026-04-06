from Repositories.auth_repository import AuthRepository
from Core.security import create_access_token, verify_password, get_password_hash

auth_repo = AuthRepository()

def authenticate_user(username: str, password: str):
    """Verifica el hash criptográfico en BD y retorna token de sesión activo acoplado a la ID real."""
    user = auth_repo.get_user_by_username(username)
    if not user:
        return None
    
    if verify_password(password, user["password_hash"]):
        return create_access_token(user["id"])
    return None

def register_new_user(username: str, password: str) -> dict:
    """Hashea una contraseña nueva segura y registra al jugador en SQLite local."""
    existing = auth_repo.get_user_by_username(username)
    if existing:
        return {"error": "El nombre de usuario ya está en uso. ¡Intenta con otro!"}
    
    hashed_pwd = get_password_hash(password)
    user_id = auth_repo.create_user(username, hashed_pwd)
    
    return {"id": user_id, "username": username}
