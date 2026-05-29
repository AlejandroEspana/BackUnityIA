from DB.sqlite_client import get_db_connection

class AnalyticsRepository:
    def log_activity(self, user_id: int, project_id: str, activity_type: str, details: str) -> int:
        """
        Inserta un nuevo registro de analítica de un estudiante.
        """
        with get_db_connection() as conn:
            cursor = conn.cursor()
            cursor.execute('''
                INSERT INTO student_analytics (user_id, project_id, activity_type, details)
                VALUES (?, ?, ?, ?)
            ''', (user_id, project_id, activity_type, details))
            conn.commit()
            return cursor.lastrowid

    def get_activities_by_user(self, user_id: int, limit: int = 100):
        """
        Recupera las últimas actividades registradas por un estudiante.
        """
        with get_db_connection() as conn:
            cursor = conn.cursor()
            cursor.execute('''
                SELECT id, project_id, activity_type, details, timestamp 
                FROM student_analytics 
                WHERE user_id = ? 
                ORDER BY id DESC LIMIT ?
            ''', (user_id, limit))
            rows = cursor.fetchall()
            
        return [
            {
                "id": row[0],
                "project_id": row[1],
                "activity_type": row[2],
                "details": row[3],
                "timestamp": row[4]
            } for row in rows
        ]

    def get_activities_by_project(self, project_id: str, limit: int = 100):
        """
        Recupera las analíticas registradas para un proyecto específico (para dashboards de profesores).
        """
        with get_db_connection() as conn:
            cursor = conn.cursor()
            cursor.execute('''
                SELECT id, user_id, activity_type, details, timestamp 
                FROM student_analytics 
                WHERE project_id = ? 
                ORDER BY id DESC LIMIT ?
            ''', (project_id, limit))
            rows = cursor.fetchall()
            
        return [
            {
                "id": row[0],
                "user_id": row[1],
                "activity_type": row[2],
                "details": row[3],
                "timestamp": row[4]
            } for row in rows
        ]
