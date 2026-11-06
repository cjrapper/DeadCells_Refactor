using UnityEngine;
using DeadCells.Core;
public class PlayerKeys:MonoBehaviour
{
    public int keys{ get; private set; }=0;
    public void AddKeys(int amount)
    {
        if(amount<=0)return;
        keys+=amount;
        EventCenter.Instance.Broadcast(EventCenter.EventType.KeyChange.ToString(),keys);
    }
    public bool TrySpendKeys(int amount)
    {
        if(amount<=0||keys<amount)return false;
        keys-=amount;
        EventCenter.Instance.Broadcast(EventCenter.EventType.KeyChange.ToString(),keys);
        return true;
    }
}