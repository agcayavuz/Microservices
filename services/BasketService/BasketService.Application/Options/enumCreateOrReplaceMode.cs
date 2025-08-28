using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasketService.Application.Options
{
    public enum enumCreateOrReplaceMode
    {
        Merge = 0,   // ürün varsa miktarı artır, yoksa ekle
        Replace = 1  // gelen listeyle sepeti tamamen değiştir
    }
}
