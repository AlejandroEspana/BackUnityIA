from fastapi import APIRouter, Depends, Header, HTTPException
from Schemas.analytics_schema import AnalyticsLogRequest
from Repositories.analytics_repository import AnalyticsRepository
from Core.security import get_current_user_id

router = APIRouter()
analytics_repo = AnalyticsRepository()

@router.post("/log", response_model=dict)
def log_student_activity(
    request: AnalyticsLogRequest,
    user_id: int = Depends(get_current_user_id),
    project_id: str = Header("default", alias="X-Project-ID")
):
    """
    Registra de forma dinámica una acción, interacción o evento de un estudiante dentro de un proyecto VR.
    """
    try:
        activity_id = analytics_repo.log_activity(
            user_id=user_id,
            project_id=project_id,
            activity_type=request.activity_type,
            details=request.details
        )
        return {"status": "success", "message": "Actividad registrada con éxito", "activity_id": activity_id}
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error al registrar analítica: {str(e)}")

@router.get("/my-logs", response_model=list)
def get_my_logs(
    user_id: int = Depends(get_current_user_id)
):
    """
    Devuelve los registros del estudiante actual (retroalimentación de lo que ha hecho).
    """
    try:
        return analytics_repo.get_activities_by_user(user_id)
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.get("/project-logs", response_model=list)
def get_project_logs(
    user_id: int = Depends(get_current_user_id),
    project_id: str = Header("default", alias="X-Project-ID")
):
    """
    Permite obtener todas las analíticas del proyecto actual (útil para profesores y supervisión).
    """
    try:
        return analytics_repo.get_activities_by_project(project_id)
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
