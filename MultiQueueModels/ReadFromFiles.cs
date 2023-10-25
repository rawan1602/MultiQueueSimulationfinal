using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace MultiQueueModels
{
    public class ReadFromFiles
    {

        static Random rnd = new Random();

        public void loadfortest(SimulationSystem sys, string path)
        {
            string[] fileContent = File.ReadAllLines(path);
            TimeDistribution timeDistribution;
            TimeDistribution servicetable;
            sys.NumberOfServers = int.Parse(fileContent[1]);
            sys.StoppingNumber = int.Parse(fileContent[4]);

            if (fileContent[7] == "1")
            {
                sys.StoppingCriteria = Enums.StoppingCriteria.NumberOfCustomers;
            }
            else if (fileContent[7] == "2")
            {
                sys.StoppingCriteria = Enums.StoppingCriteria.SimulationEndTime;
            }

            if (fileContent[10] == "1")
            {
                sys.SelectionMethod = Enums.SelectionMethod.HighestPriority;
            }
            else if (fileContent[10] == "2")
            {
                sys.SelectionMethod = Enums.SelectionMethod.LeastUtilization;
            }
            else
            {
                sys.SelectionMethod = Enums.SelectionMethod.Random;
            }

            /*---------------------------------------*/
            // calculate the interarrival-time distribution table
            int val = 13;
            int serversIndex = 0;

            while (val < fileContent.Length && fileContent[val] != "")
            {
                string[] parts = fileContent[val].Split(',');

                for (int i = 0; i < parts.Length; i += 2)
                {
                    timeDistribution = new TimeDistribution();
                    if (i + 1 < parts.Length)
                    {
                        timeDistribution.Time = int.Parse(parts[i]);
                        timeDistribution.Probability = decimal.Parse(parts[i + 1]);
                    }
                    sys.InterarrivalDistribution.Add(timeDistribution);
                }
                
                val++;
                serversIndex = val + 2;
                
            }
            cummprobability(sys.InterarrivalDistribution);
            /*---------------------------------------*/

            for (int i = 0; i < sys.NumberOfServers; i++)
            {
                Server server = new Server();

                server.ID = i + 1;
                server.TotalNoOfCustomers = new List<int>(); 
                while (serversIndex < fileContent.Length && fileContent[serversIndex] != "")
                {
                    servicetable = new TimeDistribution();

                    string[] parts = fileContent[serversIndex].Split(',');

                    servicetable.Time = int.Parse(parts[0]);
                    servicetable.Probability = decimal.Parse(parts[1]);

                    server.TimeDistribution.Add(servicetable);

                    serversIndex++;
                }
                serversIndex += 2;

                sys.Servers.Add(server);
            }

            //foreach (var item in sys.InterarrivalDistribution)
            //{
            //    Console.WriteLine($"Time: {item.Time}, Probability: {item.Probability}, Cummulative Probability: {item.CummProbability}");
            //}

            //Console.WriteLine("******************************************");
            foreach (var server in sys.Servers)
            {
                cummprobability(server.TimeDistribution);

                //for (int i = 0; i < server.TimeDistribution.Count; i++)
                //{
                //    Console.WriteLine($"Time:{server.TimeDistribution.ElementAt(i).Time}, Probability:{server.TimeDistribution.ElementAt(i).Probability}, Cummulative Probability: {server.TimeDistribution.ElementAt(i).CummProbability}");
                //}
                //Console.WriteLine("************************");

            }
        }

        public List<TimeDistribution> cummprobability(List<TimeDistribution> timeDistributions)
        {
            for (int i = 0; i < timeDistributions.Count; i++)
            {
                if (i == 0)
                {
                    timeDistributions[i].CummProbability = timeDistributions[i].Probability;
                    timeDistributions[i].MinRange = 0;
                    timeDistributions[i].MaxRange = (int)(timeDistributions[i].CummProbability * 100);
                }
                else
                {
                    timeDistributions[i].CummProbability = timeDistributions[i - 1].CummProbability + timeDistributions[i].Probability;
                    timeDistributions[i].MinRange = (int)(timeDistributions[i - 1].CummProbability * 100) + 1;
                    timeDistributions[i].MaxRange = (int)(timeDistributions[i].CummProbability * 100);
                }
            }
            return timeDistributions;
        }


        public void GetAssignedServer(SimulationCase Case, List<Server> Servers, Enums.SelectionMethod SelectionMethod)
        {
            ReadFromFiles readFromFiles = new ReadFromFiles();
            int serverNum = 0;
            int minServiceTime = Servers[0].FinishTime;
            int minBigger = 0;
            bool bigger = true;
            decimal minUtilization = Servers[0].Utilization;
            if (SelectionMethod == Enums.SelectionMethod.HighestPriority)
            {
                for (int i = 0; i < Servers.Count; i++)
                {
                    if (Servers[i].FinishTime <= Case.ArrivalTime)
                    {
                        bigger = false;
                    }
                    if (Servers[i].FinishTime < minServiceTime)
                    {
                        minBigger = i;
                        minServiceTime = Servers[i].FinishTime;
                    }
                }
                if (bigger == true)
                {
                    serverNum = minBigger;
                }
                else
                {
                    for (int i = 0; i < Servers.Count; i++)
                    {
                        if (Servers[i].FinishTime <= Case.ArrivalTime)
                        {
                            minServiceTime = Servers[i].FinishTime;
                            serverNum = i;
                            break;
                        }
                    }
                }
            }
            else
            {
                for (int i = 1; i < Servers.Count; i++)
                {
                    if (Servers[i].FinishTime < minServiceTime)
                    {
                        minServiceTime = Servers[i].FinishTime;
                        serverNum = i;
                    }
                }
            }
            Case.AssignedServer = Servers[serverNum];
            Case.RandomService = rnd.Next(1, 100);
            Case.ServiceTime = readFromFiles.RandomValues(Servers[serverNum].TimeDistribution, Case.RandomService);
            Case.StartTime = Math.Max(Servers[serverNum].FinishTime, Case.ArrivalTime);
            Case.EndTime = Case.StartTime + Case.ServiceTime;
            Case.TimeInQueue = Case.StartTime - Case.ArrivalTime;
            Servers[serverNum].FinishTime = Case.StartTime + Case.ServiceTime;
            Servers[serverNum].TotalWorkingTime += Case.ServiceTime;
            Servers[serverNum].AverageServiceTime += Case.ServiceTime;
            Servers[serverNum].ServedCount++;
        }


        public void SimulationMain(SimulationSystem System)
        {
            Queue<SimulationCase> Queue = new Queue<SimulationCase>();
            ReadFromFiles readFromFiles = new ReadFromFiles();

            int SimulationTime = 0;
            int Total_TimeinQueue = 0;
            int Number_Of_Customers_Who_Waited = 0;
            int Max_QueueLength = 0;
            List<SimulationCase> Cases = new List<SimulationCase>();
            if (System.StoppingCriteria == Enums.StoppingCriteria.NumberOfCustomers)
            {
                for (int i = 0; i < System.StoppingNumber; i++)
                {
                    SimulationCase c = new SimulationCase
                    {
                        CustomerNumber = i,
                        RandomInterArrival = rnd.Next(1, 100)
                    };
                    if (i == 0)
                    {
                        c.InterArrival = 0;
                        c.ArrivalTime = 0;
                    }
                    else
                    {
                        c.InterArrival = readFromFiles.RandomValues(System.InterarrivalDistribution, c.RandomInterArrival);
                        c.ArrivalTime = Cases[i - 1].ArrivalTime + c.InterArrival;
                    }
                    GetAssignedServer(c, System.Servers, System.SelectionMethod);
                    SimulationTime = Math.Max(SimulationTime, c.EndTime);
                    while (Queue.Count > 0)
                    {
                        if (Queue.Peek().StartTime <= c.ArrivalTime)
                        {
                            Queue.Dequeue();
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (c.TimeInQueue > 0)
                    {
                        Queue.Enqueue(c);
                        Total_TimeinQueue += c.TimeInQueue;
                        Number_Of_Customers_Who_Waited++;
                    }
                    Max_QueueLength = Math.Max(Max_QueueLength, Queue.Count);
                    Cases.Add(c);
                }
            }
            else
            {
                int Counter = 0;
                while (true)
                {
                    SimulationCase c = new SimulationCase
                    {
                        CustomerNumber = Counter,
                        RandomInterArrival = rnd.Next(1, 100)
                    };
                    if (Cases.Count == 0)
                    {
                        c.InterArrival = 0;
                        c.ArrivalTime = 0;
                    }
                    else
                    {
                        c.InterArrival = readFromFiles.RandomValues(System.InterarrivalDistribution, c.RandomInterArrival);
                        c.ArrivalTime = Cases[Cases.Count - 1].ArrivalTime + c.InterArrival;
                    }
                    if (c.ArrivalTime > System.StoppingNumber)
                    {
                        break;
                    }
                    GetAssignedServer(c, System.Servers, System.SelectionMethod);
                    while (Queue.Count > 0)
                    {
                        if (Queue.Peek().StartTime <= c.ArrivalTime)
                        {
                            Queue.Dequeue();
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (c.TimeInQueue > 0)
                    {
                        Queue.Enqueue(c);
                        Total_TimeinQueue += c.TimeInQueue;
                        Number_Of_Customers_Who_Waited++;
                    }
                    SimulationTime = Math.Max(SimulationTime, c.EndTime);
                    Max_QueueLength = Math.Max(Max_QueueLength, Queue.Count);
                    Counter++;
                    Cases.Add(c);
                }
            }
            System.PerformanceMeasures.AverageWaitingTime = ((decimal)Total_TimeinQueue / Cases.Count);
            System.PerformanceMeasures.WaitingProbability = ((decimal)Number_Of_Customers_Who_Waited / Cases.Count);
            System.PerformanceMeasures.MaxQueueLength = Max_QueueLength;
            System.SimulationTable = Cases;
            for (int i = 0; i < System.Servers.Count; i++)
            {
                decimal total_workingtime = System.Servers[i].TotalWorkingTime;
                decimal total_idletime = SimulationTime - total_workingtime;
                if (System.Servers[i].ServedCount != 0)
                    System.Servers[i].AverageServiceTime /= System.Servers[i].ServedCount;
                    System.Servers[i].Utilization = (decimal)System.Servers[i].TotalWorkingTime / SimulationTime;
                    System.Servers[i].IdleProbability = total_idletime / SimulationTime;
                
               Console.WriteLine($"AverageServiceTime:{System.Servers[i].AverageServiceTime}");


            }
            //PerformanceMeasuresPerServer(System);
        }

        public int RandomValues(List<TimeDistribution> timeDistributions, int random)
        {
            int serviceTime = -1;
            for (int i = 0; i < timeDistributions.Count; i++)
            {
                if (random >= timeDistributions[i].MinRange && random <= timeDistributions[i].MaxRange)
                { 
                    serviceTime = timeDistributions[i].Time;
                }
            }
            return serviceTime;
        }

        
        //void PerformanceMeasuresPerServer(SimulationSystem sys)
        //{
        //    //total service time of each server
        //    for (int i = 0; i < sys.SimulationTable.Count; i++)
        //    {
        //        if (sys.SimulationTable[i].EndTime > sys.total_runtime)
        //            sys.total_runtime = sys.SimulationTable[i].EndTime;
        //    }
        //    //total idle time of each server
        //    for (int i = 0; i < sys.Servers.Count; i++)
        //    {
        //        decimal total_workingtime = sys.Servers[i].TotalWorkingTime;
        //        decimal total_idletime = sys.total_runtime - total_workingtime;
        //        //IdleProbability = total idle time of server i /total run time 
        //        sys.Servers[i].IdleProbability = total_idletime / sys.total_runtime;

        //       // average service time = total service time / total no of customers
        //        int noOfCustomers = sys.Servers[i].TotalNoOfCustomers.Count;
        //        decimal avr_servicetime = 0;
        //        if (noOfCustomers > 0)
        //        {
        //            avr_servicetime = total_workingtime / noOfCustomers;
        //        }
        //        sys.Servers[i].AverageServiceTime = avr_servicetime;

        //        //utilization(i) = total time server i spends on calls / total run time of simulation
        //        sys.Servers[i].Utilization = sys.Servers[i].TotalWorkingTime / sys.total_runtime;
        //    }
        //}
    }
}
