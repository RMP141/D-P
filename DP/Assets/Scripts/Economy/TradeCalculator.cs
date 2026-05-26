namespace ConvoyManager.Economy
{
    /// <summary>
    /// Статические методы для расчёта прибыли и стоимости сделок.
    /// </summary>
    public static class TradeCalculator
    {
        public static float CalculateProfit(float buyPrice, float sellPrice, int quantity)
        {
            return (sellPrice - buyPrice) * quantity;
        }

        public static float CalculateCost(float unitPrice, int quantity)
        {
            return unitPrice * quantity;
        }
    }
}