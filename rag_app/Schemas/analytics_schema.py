from pydantic import BaseModel

class AnalyticsLogRequest(BaseModel):
    activity_type: str  # e.g., 'vr_action', 'quiz_result', 'incorrect_procedure', 'system_event'
    details: str        # e.g., 'Activated lever 1 too early', 'Score: 8/10'
