import sqlite3
import os
from Core.config import PERSIST_DIR

def get_db_connection():
    os.makedirs(PERSIST_DIR, exist_ok=True)
    db_path = os.path.join(PERSIST_DIR, "rag_database.db")
    # Forzamos chequeo de Foreign Keys para mantener Relaciones ACID
    conn = sqlite3.connect(db_path)
    conn.execute("PRAGMA foreign_keys = ON")
    return conn

def init_db():
    with get_db_connection() as conn:
        cursor = conn.cursor()
        
        # Opcional: Para migrar limpiamente en desarrollo
        # cursor.execute('DROP TABLE IF EXISTS history')
        # cursor.execute('DROP TABLE IF EXISTS users')

        # Tabla de usuarios central
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT UNIQUE NOT NULL,
                password_hash TEXT NOT NULL,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP
            )
        ''')

        # Nueva tabla indexada por llave foránea a user_id nativo en vez del string username
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER NOT NULL,
                role TEXT NOT NULL,
                message TEXT NOT NULL,
                timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
            )
        ''')
        conn.commit()

init_db()
