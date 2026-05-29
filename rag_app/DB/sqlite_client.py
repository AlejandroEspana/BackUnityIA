import sqlite3
import os
from Core.config import SQLITE_DIR

def get_db_connection():
    os.makedirs(SQLITE_DIR, exist_ok=True)
    db_path = os.path.join(SQLITE_DIR, "rag_database.db")
    # Forzamos chequeo de Foreign Keys para mantener Relaciones ACID
    # Establecemos timeout=10.0 para que las transacciones concurrentes esperen antes de fallar
    conn = sqlite3.connect(db_path, timeout=10.0)
    conn.execute("PRAGMA foreign_keys = ON")
    # Activamos WAL (Write-Ahead Logging) para permitir lecturas y escrituras concurrentes fluidas
    try:
        conn.execute("PRAGMA journal_mode=WAL")
    except Exception as e:
        pass  # Evitar fallos si la base de datos es de solo lectura en algún despliegue
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

        # Tabla para guardar el progreso (Save System)
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS saves (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER NOT NULL UNIQUE,
                save_data BLOB NOT NULL,
                timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
            )
        ''')

        # Tabla para registrar analíticas de estudiantes
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS student_analytics (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER NOT NULL,
                project_id TEXT NOT NULL,
                activity_type TEXT NOT NULL,
                details TEXT NOT NULL,
                timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
            )
        ''')
        conn.commit()

init_db()
