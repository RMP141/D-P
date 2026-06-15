namespace ConvoyManager.Combat
{
    public interface ICartManager
    {
        int CartCount { get; }
        int AvailableCarts { get; }
        bool BuyCart();
        bool UseCart();
        void ReturnCart();
    }
}
