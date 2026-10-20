using DeadCells.Core;
using UnityEngine;

namespace DeadCells.Player
{
    public class PlayerWallet:MonoBehaviour
    {
        //the amount of coins the player has
        public int Coins{ get; private set; }=0;//read公开，write私有

        public void AddCoins(int amount)
        {
            if(amount<=0)return;
            Coins+=amount;
            //事件中心播放金币change
            EventCenter.Instance.Broadcast(EventCenter.EventType.CoinChange.ToString(),Coins);
        }
        public bool TrySpendCoins(int amount)
        {
            if(amount<=0||Coins<amount)return false;
            Coins-=amount;
            //事件中心播放金币change
            EventCenter.Instance.Broadcast(EventCenter.EventType.CoinChange.ToString(),Coins);
            return true;
        }
    }
}