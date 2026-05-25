import os
import torch
import soundfile as sf
import numpy as np
import re
from faster_whisper import WhisperModel

class AudioService:
    def __init__(self):
        print("Initializing STT model (faster-whisper)...")
        self.stt_model_size = "tiny"
        self.stt_model = WhisperModel(self.stt_model_size, device="cpu", compute_type="int8")
        
        print("Initializing TTS model (Silero)...")
        self.device = torch.device('cpu')
        self.language = 'es'
        self.model_id = 'v3_es'
        self.sample_rate = 48000
        self.speaker = 'es_0'
        
        # Download and cache locally so it doesn't redownload
        cache_dir = os.path.join(os.getcwd(), 'models_cache')
        os.makedirs(cache_dir, exist_ok=True)
        torch.hub.set_dir(cache_dir)
        
        self.tts_model, _ = torch.hub.load(
            repo_or_dir='snakers4/silero-models',
            model='silero_tts',
            language=self.language,
            speaker=self.model_id,
            trust_repo=True
        )
        self.tts_model.to(self.device)
        print("Audio models initialized successfully.")

    def speech_to_text(self, audio_file_path: str) -> str:
        """Convierte archivo de audio a texto usando faster-whisper."""
        segments, info = self.stt_model.transcribe(audio_file_path, beam_size=5)
        text = " ".join([segment.text for segment in segments])
        return text

    def text_to_speech(self, text: str, output_path: str):
        """Convierte texto a audio WAV usando Silero TTS y lo guarda en output_path. Soporta textos largos dividiéndolos en fragmentos."""
        # Limpiar texto (Silero a veces falla con ciertos caracteres especiales)
        text = text.replace('\n', ' ').replace('\r', '').replace('  ', ' ')
        
        chunks = []
        sentences = re.split(r'(?<=[.?!;]) +', text)
        current_chunk = ""
        
        for sentence in sentences:
            # Límite seguro para Silero es ~800 a 900 caracteres por llamada
            if len(current_chunk) + len(sentence) < 800:
                current_chunk += sentence + " "
            else:
                if current_chunk:
                    chunks.append(current_chunk.strip())
                
                # Si una sola oración es ridículamente larga (más de 800), la forzamos a cortarse
                if len(sentence) >= 800:
                    for i in range(0, len(sentence), 800):
                        chunks.append(sentence[i:i+800])
                    current_chunk = ""
                else:
                    current_chunk = sentence + " "
                    
        if current_chunk:
            chunks.append(current_chunk.strip())
            
        audio_segments = []
        for chunk in chunks:
            if not chunk.strip(): continue
            try:
                audio = self.tts_model.apply_tts(
                    text=chunk,
                    speaker=self.speaker,
                    sample_rate=self.sample_rate
                )
                audio_segments.append(audio.numpy())
            except Exception as e:
                print(f"Error generando audio para chunk '{chunk[:30]}...': {e}")
                
        if audio_segments:
            final_audio = np.concatenate(audio_segments)
            sf.write(output_path, final_audio, self.sample_rate)
        else:
            print("No se generó ningún audio.")
            sf.write(output_path, np.zeros(self.sample_rate), self.sample_rate)
