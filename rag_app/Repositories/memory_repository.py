from DB.sqlite_client import get_db_connection

class MemoryRepository:
    def add_message(self, user_id: int, role: str, message: str):
        with get_db_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(
                "INSERT INTO history (user_id, role, message) VALUES (?, ?, ?)",
                (user_id, role, message)
            )
            conn.commit()

    def get_recent_history(self, user_id: int, limit: int = 6) -> str:
        with get_db_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(
                "SELECT role, message FROM history WHERE user_id = ? ORDER BY id DESC LIMIT ?",
                (user_id, limit)
            )
            rows = cursor.fetchall()
            
        if not rows:
            return "No hay contexto previo."
            
        rows.reverse()
        context_lines = []
        for role, msg in rows:
            name = "Jugador" if role == "user" else "Tú"
            context_lines.append(f"{name}: {msg}")
            
        return "\\n".join(context_lines)
