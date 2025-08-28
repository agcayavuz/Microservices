using BasketService.Contracts.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasketService.Contracts.Requests
{
    public sealed class UpsertBasketItemsRequest
    {
        public List<BasketItemDto> Items { get; init; } = new();
    }
}
