using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace DCPU16Sharp
{
    public class DCPU16
    {
        /*
         * DCPU-16 Version 1.1 Reference Implementation
         * Documentation http://0x10c.com/doc/dcpu-16.txt
         */

        //++ Constructor and Destructor
        public DCPU16()
        {
            Paused = true;
        }

        //+ - helper methods
        public void Start()
        {
            Paused = false;

            // infinite loop
            while (true)
            {
                if (!Paused) tick();
                Thread.Sleep(1); // simulate CPU at 1kHz
            }
        }

        public void Stop()
        {
            Paused = true;
        }

        public void Pause()
        {
            Paused = !Paused; // invert pause as in a start stop thingy
        }

        public void SetMemory(ushort[] array)
        {
            if (array.Length > 0x10000)
                return;

            array.CopyTo(ram, 0);
        }

        //+ - public properties
        public bool Paused { get; set; }

        //+ - Registers and RAM
        #region Registers and RAM
        private ushort[] ram = new ushort[0x10000];  /* 64KB of ram */

        private ushort a = 0x0000;                  /* register A */
        private ushort b = 0x0000;                  /* register B */
        private ushort c = 0x0000;                  /* register C */
        private ushort x = 0x0000;                  /* register X */
        private ushort y = 0x0000;                  /* register Y */
        private ushort z = 0x0000;                  /* register Z */
        private ushort i = 0x0000;                  /* register I */
        private ushort j = 0x0000;                  /* register J */

        private ushort sp = 0x0000;                 /* stack pointer */
        private ushort pc = 0x0000;                 /* program counter */
        private ushort o = 0x0000;                  /* overflow */
        #endregion

        //+ - "physical" properties of the CPU
        #region "physical" properties of the CPU
        private double cpuclock = 1 / 1000000;      /* time a tick needs at least to execute, default 1Mhz */
        private ushort opcode = 0x0000;
        private byte opcodestep = 0x00;
        private bool skipinstruction;

        private ushort addressA = 0x0000;
        private ushort addressB = 0x0000;
        #endregion

        //+ - simulate tick
        private void tick()
        {
            // fetch new opcode
            opcode = ram[pc++];

            // read the rest of the instruction ahead of time
            int valuea = ReadA();
            int valueb = ReadB();
            int temp = 0;

            // check if we had a jump etc.
            if (skipinstruction)
            {
                skipinstruction = false;
                return;
            }

            // parse opcode
            switch (opcode & 0x000F)
            {
                case 0x0000: /* non-basic instruction */
                    // valueb contains opcode
                    switch ((opcode >> 4) & 0x3F)
                    {
                        case 0x01: /* JSR a - pushes the address of the next instruction to the stack, then sets PC to a */
                            Push(pc++);
                            pc = (ushort)valueb;
                            break;
                        default:
                            break;
                    }
                    break;

                case 0x0001: /* SET a, b - sets a to b */
                    WriteA(valueb); break;

                case 0x0002: /* ADD a, b - sets a to a+b, sets O to 0x0001 if there's an overflow, 0x0 otherwise */
                    temp = valuea + valueb;
                    o = ((temp >> 16) > 0) ? (ushort)0x0001 : (ushort)0x0000;
                    WriteA(temp);
                    break;
                case 0x0003: /* SUB a, b - sets a to a-b, sets O to 0xffff if there's an underflow, 0x0 otherwise */
                    temp = valuea - valueb;
                    o = (temp < 0) ? (ushort)0xFFFF : (ushort)0x0000;
                    WriteA(temp);
                    break;
                case 0x0004: /* MUL a, b - sets a to a*b, sets O to ((a*b)>>16)&0xffff */
                    temp = valuea * valueb;
                    o = (ushort)((temp >> 16) & 0xFFFF);
                    WriteA(temp);
                    break;
                case 0x0005: /* DIV a, b - sets a to a/b, sets O to ((a<<16)/b)&0xffff. if b==0, sets a and O to 0 instead. */
                    if (valueb == 0) { WriteA(0); o = 0; }
                    else
                        WriteA(valuea / valueb);
                    break;
                case 0x0006: /* MOD a, b - sets a to a%b. if b==0, sets a to 0 instead. */
                    WriteA(valueb == 0 ? 0x0000 : valuea % valueb);
                    break;

                case 0x0007: /* SHL a, b - sets a to a<<b, sets O to ((a<<b)>>16)&0xffff */
                    WriteA(valuea << valueb);
                    o = (ushort)(((valuea << valueb) >> 16) & 0xFFFF);
                    break;
                case 0x0008: /* SHR a, b - sets a to a>>b, sets O to ((a<<16)>>b)&0xffff */
                    WriteA(valuea >> valueb);
                    o = (ushort)(((valuea << 16) >> valueb) & 0xFFFF);
                    break;

                case 0x0009: /* AND a, b - sets a to a&b */
                    WriteA(valuea & valueb);
                    break;
                case 0x000A: /* BOR a, b - sets a to a|b */
                    WriteA(valuea | valueb);
                    break;
                case 0x000B: /* XOR a, b - sets a to a^b */
                    WriteA(valuea ^ valueb);
                    break;

                case 0x000C: /* IFE a, b - performs next instruction only if a==b */
                    if (valuea == valueb) ;
                    else
                        skipinstruction = true;
                    break;
                case 0x000D: /* IFN a, b - performs next instruction only if a!=b */
                    if (valuea != valueb) ;
                    else
                        skipinstruction = true;
                    break;
                case 0x000E: /* IFG a, b - performs next instruction only if a>b */
                    if (valuea > valueb) ;
                    else
                        skipinstruction = true;
                    break;
                case 0x000F: /* IFB a, b - performs next instruction only if (a&b)!=0 */
                    if ((valuea & valueb) != 0) ;
                    else
                        skipinstruction = true;
                    break;
            }
        }

        //+ - Memory Operations
        #region Memory Operations
        private ushort ReadA(ushort address = 0x0000)
        {
            return ReadMemory(address, false);
        }

        private ushort ReadB(ushort address = 0x0000)
        {
            return ReadMemory(address, true);
        }

        private ushort ReadMemory(ushort address = 0x0000, bool toB = false)
        {
            // select which part we want to parse
            byte temp = toB ? (byte)((opcode >> 10) & 0x3F) : (byte)((opcode >> 4) & 0x3F);

            switch (temp)
            {
                /* register */
                case 0x00: return a; // a
                case 0x01: return b; // b
                case 0x02: return c; // c
                case 0x03: return x; // x
                case 0x04: return y; // y
                case 0x05: return z; // z
                case 0x06: return i; // i
                case 0x07: return j; // j

                /* register pointer */
                case 0x08: return ram[a]; // a
                case 0x09: return ram[b]; // b
                case 0x0A: return ram[c]; // c
                case 0x0B: return ram[x]; // x
                case 0x0C: return ram[y]; // y
                case 0x0D: return ram[z]; // z
                case 0x0E: return ram[i]; // i
                case 0x0F: return ram[j]; // j

                /* next word + register */
                case 0x10: if (toB) addressB = pc++; else addressA = pc++; return ram[(ushort)((ram[toB ? addressB : addressA] + a) & 0xFFFF)];
                case 0x11: if (toB) addressB = pc++; else addressA = pc++; return ram[(ushort)((ram[toB ? addressB : addressA] + b) & 0xFFFF)];
                case 0x12: if (toB) addressB = pc++; else addressA = pc++; return ram[(ushort)((ram[toB ? addressB : addressA] + c) & 0xFFFF)];
                case 0x13: if (toB) addressB = pc++; else addressA = pc++; return ram[(ushort)((ram[toB ? addressB : addressA] + x) & 0xFFFF)];
                case 0x14: if (toB) addressB = pc++; else addressA = pc++; return ram[(ushort)((ram[toB ? addressB : addressA] + y) & 0xFFFF)];
                case 0x15: if (toB) addressB = pc++; else addressA = pc++; return ram[(ushort)((ram[toB ? addressB : addressA] + z) & 0xFFFF)];
                case 0x16: if (toB) addressB = pc++; else addressA = pc++; return ram[(ushort)((ram[toB ? addressB : addressA] + i) & 0xFFFF)];
                case 0x17: if (toB) addressB = pc++; else addressA = pc++; return ram[(ushort)((ram[toB ? addressB : addressA] + j) & 0xFFFF)];

                /* stack */
                case 0x18: return ram[sp++]; // pop
                case 0x19: return ram[sp]; // peek
                case 0x1A: return ram[--sp]; // push (does not make much sense in writing to the stack but documents do not restrict this)
                case 0x1B: return sp; // stack pointer count

                /* runtime */
                case 0x1C: return pc; // program counter
                case 0x1D: return o; // overflow

                /* in program code values */
                case 0x1E: if (toB) addressB = pc++; else addressA = pc++; return ram[ram[toB ? addressB : addressA]]; // next word pointer
                case 0x1F: if (toB) addressB = pc++; else addressA = pc++; return ram[toB ? addressB : addressA]; // next word

                default:
                    return (ushort)(temp - 0x20);
            }
        }

        private void WriteA(int value)
        {
            WriteA((ushort)(value & 0xFFFF));
        }

        private void WriteA(ushort value)
        {
            byte temp = (byte)((opcode >> 4) & 0x3F);

            switch (temp) // bitmask for 1111 1100 0000 0000
            {
                /* register */
                case 0x00: a = value; break; // a
                case 0x01: b = value; break; // b
                case 0x02: c = value; break; // c
                case 0x03: x = value; break; // x
                case 0x04: y = value; break; // y
                case 0x05: z = value; break; // z
                case 0x06: i = value; break; // i
                case 0x07: j = value; break; // j

                /* register pointer */
                case 0x08: ram[a] = value; break; // a
                case 0x09: ram[b] = value; break; // b
                case 0x0A: ram[c] = value; break; // c
                case 0x0B: ram[x] = value; break; // x
                case 0x0C: ram[y] = value; break; // y
                case 0x0D: ram[z] = value; break; // z
                case 0x0E: ram[i] = value; break; // i
                case 0x0F: ram[j] = value; break; // j

                /* next word + register */
                case 0x10: ram[(ram[addressA] + a) & 0xFFFF] = value; break;
                case 0x11: ram[(ram[addressA] + b) & 0xFFFF] = value; break;
                case 0x12: ram[(ram[addressA] + c) & 0xFFFF] = value; break;
                case 0x13: ram[(ram[addressA] + x) & 0xFFFF] = value; break;
                case 0x14: ram[(ram[addressA] + y) & 0xFFFF] = value; break;
                case 0x15: ram[(ram[addressA] + z) & 0xFFFF] = value; break;
                case 0x16: ram[(ram[addressA] + i) & 0xFFFF] = value; break;
                case 0x17: ram[(ram[addressA] + j) & 0xFFFF] = value; break;

                /* stack */
                case 0x18: ram[sp++] = value; break; // pop (does not make much sense in writing to the stack this way but documents do not restrict this)
                case 0x19: ram[sp] = value; break; // peek
                case 0x1A: ram[--sp] = value; break; // push 
                case 0x1B: sp = value; break; // stack pointer count

                /* runtime */
                case 0x1C: pc = value; break; // program counter
                case 0x1D: o = value; break; // overflow

                /* in program code values */
                case 0x1E: ram[ram[addressA]] = value; break; // next word pointer
                case 0x1F: ram[addressA] = value; break; // next word
            }
        }


        private void Push(ushort value)
        {
            ram[--sp] = value;
        }

        private ushort Peek()
        {
            return ram[sp];
        }

        private ushort Pop()
        {
            return ram[sp++];
        }
        #endregion
    }
}
