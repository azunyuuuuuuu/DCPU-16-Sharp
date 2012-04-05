using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using DCPU16Sharp;

namespace TestApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            var cpu = new DCPU16();

            var temp = File.ReadAllBytes("ramdump.bin");
            var newtemp = new ushort[0x10000];

            for (int i = 0; i < temp.Length; i++)
            {
                if (i % 2 == 0)
                {
                    newtemp[i / 2] |= (ushort)(temp[i] << 8);
                }
                else
                {
                    newtemp[i / 2] |= (ushort)(temp[i]);
                }
            }

            cpu.SetMemory(newtemp);
            cpu.Start();
        }
    }
}
