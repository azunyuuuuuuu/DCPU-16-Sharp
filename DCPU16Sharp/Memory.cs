using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCPU16Sharp
{
    public class Memory
    {
        private ushort[] ram = new ushort[0x10000];
        private object[] ramlock = new object[0x10000];
        private object ramgloballock = new object();

        public Memory()
        {
            ramlock = ramlock.AsParallel().Select(x => new object()).ToArray();
        }

        public ushort this[ushort address]
        {
            get { lock (ramgloballock) lock (ramlock[address]) return ram[address]; }
            set { lock (ramgloballock) lock (ramlock[address]) ram[address] = value; }
        }

        public ushort this[int address]
        {
            get { if (address >= ram.Length) return 0; lock (ramlock) lock (ramlock[address]) return ram[address]; }
            set { if (address >= ram.Length) return; lock (ramlock) lock (ramlock[address]) ram[address] = value; }
        }

        public ushort[] Screen
        {
            get
            {
                var temp = new ushort[36 * 14];
                lock (ramlock) Array.Copy(ram, 0xE000, temp, 0, temp.Length);
                return temp;
            }
        }

        public void WriteImage(ushort[] image)
        {
            lock (ramlock)
            {
                ram = new ushort[0x10000];
                Array.Copy(image, ram, image.Length);
            }
        }
    }
}
