using Microsoft.Research.DynamicDataDisplay.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCS_WPF_2
{
    public class PointCollection : RingArray<Points>
    {
        private const int POIN_TOTAL = 300;

        public PointCollection()
            : base(POIN_TOTAL)
        { }
    }

    public class Points
    {
        public double Waktu { get; set; }

        public double Variabel { get; set; }

        public Points(double variabel, double waktu)
        {
            this.Waktu = waktu;
            this.Variabel = variabel;
        }
    }

    }
