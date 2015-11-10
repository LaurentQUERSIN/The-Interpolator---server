using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interpolator
{
    public class Player
    {
        public float x = 0;
        public float y = 0;
        public float z = 0;

        public float vx = 0;
        public float vy = 0;
        public float vz = 0;

        public float rx = 0;
        public float ry = 0;
        public float rz = 0;
        public float rw = 0;

        public string name;
        public long Id;

        public Player (string nm, long id)
        {
            name = nm;
            Id = id;
        }

        public void UpdatePosition(float nx, float ny, float nz, float nvx, float nvy, float nvz, float nrx, float nry, float nrz, float nrw)
        {
            vx = nvx/* / 1000*/;
            vy = nvy/* / 1000*/;
            vz = nvz/* / 1000*/;

            x = nx;
            y = ny;
            z = nz;

            rx = nrx;
            ry = nry;
            rz = nrz;
            rw = nrw;
        }
    }
}
