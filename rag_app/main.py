from contextlib import asynccontextmanager
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from Api.routes import auth_routes, chat_routes, save_routes, analytics_routes

@asynccontextmanager
async def lifespan(app: FastAPI):
    # Despiértase e inicializa a Chroma desde Base RAG Service.
    # Usamos el proyecto por defecto 'default' al iniciar
    chat_routes.rag_engine.load_or_rebuild("default")
    yield

app = FastAPI(title="RAG for Unity API (Clean Arch)", version="3.0", lifespan=lifespan)

# Restricciones Cross-Origin (optimizadas para WebGL de Unity en navegadores)
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=False,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Adherencia total de Rutas Modularizadas y enrutadores
app.include_router(auth_routes.router, tags=["Authentication"])
app.include_router(chat_routes.router, tags=["Chat e Inferencia"])
app.include_router(save_routes.router, tags=["Save System"])
app.include_router(analytics_routes.router, prefix="/analytics", tags=["Student Analytics"])
