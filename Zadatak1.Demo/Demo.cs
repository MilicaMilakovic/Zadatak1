﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zadatak1.Demo
{
    public class Demo
    {
        /// <summary>Broj niti u bazenu niti. Maksimalan nivo paralelizma.</summary>
        const int numOfThreads = 4;

        const int defaultDuration = 10;

        public static MyTaskScheduler mts;

        /// <summary>
        /// Funkcija koju korisnik poziva za rasporedjivanje zadataka.
        /// </summary>
        /// <param name="priority">Prioritet zadatka.</param>
        /// <param name="tte">Ono sto zadatak treba da izvrsi.</param>
        /// <param name="maxDuration">Rok izvrsenja prosljedjenog zadatka.</param>
        public static void ScheduleTask(int priority, TaskToExecute tte, int maxDuration)
        {
            MyTask task = new MyTask(priority, tte, maxDuration);
            mts.AddTask(task);
        }

        /// <summary>
        /// 
        /// Funkcija koja simulira zadatak koji treba da se rasporedi i izvrsi.
        /// Da bi rasporedjivac radio ispravno, korisnicka funkcija mora biti definisana na ovaj nacin.
        /// 
        /// Pretpostavlja se da ce zadatak koji se izvrsava trajati <c>defaultDuration</c> sekundi, a samo izvrsavanje
        /// se simulira prolaskom kroz for petlju.
        /// 
        /// Kako bi se omogucilo kooperativno zaustavljanje, neophodno je provjeravati status zadatka koji se izvrsava,
        /// te u skladu sa vrijednostima flegova, vrsiti bilo pauziranje, ili zaustavljanje taska.
        /// 
        /// U slucaju da je zadatak prekinut jer je prekoracio rok izvrsavanja, bice uklonjen sa liste zadataka koji se 
        /// trenutno izvrsavaju, i izvrsavanje ce biti okoncano, uz ispis odgovarajuce poruke.
        /// 
        /// Ukoliko je zadatak pauziran, sto se moze desiti u slucaju preventivnog rasporedjivanja, ako umjesto njega 
        /// treba da se izvrsi zadatak veceg prioriteta, koristi se polje <c>executeNext</c>, koje omogucava
        /// pokretanje novog zadatka na trenutnom thread-u, te nastavljanje pauziranog zadatka, po zavrsetku novog.
        /// 
        /// </summary>
        /// <param name="mt">Podaci o zadatku koji se izvrsava.</param>

        public static void PrintFunction(MyTask mt)
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
                    return;
                }

                Console.WriteLine("Prioritet:" + mt.taskPriority + "| ThreadID:" + Thread.CurrentThread.ManagedThreadId);

                Task.Delay(1000).Wait();
            }
            Console.WriteLine("Prioritet:" + mt.taskPriority + "| ThreadID:" + Thread.CurrentThread.ManagedThreadId + " |  ZAVRSEN.");

            lock(MyTaskScheduler.currentlyRunning)
            {
                MyTaskScheduler.currentlyRunning.Remove(mt);
            }
        }
               
        /// <summary>
        /// Funkcije koje ce se pozvati umjesto printFunction, za demonstraciju deadlock-a.
        /// </summary>
        /// <param name="mt"></param>
        public static void Deadlock1 (MyTask mt)
        {
            Console.WriteLine("deadlock1");

            MyTaskScheduler.resources[0].TryGetLock(mt);
            Thread.Sleep(1000);             
            MyTaskScheduler.resources[1].TryGetLock(mt);
           
        }
        public static void Deadlock2(MyTask mt)
        {
            Console.WriteLine("deadlock2");
            
            MyTaskScheduler.resources[1].TryGetLock(mt);
            Thread.Sleep(1000);            
            MyTaskScheduler.resources[0].TryGetLock(mt);            
        }

        public static void DeadlockDemo()
        {
            // :(

            mts = new MyTaskScheduler(numOfThreads,false);

            MyTaskScheduler.resources.Add(new MyResource());
            MyTaskScheduler.resources.Add(new MyResource());

            ScheduleTask(7, Deadlock1, 11);
            ScheduleTask(3, Deadlock2, 11);

        }

        public static void NonPreemptiveDemo()
        {
            mts = new MyTaskScheduler(numOfThreads, false);

            ScheduleTask(7, PrintFunction, 11);
            ScheduleTask(3, PrintFunction, 11);
            ScheduleTask(2, PrintFunction, 11);
            ScheduleTask(4, PrintFunction, 11);

            Thread.Sleep(5000);

            //Console.WriteLine("=>  Dolazi prioritet 10...");

            ScheduleTask(10, PrintFunction, 5);


            Thread.Sleep(3000);
            //Console.WriteLine("=>  Dolazi prioritet 8...");

            ScheduleTask(8, PrintFunction, 11);

            Thread.Sleep(5000);
            //Console.WriteLine("=>  Dolazi prioritet 9...");
            ScheduleTask(9, PrintFunction, 7);
        }

        public static void PreemptiveDemo()
        {           
            mts = new MyTaskScheduler(numOfThreads, true);            

            ScheduleTask(7, PrintFunction, 11);
            ScheduleTask(3, PrintFunction, 11);
            ScheduleTask(2, PrintFunction, 11);
            ScheduleTask(4, PrintFunction, 11);

            Thread.Sleep(5000);
            Console.WriteLine("=>  Dolazi prioritet 10...");
            ScheduleTask(10, PrintFunction, 5);
            Thread.Sleep(3000);
            Console.WriteLine("=>  Dolazi prioritet 8...");
            ScheduleTask(8, PrintFunction, 11);
            Thread.Sleep(5000);
            Console.WriteLine("=>  Dolazi prioritet 9...");
            ScheduleTask(9, PrintFunction, 7);
        }

        static void Main(string[] args)
        {                                
            Console.WriteLine("Hello World! Pritisnuti enter na kraju...\n");
           
            
            TaskToExecute tte = PrintFunction;

            NonPreemptiveDemo();

            //PreemptiveDemo();

            //DeadlockDemo();

            try
            {
                Console.ReadLine();
                //Task.WaitAll(mts.taskovi.ToArray());

                Console.WriteLine("=======================================");
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
