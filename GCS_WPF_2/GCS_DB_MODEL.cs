using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCS_WPF_2
{
    class GCS_DB_MODEL
    {
        public int ID;
        public string alt, yaw, pitch, roll, lat, lng, time;

        //public GCS_DB_MODEL()
        //{
        //    this.ID1 = 0;
        //    this.Alt = "";
        //    this.Yaw = "";
        //    this.Pitch = "";
        //    this.Roll = "";
        //    this.Lat = "";
        //    this.Lng = "";
        //}

        //public GCS_DB_MODEL(string alt, string yaw, string pitch, string roll, string lat, string lng)
        //{
        //    this.Alt = alt;
        //    this.Yaw = yaw;
        //    this.Pitch = pitch;
        //    this.Roll = roll;
        //    this.Lat = lat;
        //    this.Lng = lng;
        //}

        public int ID1
        {
            get
            {
                return ID;
            }

            set
            {
                ID = value;
            }
        }

        public string Alt
        {
            get
            {
                return alt;
            }

            set
            {
                alt = value;
            }
        }

        public string Yaw
        {
            get
            {
                return yaw;
            }

            set
            {
                yaw = value;
            }
        }

        public string Pitch
        {
            get
            {
                return pitch;
            }

            set
            {
                pitch = value;
            }
        }

        public string Roll
        {
            get
            {
                return roll;
            }

            set
            {
                roll = value;
            }
        }

        public string Lat
        {
            get
            {
                return lat;
            }

            set
            {
                lat = value;
            }
        }

        public string Lng
        {
            get
            {
                return lng;
            }

            set
            {
                lng = value;
            }
        }

        public string Time
        {
            get
            {
                return time;
            }

            set
            {
                time = value;
            }
        }
    }
}
