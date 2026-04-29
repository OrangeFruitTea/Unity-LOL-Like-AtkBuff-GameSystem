namespace Gameplay.Shop
{
    /// <summary>与经济子系统隔离的占位接口（局内金币扣费）。 </summary>
    public interface ICurrencyWallet
    {
        bool TrySpend(int goldAmount);

        bool TryRefund(int goldAmount);

        /// <summary> 当前可用余额（占位；联机时需权威快照）。 </summary>
        long CurrentBalanceSnapshot { get; }
    }

    /// <summary> 调试/单机用：不设上限。 </summary>
    public sealed class DebugCurrencyWallet : ICurrencyWallet
    {
        private long _balance;

        public DebugCurrencyWallet(long initialGold)
        {
            _balance = initialGold;
        }

        public long CurrentBalanceSnapshot => _balance;

        public bool TryRefund(int goldAmount)
        {
            if (goldAmount < 0)
                return false;
            checked
            {
                _balance += goldAmount;
            }

            return true;
        }

        public bool TrySpend(int goldAmount)
        {
            if (goldAmount < 0 || _balance < goldAmount)
                return false;
            checked
            {
                _balance -= goldAmount;
            }

            return true;
        }
    }
}
