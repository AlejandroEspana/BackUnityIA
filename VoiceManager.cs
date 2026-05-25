using UnityEngine;
using UnityEngine.UI;
using System.Text;

public class VoiceManager : MonoBehaviour
{
    public ChatManager chatManager;
    public Text chatOutputText;

    [Header("--- UI de Voz (Push to Talk) ---")]
    public Button pushToTalkButton;
    public bool useSpacebarToTalk = true;
    
    private AudioClip recordingClip;
    private bool isRecording = false;
    private string microphoneDevice;

    private void Update()
    {
        if (useSpacebarToTalk && ApiManager.Instance.IsAuthenticated())
        {
            if (Input.GetKeyDown(KeyCode.Space)) StartRecording();
            if (Input.GetKeyUp(KeyCode.Space)) StopRecordingAndSend();
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
        Microphone.End(microphoneDevice);
        isRecording = false;
        if (chatOutputText) chatOutputText.text = "Procesando la voz con la IA en local... ⏳";
        
        byte[] wavBytes = EncodeToWAV(recordingClip);
        if (chatManager != null)
        {
            chatManager.SendAudio(wavBytes);
        }
        else
        {
            Debug.LogError("ChatManager no asignado en VoiceManager.");
        }
    }

    private byte[] EncodeToWAV(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
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
