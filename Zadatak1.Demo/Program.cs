using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Zadatak1.Demo
{
    class Program
    {
        
        static void Main(string[] args)
        {          
                       
            Console.WriteLine("Hello World!");
          

            MyTaskScheduler mts = new MyTaskScheduler(4);

            Action action1 = () =>
                {
                    for (int j = 0; j < 10; j++)
                    {
                        Console.WriteLine("5    " + Thread.CurrentThread.ManagedThreadId);
                       
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
                    Console.WriteLine("3    " + Thread.CurrentThread.ManagedThreadId);
                    Thread.Sleep(1000);
                }
            };
            Task t2 = new Task(action2);

            mts.AddTask(3, t2);


            Action action3 = () =>
            {
                for (int j = 0; j < 10; j++)
                {
                    Console.WriteLine(9 + "    "+ Thread.CurrentThread.ManagedThreadId);
                    Thread.Sleep(1000);
                }
            };
            Task t3 = new Task(action3);

            mts.AddTask(9, t3);


            Action action4 = () =>
            {
                for (int j = 0; j < 10; j++)
                {
                    Console.WriteLine(2 + "    " + Thread.CurrentThread.ManagedThreadId);
                    Thread.Sleep(1000);
                }
            };
            Task t4 = new Task(action4);

            mts.AddTask(2, t4);




            Action action5 = () =>
            {
                for (int j = 0; j < 10; j++)
                {
                    Console.WriteLine(1 + "    " + Thread.CurrentThread.ManagedThreadId);
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
