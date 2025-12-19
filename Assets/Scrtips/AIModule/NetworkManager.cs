using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
[System.Serializable]
public class ChatRequest
{
    public string message;
}

[System.Serializable]
public class ChatResponse
{
    public string reply;
}
[System.Serializable]
public class SoundRequest
{
    public string voice;
    public string language;
    public string text;
}
[System.Serializable]
public class SoundResponse
{
    public string soundWAV;
}
public class NetworkManager : MonoBehaviour
{


    private AudioSource audioSource;
    const int SampleRate = 44100;   //  API 오디오 샘플레이트와 반드시 일치
    const int Channels = 1;

    private ConcurrentQueue<float> audioQueue = new ConcurrentQueue<float>();
    float dcOffset = 0f;
    const float DC_ALPHA = 0.995f;

    [SerializeField, TextArea]
    string DialogueBox;

    [ContextMenu("Send!")]
    public async void Request()
    {
        audioSource = GetComponent<AudioSource>();

        string reply = await SendChat(DialogueBox);
        Debug.Log(reply);
        string soundStr = await ConvertVoice(DialogueBox);
        EnqueueBase64Wav(soundStr);

        AudioClip clip = AudioClip.Create(
            "TTS_Stream",
            SampleRate,   // buffer length (1초)
            Channels,
            SampleRate,
            true,
            OnAudioRead
        );

        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
    }
    // 🔹 서버에서 Base64 WAV 문자열 받을 때 호출
    public void EnqueueBase64Wav(string base64Wav)
    {
        byte[] wavBytes = Convert.FromBase64String(base64Wav);

        const int WAV_HEADER = 44; // PCM16 고정
        int pcmLen = wavBytes.Length - WAV_HEADER;
        if (pcmLen <= 0) return;

        for (int i = WAV_HEADER; i < wavBytes.Length; i += 2)
        {
            // Little Endian 강제
            short sample = (short)(wavBytes[i] | (wavBytes[i + 1] << 8));

            // int16 → float
            float f = sample / 32767f;

            // DC Offset 제거 (지직 제거 핵심)
            f -= dcOffset;
            dcOffset = f + DC_ALPHA * dcOffset;

            // 클리핑 방지
            audioQueue.Enqueue(f * 0.8f);
        }

    }

    // 🔹 Unity 오디오 스레드
    void OnAudioRead(float[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            if (audioQueue.TryDequeue(out float sample))
                data[i] = sample;
            else
                data[i] = 0f; // 언더런 방지
        }
    }
    private async UniTask<string> ConvertVoice(string msg)
    {
        var req = new SoundRequest { 
            text = msg,
            language = "korean",
            voice = "멀더"
        };
        string json = JsonUtility.ToJson(req);

        var request = new UnityWebRequest(
            "localhost:8000/api/voice", "POST"
        );

        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        await request.SendWebRequest();

        Debug.Log(request.downloadHandler.text);

        var res = JsonUtility.FromJson<SoundResponse>(
            request.downloadHandler.text
        );

        return res.soundWAV;
    }
    private async UniTask<string> SendChat(string msg)
    {
        var req = new ChatRequest { message = msg };
        string json = JsonUtility.ToJson(req);

        var request = new UnityWebRequest(
            "localhost:8000/api/chat", "POST"
        );

        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        await request.SendWebRequest();

        Debug.Log(request.downloadHandler.text);

        var res = JsonUtility.FromJson<ChatResponse>(
            request.downloadHandler.text
        );

        return res.reply;
    }


}
