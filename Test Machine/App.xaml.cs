using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using DCPU16Sharp;

namespace Test_Machine
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static DCPU16 CPU = new DCPU16();

        public App()
        {
            InitializeComponent();
        }
    }
}
