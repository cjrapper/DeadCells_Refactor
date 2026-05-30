using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    [Header("Health Bar")]
    public Image healthBarFill;
    [Header("Game Over UI")]
    public GameObject gameOverPanel; // 请在Inspector中拖拽面板

    void Start(){
        if(EventCenter.Instance != null)
        {
            EventCenter.Instance.AddListener<int, int>(EventCenter.EventType.PlayerHealthChange.ToString(), UpdateHealthBar);
            EventCenter.Instance.AddListener(EventCenter.EventType.PlayerDead.ToString(), ShowGameOverPanel);
        }
        else
        {
            Debug.LogError("EventCenter.Instance is null! 请确保场景中有一个物体挂载了 EventCenter 脚本。");
        }
    }

    void Awake()
    {
        if(instance == null) instance = this;
        else Destroy(gameObject);
    }
    public void UpdateHealthBar(int current,int max)
    {
        if(healthBarFill != null)
        {
            float ratio = (float)current / max;
            healthBarFill.fillAmount = ratio;
        }
    }
    
    private void ShowGameOverPanel()
    {
        if(gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    // 重新加载当前场景
    public void RestartGame()
    {
        PlayerController.RestoreTimeScale();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnDestroy(){
        if(EventCenter.Instance != null)
        {
            EventCenter.Instance.RemoveListener<int, int>(EventCenter.EventType.PlayerHealthChange.ToString(), UpdateHealthBar);
            EventCenter.Instance.RemoveListener(EventCenter.EventType.PlayerDead.ToString(), ShowGameOverPanel);
        }
    }

}
