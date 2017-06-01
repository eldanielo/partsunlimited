using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PartsUnlimited.Models
{
    public class Merchant
    {
        public int MerchantId { get; set; }
        public string Name { get; set; }
        public bool IsCertified { get; set; }
        public string Adress { get; set; }
        public string CertLevel { get; set; }
        public string Homepage { get; set; }
    }
}
