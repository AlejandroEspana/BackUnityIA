using UnityEngine;
using UnityEngine.UI;
using System.Text;
using TMPro;

public class VoiceManager : MonoBehaviour
{
    public ChatManager chatManager;
    public TMP_Text chatOutputText;

    [Header("--- UI de Voz (Push to Talk) ---")]
    public Button pushToTalkButton;
    public bool useSpacebarToTalk = true;
    
    private AudioClip recordingClip;
    private bool isRecording = false;
    private string microphoneDevice;

    private void Update()
    {
        if (ApiManager.Instance.IsAuthenticated())
        {
            bool isTriggered = false;
            
            // 1. Detección en PC (Barra espaciadora para depuración)
            if (useSpacebarToTalk)
            {
                if (Input.GetKeyDown(KeyCode.Space)) StartRecording();
                if (Input.GetKeyUp(KeyCode.Space)) StopRecordingAndSend();
                
                if (Input.GetKey(KeyCode.Space)) isTriggered = true;
            }
            
            // 2. Detección en VR Mandos (Botón A del mando derecho / Mano Derecha)
            if (!isTriggered)
            {
                bool primaryButtonPressed = false;
                var rightHandDevices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
                UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.RightHand, rightHandDevices);
                
                if (rightHandDevices.Count > 0)
                {
                    UnityEngine.XR.InputDevice device = rightHandDevices[0];
                    device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out primaryButtonPressed);
                }
                
                if (primaryButtonPressed)
                {
                    if (!isRecording)
                    {
                        StartRecording();
                    }
                }
                else
                {
                    // Solo detener si estaba grabando y el espacio de PC tampoco está pulsado
                    if (isRecording && !Input.GetKey(KeyCode.Space))
                    {
                        StopRecordingAndSend();
                    }
                }
            }
        }
    }

    public void StartRecording()
    {
        if (Microphone.devices.Length == 0) {
            Debug.LogError("No hay micrófono detectado.");
            return;
        }
        microphoneDevice = Microphone.devices[0];
        recordingClip = Microphone.Start(microphoneDevice, false, 15, 44100);
        isRecording = true;
        if (chatOutputText) chatOutputText.text = "Escuchando...🎙️ (suelta para enviar)";
    }

    public void StopRecordingAndSend()
    {
        if (!isRecording) return;
        
        // Obtener la posición de la última muestra grabada antes de detener el micrófono
        int lastSamplePos = Microphone.GetPosition(microphoneDevice);
        Microphone.End(microphoneDevice);
        isRecording = false;
        
        if (chatOutputText) chatOutputText.text = "Procesando la voz con la IA en local... ⏳";
        
        // Evitar enviar audios vacíos o extremadamente cortos
        if (lastSamplePos <= 0)
        {
            if (chatOutputText) chatOutputText.text = "Grabación demasiado corta o inválida.";
            return;
        }
        
        // Codificar únicamente la sección de audio efectivamente grabada
        byte[] wavBytes = EncodeToWAV(recordingClip, lastSamplePos);
        if (chatManager != null)
        {
            chatManager.SendAudio(wavBytes);
        }
        else
        {
            Debug.LogError("ChatManager no asignado en VoiceManager.");
        }
    }

    private byte[] EncodeToWAV(AudioClip clip, int maxSamples = -1)
    {
        int sampleCount = clip.samples;
        // Si se especifica un número máximo de muestras, recortamos
        if (maxSamples > 0 && maxSamples < sampleCount)
        {
            sampleCount = maxSamples;
        }

        float[] samples = new float[sampleCount * clip.channels];
        clip.GetData(samples, 0);

        using (var memoryStream = new System.IO.MemoryStream())
        using (var writer = new System.IO.BinaryWriter(memoryStream))
        {
            writer.Write(Encoding.UTF8.GetBytes("RIFF"));
            writer.Write(36 + samples.Length * 2);
            writer.Write(Encoding.UTF8.GetBytes("WAVE"));
            writer.Write(Encoding.UTF8.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1); // Formato PCM
            writer.Write((short)clip.channels);
            writer.Write(clip.frequency);
            writer.Write(clip.frequency * clip.channels * 2);
            writer.Write((short)(clip.channels * 2));
            writer.Write((short)16); // Bits por muestra
            writer.Write(Encoding.UTF8.GetBytes("data"));
            writer.Write(samples.Length * 2);

            foreach (var sample in samples)
            {
                float s = sample;
                if (s > 1f) s = 1f;
                if (s < -1f) s = -1f;
                short intSample = (short)(s * 32767f);
                writer.Write(intSample);
            }

            return memoryStream.ToArray();
        }
    }
}
