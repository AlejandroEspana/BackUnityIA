import os
import shutil
from langchain_community.document_loaders import DirectoryLoader, TextLoader
from langchain_text_splitters import RecursiveCharacterTextSplitter
from langchain_chroma import Chroma
from langchain_huggingface import HuggingFaceEmbeddings
from langchain_ollama import OllamaLLM as Ollama
from Core.config import PERSIST_DIR, DOCS_DIR, OLLAMA_URL, MODEL_NAME

class RAGService:
    def __init__(self):
        self.embeddings = HuggingFaceEmbeddings(model_name="sentence-transformers/all-MiniLM-L6-v2")
        # Establecemos temperature=0.0 para eliminar la creatividad y alucinación semántica en respuestas de RAG
        self.llm = Ollama(base_url=OLLAMA_URL, model=MODEL_NAME, temperature=0.0)

    def _get_latest_mtime(self, project_id: str) -> float:
        project_docs_dir = os.path.join(DOCS_DIR, project_id)
        # Retrocompatibilidad: si no existe la subcarpeta específica, usar la raíz
        if not os.path.exists(project_docs_dir):
            project_docs_dir = DOCS_DIR
            
        if not os.path.exists(project_docs_dir):
            return 0.0
            
        latest_mtime = 0.0
        for root, _, files in os.walk(project_docs_dir):
            for file in files:
                if file.endswith(".txt"):
                    file_path = os.path.join(root, file)
                    try:
                        mtime = os.path.getmtime(file_path)
                        if mtime > latest_mtime:
                            latest_mtime = mtime
                    except Exception:
                        pass
        return latest_mtime

    def _is_cache_valid(self, project_id: str, current_mtime: float) -> bool:
        mtime_file = os.path.join(PERSIST_DIR, f"last_mtime_{project_id}.txt")
        if not os.path.exists(mtime_file):
            return False
        with open(mtime_file, "r") as f:
            try:
                saved_mtime = float(f.read().strip())
                return current_mtime <= saved_mtime
            except ValueError:
                return False

    def _save_mtime(self, project_id: str, mtime: float):
        os.makedirs(PERSIST_DIR, exist_ok=True)
        mtime_file = os.path.join(PERSIST_DIR, f"last_mtime_{project_id}.txt")
        with open(mtime_file, "w") as f:
            f.write(str(mtime))

    def load_or_rebuild(self, project_id: str = "default", force_rebuild=False) -> Chroma:
        """
        Carga o reconstruye el vectorstore de forma dinámica y aislada
        usando colecciones independientes en Chroma DB para cada proyecto.
        """
        current_mtime = self._get_latest_mtime(project_id)
        collection_name = f"project_{project_id}"
        
        if not force_rebuild and self._is_cache_valid(project_id, current_mtime):
            print(f"[RAG] Cargando Chroma VectorStore para la colección: '{collection_name}' (caché sincronizada)")
            return Chroma(
                collection_name=collection_name, 
                persist_directory=PERSIST_DIR, 
                embedding_function=self.embeddings
            )

        print(f"[RAG] Reconstruyendo/Sincronizando VectorStore para la colección: '{collection_name}'")
        
        project_docs_dir = os.path.join(DOCS_DIR, project_id)
        if not os.path.exists(project_docs_dir):
            # Fallback a la raíz
            project_docs_dir = DOCS_DIR
            
        if not os.path.exists(project_docs_dir):
            print(f"[RAG] Directorio de documentos {project_docs_dir} no existe. Retornando None.")
            return None

        # Cargar documentos con encoding UTF-8 forzado para evitar UnicodeDecodeError en Windows
        loader = DirectoryLoader(
            project_docs_dir, 
            glob="**/*.txt", 
            loader_cls=TextLoader, 
            use_multithreading=True,
            loader_kwargs={"encoding": "utf-8"}
        )
        docs = loader.load()
        if not docs:
            print(f"[RAG] No se encontraron documentos en {project_docs_dir}")
            return None

        text_splitter = RecursiveCharacterTextSplitter(chunk_size=1000, chunk_overlap=100)
        splits = text_splitter.split_documents(docs)

        # Para reconstruir limpiamente eliminamos la colección específica de Chroma
        # Esto previene que se dupliquen fragmentos sin dañar colecciones de otros proyectos.
        try:
            client = Chroma(
                collection_name=collection_name,
                persist_directory=PERSIST_DIR,
                embedding_function=self.embeddings
            )
            client.delete_collection()
        except Exception:
            pass

        vectorstore = Chroma.from_documents(
            documents=splits, 
            embedding=self.embeddings, 
            persist_directory=PERSIST_DIR,
            collection_name=collection_name
        )
        self._save_mtime(project_id, current_mtime)
        print(f"[RAG] Sincronización de colección '{collection_name}' completada con éxito.")
        return vectorstore

    def chat(self, user_message: str, chat_history: str = "No hay contexto previo.", project_id: str = "default") -> str:
        """
        Consulta dinámicamente al RAG aislado del proyecto especificado.
        """
        current_mtime = self._get_latest_mtime(project_id)
        collection_name = f"project_{project_id}"
        
        if not self._is_cache_valid(project_id, current_mtime):
            vectorstore = self.load_or_rebuild(project_id, force_rebuild=True)
        else:
            vectorstore = Chroma(
                collection_name=collection_name,
                persist_directory=PERSIST_DIR,
                embedding_function=self.embeddings
            )

        if vectorstore is None:
            return "no tengo informacion sobre la pregunta que me hiciste"
            
        docs = vectorstore.similarity_search(user_message, k=2)
        if not docs:
            return "no tengo informacion sobre la pregunta que me hiciste"
            
        context = "\n".join([doc.page_content for doc in docs])
        
        prompt = f"""Instrucciones para el Asistente:
        Debes adoptar COMPLETAMENTE cualquier personalidad, rol o tono que se describa en el Contexto documental proporcionado a continuación.
        Si el contexto indica que eres un experto en algo, compórtate como tal en tu respuesta.
        
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

    def generate_quiz_feedback(self, question_text: str, options: list, correct_option: str, selected_option: str, category: str = "POO", difficulty: str = "Medio", project_id: str = "default") -> str:
        """
        Genera una explicación pedagógica basada en la metodología CUPI2 y POO a partir de un Quiz.
        """
        current_mtime = self._get_latest_mtime(project_id)
        collection_name = f"project_{project_id}"
        
        if not self._is_cache_valid(project_id, current_mtime):
            vectorstore = self.load_or_rebuild(project_id, force_rebuild=True)
        else:
            vectorstore = Chroma(
                collection_name=collection_name,
                persist_directory=PERSIST_DIR,
                embedding_function=self.embeddings
            )

        context = ""
        if vectorstore is not None:
            # Buscar contexto relacionado con la categoría y el enunciado de la pregunta
            docs = vectorstore.similarity_search(f"{category} {question_text}", k=3)
            if docs:
                context = "\n\n".join([doc.page_content for doc in docs])

        prompt = f"""Eres el tutor virtual de AlgoLab, un experto en programación orientada a objetos (POO) y en la metodología CUPI2.
Tu objetivo es dar una retroalimentación detallada y pedagógica sobre una pregunta de un Quiz.

Información de la Pregunta:
- Enunciado: {question_text}
- Opciones posibles: {", ".join(options)}
- Respuesta Correcta: {correct_option}
- Respuesta Seleccionada por el Estudiante: {selected_option}
- Tema/Categoría: {category}
- Dificultad: {difficulty}

Contexto de la Metodología y Conceptos del Proyecto:
{context}

Instrucciones para tu respuesta:
1. Comienza felicitando amablemente al estudiante si la respuesta seleccionada es igual a la respuesta correcta, o animándolo con empatía si es incorrecta.
2. Si el estudiante falló, explica conceptualmente por qué su opción elegida es incorrecta (basándote en el encapsulamiento, relaciones de clases, constructores, etc.). No des simplemente la solución del código directo; hazle entender el error teórico.
3. Justifica detalladamente por qué la respuesta correcta es la adecuada.
4. Explica los conceptos de POO y CUPI2 involucrados (como la separación entre el Mundo y la Interfaz, encapsulamiento, etc.) según el contexto documental provisto.
5. Da un consejo práctico corto y motivador para ayudarle a resolver problemas similares en el futuro.

Respuesta del Tutor (en español, clara y estructurada):"""

        response = self.llm.invoke(prompt)
        return response.strip()

