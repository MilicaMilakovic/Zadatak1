using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zadatak1.Demo
{
    class Program
    {
        const int numOfThreads = 4;
        const int defaultDuration = 10;
        public static MyTaskScheduler mts = new MyTaskScheduler(numOfThreads);

        public static void ScheduleTask(int priority, TaskToExecute tte, int maxDuration)
        {
            MyTask task = new MyTask(priority, tte, maxDuration);
            mts.AddTask(task);
        }

        public static void printFunction(MyTask mt)
        {
            for (int i = 0; i < defaultDuration; ++i)
            {
                if(mt.isPaused)
                {
                    Console.WriteLine("Prioritet:" + mt.taskPriority + "| ThreadID:" + Thread.CurrentThread.ManagedThreadId + " |  PAUZIRAN.");

                    if (mt.executeNext != null && mt.executeNextInfo != null)
                    {
                        mt.executeNext(mt.executeNextInfo);

                        Console.WriteLine("Prioritet:" + mt.taskPriority + "| ThreadID:" + Thread.CurrentThread.ManagedThreadId + " |  NASTAVLJA...");
                        mt.Resume();
                    }
                }              

                if (mt.isCancelled)
                {
                    Console.WriteLine("Prioritet:" + mt.taskPriority + "| ThreadID:" + Thread.CurrentThread.ManagedThreadId + " |  PREKINUT.");
                    return;
                }

                Console.WriteLine("Prioritet:" + mt.taskPriority + "| ThreadID:" + Thread.CurrentThread.ManagedThreadId);

                Task.Delay(1000).Wait();
            }
            Console.WriteLine("Prioritet:" + mt.taskPriority + "| ThreadID:" + Thread.CurrentThread.ManagedThreadId + " |  ZAVRSEN.");

        }
        static void Main(string[] args)
        {                                
            Console.WriteLine("Hello World!");     
            
            TaskToExecute tte = printFunction;

            ScheduleTask(7, printFunction, 11);
            ScheduleTask(3, printFunction, 11);
            ScheduleTask(2, printFunction, 11);
            ScheduleTask(4, printFunction, 11);

            Thread.Sleep(5000);

            //Console.WriteLine("prvi thread sleep. dolazi prioritet 10");

            ScheduleTask(10, printFunction, 5);


            Thread.Sleep(3000);
            //Console.WriteLine("drugi thread sleep");

            ScheduleTask(8, printFunction, 11);

            Thread.Sleep(5000);
            //Console.WriteLine("treci thread sleep");
            ScheduleTask(9, printFunction, 7);

            try
            {
                Task.WaitAll(MyTaskScheduler.taskovi.ToArray());

                Console.WriteLine("=====================================");
                Console.WriteLine("\t \t Done.");

                Environment.Exit(0);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
