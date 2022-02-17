using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using IniParser;
using IniParser.Model;
using System.IO;
using System.Reflection;
using System.Dynamic;
using System.Management;
using Microsoft.Win32;
using System.ServiceProcess;
using Microsoft.VisualBasic.Devices;

namespace CpuUsageTool
{
    class Program
    {
        static ServiceController[] scServices = ServiceController.GetServices();
         
        static void Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.Green;    
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Clear();
            
            Console.WriteLine("---------CPU usage tool-------------developed by Bogdan");
            Console.WriteLine("Available ram in MB: " + new ComputerInfo().AvailablePhysicalMemory/1024/1024);
            Console.WriteLine("Total ram in MB: " + new ComputerInfo().TotalPhysicalMemory / 1024 / 1024);                           
            Console.WriteLine("");
            Console.WriteLine("Operation System Information");
            Console.WriteLine("----------------------------");         
            Console.WriteLine("OS name "+ new ComputerInfo().OSFullName);
            Console.WriteLine("OS platform " + new ComputerInfo().OSPlatform);
            Console.WriteLine("OS version " + new ComputerInfo().OSVersion);            
            Console.WriteLine("");
            Console.WriteLine("");

            string numeprocess = "";
            string numeserviciu = "";
            string m_BaseDir = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string strWorkPath = System.IO.Path.GetDirectoryName(m_BaseDir);
            string strSettingsiniFilePath = System.IO.Path.Combine(strWorkPath, "configCpu.ini");
            int procentMin = 0;
            int procentMax = 0;
            int intervalverificare = 0;
            int minut = 0;
            bool istrue = true;
            var timer = new Stopwatch();

            try
            {

                FileIniDataParser i = new FileIniDataParser();
                i.Parser.Configuration.CommentString = "#";
                IniData data = i.ReadFile(strSettingsiniFilePath);
                numeserviciu = data["CpuUsage"]["serviciu"];
                numeprocess = data["CpuUsage"]["proces"];
                procentMin = Convert.ToInt32(data["CpuUsage"]["Minim"]);
                procentMax = Convert.ToInt32(data["CpuUsage"]["Maxim"]);
                intervalverificare = Convert.ToInt32(data["CpuUsage"]["Interval"]);
                minut = Convert.ToInt32(data["CpuUsage"]["durata"]);
                istrue = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("The ini file not found");
                Console.WriteLine("");
                Console.WriteLine(""); 
                istrue = false;
            }


            Console.WriteLine("------------------Cpu usage in percentange------------for "+numeprocess);
 
            while (istrue)
            {
                double rez = getCpuUsage(numeprocess.Substring(0, numeprocess.IndexOf(".")));
                Console.WriteLine("cpu level " + rez + " %");

                Thread.Sleep(intervalverificare * 1000);

                if (rez >= procentMin)
                {
                    timer.Start();
                    TimeSpan timeTaken = timer.Elapsed;
                    if (timeTaken.TotalMinutes > minut)
                    {
                        OpresteServiciu(numeserviciu, 1000);
                        Console.WriteLine("");
                        Thread.Sleep(20000);
                        foreach (ServiceController scTemp in scServices)
                        {
                            if (scTemp.DisplayName == numeserviciu) {
                                if (scTemp.Status == ServiceControllerStatus.Running)
                                {
                                    killProces(numeserviciu, numeprocess);
                                    timer.Stop();
                                    timer.Reset();
                                    Console.WriteLine("");
                                    Console.WriteLine("___________________");
                                    Console.WriteLine("the  : " + numeprocess + " has been closed and it will be started");
                                    //Console.ForegroundColor = ConsoleColor.DarkYellow;
                                    Thread.Sleep(10000);
                                    Console.WriteLine("");
                                    StartServiciu(numeserviciu, 1000);
                                    Console.WriteLine("");
                                }
                                else
                                {
                                    Console.WriteLine("");
                                    StartServiciu(numeserviciu, 1000);
                                    Console.WriteLine("");
                                    timer.Stop();
                                    timer.Reset();
                                }
                            }
                        }
                    }                  
                } 
                else
                    {
                        timer.Stop();
                        timer.Reset();
                    }
            }

            Console.WriteLine("Press any key to close the CPU usage tool...");
            Console.ReadKey();
            
        }
        public static void OpresteServiciu(string serviceName, int timeoutMilliseconds)
        {
            bool stopat = true;
            Console.WriteLine("service stopping...");
            ServiceController service = new ServiceController(serviceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
                foreach (ServiceController scTemp in scServices)
                {
                    if (scTemp.DisplayName == serviceName)
                    {
                        scTemp.Stop();
                        while (scTemp.Status != ServiceControllerStatus.Stopped)
                        {
                            Thread.Sleep(1000);
                            scTemp.Refresh();
                        }

                        Console.WriteLine("service stopped");                     
                        //Console.ForegroundColor = ConsoleColor.DarkYellow;
                        scTemp.Refresh();
                        stopat = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("error service stopped,service not found");
                //Console.ForegroundColor = ConsoleColor.Red;                
                stopat = false;
            }

        }
        public static void RestartServiciu(string serviceName, int timeoutMilliseconds)
        {
            bool restartat = true;
            Console.WriteLine("service restarting...");
            ServiceController service = new ServiceController(serviceName);
            try
            {
                int millisec1 = Environment.TickCount;
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
                foreach (ServiceController scTemp in scServices)
                {
                    if (scTemp.DisplayName == serviceName)
                    {
                        scTemp.Stop();
                        scTemp.WaitForStatus(ServiceControllerStatus.Stopped, timeout);

                        // count the rest of the timeout
                        int millisec2 = Environment.TickCount;
                        timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds - (millisec2 - millisec1));

                        scTemp.Start();
                        scTemp.WaitForStatus(ServiceControllerStatus.Running, timeout);
                        restartat = true;
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                    }
                }
            }
            catch
            {
                Console.WriteLine("error service restarted,service not found");
                Console.ForegroundColor = ConsoleColor.Red;
                restartat = false;
            }

        }
        public static void StartServiciu(string serviceName, int timeoutMilliseconds)
        {
            bool startedd = true;
            Console.WriteLine("service starting...");
            ServiceController service = new ServiceController(serviceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds*2);
                foreach (ServiceController scTemp in scServices)
                {
                    if (scTemp.DisplayName == serviceName)
                    {
                        scTemp.Start();
                        while (scTemp.Status != ServiceControllerStatus.Running)
                        {
                            Thread.Sleep(1000);
                            scTemp.Refresh();
                        }
                        
                        Console.WriteLine("service started");
                        //Console.ForegroundColor = ConsoleColor.DarkYellow;                  
                        scTemp.Refresh();
                        startedd = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("error service started,service not found");
                //Console.ForegroundColor = ConsoleColor.Red;             
                startedd = false;
            }

        }
        public static void killProces(string service1, string proces1)
        {
            Console.WriteLine("the proces for the service " + service1 + " can not be closed");
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Process");

                foreach (ServiceController scTemp in scServices)
                {
                    if (scTemp.DisplayName == service1)
                    {
                        if (scTemp.Status == ServiceControllerStatus.Running)
                        {

                            foreach (ManagementObject queryObj in searcher.Get())
                            {
                                string namef = queryObj["Name"].ToString();
                                if (namef == proces1)
                                {
                                    object[] obj = new object[] { 0 };
                                    queryObj.InvokeMethod("Terminate", obj);
                                }
                            }

                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("the process "+proces1+" not found for killing");
                Console.ForegroundColor = ConsoleColor.Red;
                 
            }
            
        }
        public static void startProces(string service1, string proces1)
        {
           
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Process");

                foreach (ServiceController scTemp in scServices)
                {
                    if (scTemp.DisplayName == service1)
                    {
                        if (scTemp.Status == ServiceControllerStatus.Stopped)
                        {

                            foreach (ManagementObject queryObj in searcher.Get())
                            {
                                string namef = queryObj["Name"].ToString();
                                if (namef == proces1)
                                {
                                    object[] obj = new object[] { 0 };
                                    queryObj.InvokeMethod("Start", obj);
                                }
                            }

                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("the process " + proces1 + " not found for killing");
                Console.ForegroundColor = ConsoleColor.Red;
            }

        }
        public static double getCpuUsage(string proces1)
        {
            double rez = 0;
            try
            {
                ManagementObjectSearcher searcher1 = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PerfFormattedData_PerfProc_Process where Name='" + proces1 + "'");
                foreach (ManagementObject obj in searcher1.Get())
                {
                    //Console.WriteLine(obj["Name"]);
                    ulong d1 = (ulong)obj["PercentProcessorTime"];
                    ulong a = (ulong)DateTime.UtcNow.Millisecond;
                    Thread.Sleep(1000);
                    ulong b = (ulong)DateTime.UtcNow.Millisecond;
                    ulong d2 = (ulong)obj["PercentUserTime"];

                    rez = (int)d1/Environment.ProcessorCount-2;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("the process " + proces1 + " not found");
                Console.ForegroundColor = ConsoleColor.Red;
            }
            return rez;
        }
  
    }
}

 
