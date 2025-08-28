namespace BasketService.Domain.Entities
{

    public sealed class BasketItem
    {
        public string ProductId { get; }
        public string ProductName { get; }
        public decimal UnitPrice { get; }
        public int Quantity { get; private set; }

        public BasketItem(string productId, string productName, decimal unitPrice, int quantity)
        {
            if (string.IsNullOrWhiteSpace(productId)) throw new ArgumentException("ProductId boş olamaz");
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity > 0 olmalı");
            if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice), "UnitPrice negatif olamaz");

            ProductId = productId.Trim();
            ProductName = productName?.Trim() ?? string.Empty;
            UnitPrice = unitPrice;
            Quantity = quantity;
        }

        public void Increase(int qty)
        {
            if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
            Quantity += qty;
        }

        public void SetQuantity(int qty)
        {
            if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
            Quantity = qty;
        }



        public void Decrease(int qty)
        {
            if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty), "qty > 0 olmalı");
            if (qty > Quantity) throw new InvalidOperationException("Silinecek miktar mevcut olandan fazla olamaz.");
            Quantity -= qty;
        }

        public decimal LineTotal => UnitPrice * Quantity;
    }
}
