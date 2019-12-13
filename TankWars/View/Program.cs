using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TankWars
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            GameController c = new GameController();
            TankWars tankWars = new TankWars(c);
            //Do process updates for server here method for controller
            Application.Run(tankWars);
        }
    }
}
