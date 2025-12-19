using Cysharp.Threading.Tasks;
using Runtime.Singleton;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoSingleton<GameManager>
{
    public async UniTask ChangeScene(string sceneName)
    {
        await SceneManager.LoadSceneAsync(sceneName)
    }
}
