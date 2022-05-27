using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace SmileSSMSSendList
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new SMSWinService() //Shinee & ClickNext
                //new SMSSendListService() //ClickNext
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}