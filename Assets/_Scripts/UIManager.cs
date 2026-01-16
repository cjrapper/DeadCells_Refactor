using System.Collections;
using UnityEngine.UI;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    [Header("Health Bar")]
    public Image healthBarFill;

    void Awake()
    {
        if(instance == null) instance = this;
        else Destroy(gameObject);
    }
    public void UpadteHealthBar(int current,int max)
    {
        if(healthBarFill != null)
        {
            float ratio = (float)current / max;
            healthBarFill.fillAmount = ratio;
        }
    }

}
