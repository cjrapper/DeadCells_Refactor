using DeadCells.Core;
using UnityEngine;

namespace DeadCells.Player
{
    public class PlayerWallet:MonoBehaviour
    {
        public int Coins{get;private set ;}

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            Coins +=amount;
            EventCenter.Instance?.Broadcast(EventCenter.EventType.CoinChange.ToString(), Coins);
        }

        public bool TrySpend(int amount)
        {
            if(Coins<amount)return false;
            Coins -= amount;
            EventCenter.Instance?.Broadcast(EventCenter.EventType.CoinChange.ToString(), Coins);
            return true;
        }
    }
}