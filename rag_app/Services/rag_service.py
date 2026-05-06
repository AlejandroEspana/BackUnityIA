import os
import shutil
from langchain_community.document_loaders import DirectoryLoader, TextLoader
from langchain_text_splitters import RecursiveCharacterTextSplitter
from langchain_chroma import Chroma
from langchain_huggingface import HuggingFaceEmbeddings
from langchain_ollama import OllamaLLM as Ollama
from Core.config import PERSIST_DIR, DOCS_DIR, OLLAMA_URL, MODEL_NAME, BACKEND_PERSONALITY

class RAGService:
    def __init__(self):
        self.vectorstore = None
        self.embeddings = HuggingFaceEmbeddings(model_name="sentence-transformers/all-MiniLM-L6-v2")
        self.llm = Ollama(base_url=OLLAMA_URL, model=MODEL_NAME)
        self.mtime_file = os.path.join(PERSIST_DIR, "last_mtime.txt")

    def _get_latest_mtime(self) -> float:
        if not os.path.exists(DOCS_DIR):
            return 0.0
        latest_mtime = 0.0
        for root, _, files in os.walk(DOCS_DIR):
            for file in files:
                if file.endswith(".txt"):
                    file_path = os.path.join(root, file)
                    mtime = os.path.getmtime(file_path)
                    if mtime > latest_mtime:
                        latest_mtime = mtime
        return latest_mtime

    def _is_cache_valid(self, current_mtime: float) -> bool:
        if not os.path.exists(self.mtime_file):
            return False
        with open(self.mtime_file, "r") as f:
            try:
                saved_mtime = float(f.read().strip())
                return current_mtime <= saved_mtime
            except ValueError:
                return False

    def _save_mtime(self, mtime: float):
        os.makedirs(PERSIST_DIR, exist_ok=True)
        with open(self.mtime_file, "w") as f:
            f.write(str(mtime))

    def load_or_rebuild(self, force_rebuild=False):
        current_mtime = self._get_latest_mtime()
        
        if not force_rebuild and self._is_cache_valid(current_mtime):
            print("Cargando VectorStore desde disco (caché sincronizada)...")
            self.vectorstore = Chroma(persist_directory=PERSIST_DIR, embedding_function=self.embeddings)
            return

        print("Detectados cambios en los documentos o primera inicialización...")
        if os.path.exists(PERSIST_DIR):
            for item in os.listdir(PERSIST_DIR):
                item_path = os.path.join(PERSIST_DIR, item)
                try:
                    if os.path.isfile(item_path):
                        os.unlink(item_path)
                    elif os.path.isdir(item_path):
                        shutil.rmtree(item_path)
                except Exception as e:
                    print(f"Error borrando caché secundaria: {e}")
        
        if not os.path.exists(DOCS_DIR):
            print(f"Directorio {DOCS_DIR} no encontrado. RAG quedará vacío.")
            return

        loader = DirectoryLoader(DOCS_DIR, glob="**/*.txt", loader_cls=TextLoader, use_multithreading=True)
        docs = loader.load()
        if not docs:
            print("No se encontraron documentos en /documentos")
            return

        text_splitter = RecursiveCharacterTextSplitter(chunk_size=1000, chunk_overlap=100)
        splits = text_splitter.split_documents(docs)

        self.vectorstore = Chroma.from_documents(documents=splits, embedding=self.embeddings, persist_directory=PERSIST_DIR)
        self._save_mtime(current_mtime)

    def chat(self, user_message: str, chat_history: str = "No hay contexto previo.") -> str:
        current_mtime = self._get_latest_mtime()
        if not self._is_cache_valid(current_mtime):
            self.load_or_rebuild(force_rebuild=True)

        if self.vectorstore is None:
            return "no tengo informacion sobre la pregunta que me hiciste"
            
        docs = self.vectorstore.similarity_search(user_message, k=2)
        context = "\n".join([doc.page_content for doc in docs])
        
        prompt = f"""{BACKEND_PERSONALITY}
        
        Historial de tu conversación reciente con este usuario (solo como referencia):
        {chat_history}
        
        Responde a la pregunta basándote ESTRICTAMENTE en el siguiente contexto. 
        Si la información no está explícitamente en el contexto a continuación, debes responder EXACTAMENTE: "no tengo informacion sobre la pregunta que me hiciste" y nada más.
        
        Contexto documental:
        {context}
        
        Pregunta actual: {user_message}
        Respuesta Asistente:"""
        
        response = self.llm.invoke(prompt)
        return response.strip()
