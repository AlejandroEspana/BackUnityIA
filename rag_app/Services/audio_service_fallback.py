import wave
import struct
import os

class FallbackAudioService:
    """
    Servicio de audio de respaldo (fallback) de extrema robustez.
    Se inicializa cuando el hardware de la máquina o las dependencias pesadas (PyTorch, Whisper)
    no están disponibles. Permite que el chat de texto siga funcionando generando WAVs de silencio.
    """
    def __init__(self):
        print("\n⚠️ [AUDIO SYSTEM WARNING] El motor de audio principal no pudo iniciarse (falta de hardware, CUDA o dependencias).")
        print("⚠️ [AUDIO SYSTEM WARNING] Iniciando FallbackAudioService de respaldo. El chat de texto de Unity funcionará correctamente.")
        self.sample_rate = 44100

    def speech_to_text(self, audio_file_path: str) -> str:
        """
        Devuelve un texto indicando que la entrada de voz no está soportada en el modo de respaldo.
        """
        print("[AUDIO SYSTEM FALLBACK] Recibida petición STT pero el motor de audio está en modo Fallback.")
        return "El reconocimiento de voz está deshabilitado en este servidor debido a limitaciones de hardware."

    def text_to_speech(self, text: str, output_path: str):
        """
        Genera un archivo WAV de silencio de corta duración (0.5 segundos) de manera nativa
        usando la biblioteca estándar de Python, evitando dependencias de PyTorch/numpy.
        """
        print(f"[AUDIO SYSTEM FALLBACK] Generando silencio WAV para la respuesta: '{text[:40]}...'")
        
        # Asegurar que el directorio de salida existe
        os.makedirs(os.path.dirname(output_path), exist_ok=True)
        
        # 0.5 segundos de duración para optimizar ancho de banda en fallback
        duration_seconds = 0.5
        num_samples = int(duration_seconds * self.sample_rate)
        
        try:
            with wave.open(output_path, 'wb') as wav_file:
                wav_file.setnchannels(1)       # Mono
                wav_file.setsampwidth(2)      # 16-bit PCM (2 bytes)
                wav_file.setframerate(self.sample_rate)
                
                # 0 en 16-bit signed PCM representa silencio absoluto
                silent_sample = struct.pack('<h', 0)
                
                # Escribir todas las muestras de silencio de una sola vez para máxima velocidad
                wav_file.writeframesraw(silent_sample * num_samples)
                
            print(f"[AUDIO SYSTEM FALLBACK] Archivo WAV de silencio generado exitosamente en {output_path}")
        except Exception as e:
            print(f"[AUDIO SYSTEM FALLBACK] Error crítico al escribir WAV de silencio: {e}")
