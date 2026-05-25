from DB.sqlite_client import get_db_connection

class SaveRepository:
    def upsert_save(self, user_id: int, save_data: bytes):
        with get_db_connection() as conn:
            cursor = conn.cursor()
            cursor.execute('''
                INSERT INTO saves (user_id, save_data, timestamp)
                VALUES (?, ?, CURRENT_TIMESTAMP)
                ON CONFLICT(user_id) DO UPDATE SET 
                save_data=excluded.save_data, timestamp=CURRENT_TIMESTAMP
            ''', (user_id, save_data))
            conn.commit()

    def get_save(self, user_id: int) -> bytes:
        with get_db_connection() as conn:
            cursor = conn.cursor()
            cursor.execute('SELECT save_data FROM saves WHERE user_id = ?', (user_id,))
            row = cursor.fetchone()
            if row:
                return row[0]
            return None
