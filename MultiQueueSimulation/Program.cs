using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using MultiQueueTesting;
using MultiQueueModels;
using System.IO;

namespace MultiQueueSimulation
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            SimulationSystem system = new SimulationSystem();
            //Random random = new Random();
            ReadFromFiles readFromFiles = new ReadFromFiles();
            string file = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory().ToString()).ToString()) + "\\TestCases\\TestCase1.txt";

            readFromFiles.loadfortest(system, file);
            readFromFiles.SimulationMain(system);

            string result = TestingManager.Test(system, Constants.FileNames.TestCase1);
            MessageBox.Show(result);
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());
            for (int i = 0; i < system.Servers.Count; i++)
            {
                Console.WriteLine($"prob of idle server :{system.Servers[i].IdleProbability} ");
                Console.WriteLine($"pavr service timer : { system.Servers[i].AverageServiceTime}");
                Console.WriteLine($"utilization : { system.Servers[i].Utilization}");
                Console.WriteLine("======================================================");


            }


        }
        

    }
}
