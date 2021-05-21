using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Fourier_Plotter
{
    [Serializable()]

    public class pair
    {
        [XmlElementAttribute("radius")] public int A { get; set; }
        [XmlElementAttribute("frequency")] public double B { get; set; }
        public pair()       //needed along with/for canUserAddRows=true" in Datagrid as described in https://stackoverflow.com/a/42922322
        {
        }
        public pair(int x, double y)
        {
            A = x;
            B = y;
        }
        public int firstVal()
        {
            return A;
        }
        public double secondVal()
        {
            return B;
        }
    }
}
