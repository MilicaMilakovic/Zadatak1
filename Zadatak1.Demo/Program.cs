﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Zadatak1.Demo
{
    class Program
    {        
        static void Main(string[] args)
        {

            const int numOfThreads = 4;
                       
            Console.WriteLine("Hello World!");
          

            MyTaskScheduler mts = new MyTaskScheduler(numOfThreads);
            LaneWriter laneWriter = new LaneWriter(numOfThreads);

            void printFunction(MyTask mt)
            {
                for (int i = 0; i < 10; ++i)
                {
                    while(mt.isPaused)
                    {
                        Thread.Sleep(1000);
                    }

                    if(mt.isCancelled)
                    {
                        Console.WriteLine("Prioritet:" + mt.taskPriority + "| ThreadID:" + Thread.CurrentThread.ManagedThreadId+" |  PREKINUT.");
                        return;
                    }
                    Console.WriteLine("Prioritet:"+mt.taskPriority+ "| ThreadID:"+Thread.CurrentThread.ManagedThreadId);
                   
                    Task.Delay(1000).Wait();
                }
                Console.WriteLine("Prioritet:" + mt.taskPriority + "| ThreadID:" + Thread.CurrentThread.ManagedThreadId+" | ZAVRSEN.");

            }

            TaskToExecute tte = printFunction;

            MyTask fistTask = new MyTask(1, printFunction, 4);
            mts.AddTask(fistTask);


           /* mts.AddTask(3, (x) => { Console.WriteLine(3 + " nit " + Thread.CurrentThread.ManagedThreadId);
                Thread.Sleep(10000);
            }, 1000);

            mts.AddTask(2, (x) => { Console.WriteLine(2 + " nit " + Thread.CurrentThread.ManagedThreadId);
                Thread.Sleep(1200);
            },500);

            /*
            mts.AddTask(1, (x) => { Console.WriteLine(1 + " nit " + Thread.CurrentThread.ManagedThreadId);
                Thread.Sleep(1100);
            });
            mts.AddTask(5, (x) => { Console.WriteLine(5 + " nit " + Thread.CurrentThread.ManagedThreadId);
                Thread.Sleep(1500);
            });
            mts.AddTask(7, (x) => { Console.WriteLine(7 + " nit " + Thread.CurrentThread.ManagedThreadId);
                Thread.Sleep(1700);
            });
            mts.AddTask(4, (x) => { Console.WriteLine(4 + " nit " + Thread.CurrentThread.ManagedThreadId);
                Thread.Sleep(1400);
            });
            mts.AddTask(8, (x) => { Console.WriteLine(8 + " nit " + Thread.CurrentThread.ManagedThreadId);
                Thread.Sleep(1800);
            });*/



            /* Action action1 = () =>
                 {
                     for (int j = 0; j < 10; j++)
                     {
                         Console.WriteLine("5  running on thread  " + Thread.CurrentThread.ManagedThreadId);

                         Thread.Sleep(1000);
                     }
                 };

             Task t11 = new Task(action1);


             //TaskFactory tf = new TaskFactory(mts);

             //Task tttt = Task.Factory.StartNew(() =>
             //{
             //    Console.WriteLine(5);
             //    Thread.Sleep(1000);
             //});


             mts.AddTask(5, t11);
             Thread.Sleep(2000);

            Action action2 = () =>
             {
                 for (int j = 0; j < 10; j++)
                 {
                     Console.WriteLine("3  running on thread    " + Thread.CurrentThread.ManagedThreadId);
                     Thread.Sleep(1000);
                 }
             };
             Task t2 = new Task(action2);

             mts.AddTask(3, t2);


             Action action3 = () =>
             {
                 for (int j = 0; j < 10; j++)
                 {
                     Console.WriteLine(9 + "  running on thread   " + Thread.CurrentThread.ManagedThreadId);
                     Thread.Sleep(1000);
                 }
             };
             Task t3 = new Task(action3);

             mts.AddTask(9, t3);


             Action action4 = () =>
             {
                 for (int j = 0; j < 10; j++)
                 {
                     Console.WriteLine(2 + "  running on thread   " + Thread.CurrentThread.ManagedThreadId);
                     Thread.Sleep(1000);
                 }
             };
             Task t4 = new Task(action4);

             mts.AddTask(2, t4);

             Action action5 = () =>
             {
                 for (int j = 0; j < 10; j++)
                 {
                     Console.WriteLine(1 + " running on thread    " + Thread.CurrentThread.ManagedThreadId);
                     Thread.Sleep(1000);
                 }
             };
             Task t5 = new Task(action5);

             mts.AddTask(1, t5);

             /*

             Action action6 = () =>
             {
                 for (int j = 0; j < 10; j++)
                 {
                     Console.WriteLine(7);
                     Thread.Sleep(1000);
                 }
             };
             Task t6 = new Task(action6);
             mts.AddTask(7, t6);
             */


        }
    }
}
