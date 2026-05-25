import os

OLLAMA_URL = os.getenv("OLLAMA_URL", "http://host.docker.internal:11434")
MODEL_NAME = os.getenv("MODEL_NAME", "llama3")

PERSIST_DIR = "/app/chroma_db"
SQLITE_DIR = "/app/sqlite_db"
DOCS_DIR = "/documentos"
