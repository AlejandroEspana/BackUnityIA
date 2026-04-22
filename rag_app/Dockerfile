FROM python:3.10-slim

WORKDIR /app

# Instalar dependencias del sistema necesarias para compilar paquetes (ej. chromadb)
RUN apt-get update && apt-get install -y \
    build-essential \
    ffmpeg \
    && rm -rf /var/lib/apt/lists/*

# Copiar el archivo de dependencias primero para cachear esta capa y evitar recompilar en cada cambio de código
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copiar el codigo fuente
COPY . .

# Exponer explícitamente el puerto para facilitar el mapping en redes Docker personalizadas
EXPOSE 8000

# Comando de inicio del servidor FastAPI (usar 0.0.0.0 para escuchar en todas las interfaces)
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
