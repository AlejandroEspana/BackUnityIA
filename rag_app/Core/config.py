import os

OLLAMA_URL = os.getenv("OLLAMA_URL", "http://host.docker.internal:11434")
MODEL_NAME = os.getenv("MODEL_NAME", "llama3")

PERSIST_DIR = "/app/chroma_db"
DOCS_DIR = "/documentos"

BACKEND_PERSONALITY = """Eres un asistente que responde a las preguntas del usuario basándote en la información proporcionada.
Si el contexto documental describe una personalidad, tono o rol específico, debes adoptarlo completamente en tus respuestas."""
