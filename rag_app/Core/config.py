import os

OLLAMA_URL = os.getenv("OLLAMA_URL", "http://host.docker.internal:11434")
MODEL_NAME = os.getenv("MODEL_NAME", "llama3")

# Calcular directorios dinámicamente para garantizar portabilidad (Windows, Mac, Docker)
CORE_DIR = os.path.dirname(os.path.abspath(__file__))
RAG_APP_DIR = os.path.dirname(CORE_DIR)
ROOT_DIR = os.path.dirname(RAG_APP_DIR)

PERSIST_DIR = os.getenv("PERSIST_DIR", os.path.join(ROOT_DIR, "chroma_db"))
SQLITE_DIR = os.getenv("SQLITE_DIR", os.path.join(ROOT_DIR, "sqlite_db"))
DOCS_DIR = os.getenv("DOCS_DIR", os.path.join(ROOT_DIR, "documentos"))

