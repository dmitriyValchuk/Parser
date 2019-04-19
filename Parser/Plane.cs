using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser
{
    public class Plane
    {
        public string Name { get; set; }
        public string Price { get; set; }
        public string Currency { get; set; }
        public string Year { get; set; }
        public string Location { get; set; }
        public string SerialNumber { get; set; }
        public string Registration { get; set; }
        public string TotlaTimeAirFrame { get; set; }

        public string Discription { get; set; }
        public List<Specification> specifications { get; set; }

        public Seller Seller { get; set; }
    }
}
