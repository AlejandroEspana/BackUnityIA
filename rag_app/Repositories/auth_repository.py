from DB.sqlite_client import get_db_connection

class AuthRepository:
    def get_user_by_username(self, username: str):
        """Busca en SQLite la ID indexada, nombre y el hash cifrado original del usuario."""
        with get_db_connection() as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT id, username, password_hash FROM users WHERE username = ?", (username,))
            row = cursor.fetchone()
            if row:
                return {"id": row[0], "username": row[1], "password_hash": row[2]}
            return None

    def create_user(self, username: str, password_hash: str) -> int:
        """Inyecta de forma persistente a un nuevo usuario cifrado y devuelve la Primary Key nativa asignada."""
        with get_db_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(
                "INSERT INTO users (username, password_hash) VALUES (?, ?)", 
                (username, password_hash)
            )
            conn.commit()
            return cursor.lastrowid
