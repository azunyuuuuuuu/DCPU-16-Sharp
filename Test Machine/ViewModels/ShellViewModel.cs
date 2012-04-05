using System.ComponentModel.Composition;
using Microsoft.Win32;
using System.IO;

namespace Test_Machine
{
    [Export(typeof(IShell))]
    public class ShellViewModel : IShell
    {
        private string screenoutput = 
            "************************************\n" +
            "*                                  *\n" +
            "* 36 x 14 Screen                   *\n" +
            "*                                  *\n" +
            "*                                  *\n" +
            "*                                  *\n" +
            "*                                  *\n" +
            "*                                  *\n" +
            "*                                  *\n" +
            "*                                  *\n" +
            "*                                  *\n" +
            "*                                  *\n" +
            "*                                  *\n" +
            "************************************";

        public string ScreenOutput { get { return screenoutput; } }

        public void StartCPU()
        {
            App.CPU.Start();
        }

        public void LoadImage()
        {
            var filedialog = new OpenFileDialog()
            {
                Multiselect = false
            };
            
            if (filedialog.ShowDialog() == true)
            {
                var temp = File.ReadAllBytes(filedialog.FileName);
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

                App.CPU.SetMemory(newtemp);
            }

        }
    }

}
