using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zadatak1.Demo
{
    class Program
    {
        const int numOfThreads = 4;
        const int defaultDuration = 10;

        public static MyTaskScheduler mts;

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
                    lock (MyTaskScheduler.currentlyRunning)
                    {
                        MyTaskScheduler.currentlyRunning.Remove(mt);
                    }
                    mt.isDone = true;
                    return;
                }

                Console.WriteLine("Prioritet:" + mt.taskPriority + "| ThreadID:" + Thread.CurrentThread.ManagedThreadId);

                Task.Delay(1000).Wait();
            }
            Console.WriteLine("Prioritet:" + mt.taskPriority + "| ThreadID:" + Thread.CurrentThread.ManagedThreadId + " |  ZAVRSEN.");

            mt.isDone = true;
            lock(MyTaskScheduler.currentlyRunning)
            {
                MyTaskScheduler.currentlyRunning.Remove(mt);
            }
        }
        static void Main(string[] args)
        {                                
            Console.WriteLine("Hello World!\n");     
            
            TaskToExecute tte = printFunction;

            MyTaskScheduler.setPreemption();

            mts = new MyTaskScheduler(numOfThreads);

            ScheduleTask(7, printFunction, 11);
            ScheduleTask(3, printFunction, 11);
            ScheduleTask(2, printFunction, 11);
            ScheduleTask(4, printFunction, 11);

            Thread.Sleep(5000);

            Console.WriteLine("prvi thread sleep. dolazi prioritet 10");

            ScheduleTask(10, printFunction, 5);


            Thread.Sleep(3000);
            Console.WriteLine("drugi thread sleep, dolazi 8");

            ScheduleTask(8, printFunction, 11);

            Thread.Sleep(5000);
            Console.WriteLine("treci thread sleep, dolazi 9");
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
