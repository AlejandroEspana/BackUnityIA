from pydantic import BaseModel

class SaveRequest(BaseModel):
    save_data_base64: str

class SaveResponse(BaseModel):
    save_data_base64: str
    message: str = "Success"
