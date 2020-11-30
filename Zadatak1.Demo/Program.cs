using System;
using System.Collections.Generic;
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
                while (mt.isPaused)
                {
                    Thread.Sleep(1000);
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

            ScheduleTask(1, printFunction, 2);
            ScheduleTask(3, printFunction, 8);
            ScheduleTask(2, printFunction, 11);
            ScheduleTask(4, printFunction, 5);
            ScheduleTask(5,printFunction, 7);           

        }
    }
}
