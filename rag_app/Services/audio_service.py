import os
import torch
import soundfile as sf
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
        """Convierte texto a audio WAV usando Silero TTS y lo guarda en output_path."""
        audio = self.tts_model.apply_tts(
            text=text,
            speaker=self.speaker,
            sample_rate=self.sample_rate
        )
        sf.write(output_path, audio.numpy(), self.sample_rate)
