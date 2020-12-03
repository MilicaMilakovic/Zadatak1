using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Zadatak1
{
    public delegate void TaskToExecute(MyTask mt);

    /*<summary>Klasa <c>MyTaskScheduler</c> 
     * predstavlja klasu koja modeluje rasporedjivac zadataka. 
     * Ova klasa nasljedjuje klasu <c>TaskScheduler</c>. </summary> 
     * **/
    public class MyTaskScheduler : TaskScheduler
    {
        //niti u bazenu
        private Thread[] myThreadPool;

        // koliko taskova moze istovremeno da se izvrsava, broj niti u thread pool-u
        private int MaxDegreeOfParallelism;

        // pozicija na koju se dodaje novi zadatak u listi allTasks
        private int position;

        // svi taskovi 
        public static List<MyTask> allTasks = new List<MyTask>();
        
        public static List<Task> taskovi = new List<Task>();

        // taskovi koji treba da se izvrse
        public static List<Task> pendingTasks = new List<Task>();

        // taskovi koji se trenutno izvrsavaju
        public static List<MyTask> currentlyRunning=new List<MyTask>();       

        // flag pomocu kojeg se bira preventivno ili nepreventivno rasporedjivanje
        private static bool preemption = false;

        // zadatak koji ceka na izvrsavanje, zbog zauzetosti svih niti
        Task novi;

        private Dictionary<Task, MyTask> mapa = new Dictionary<Task, MyTask>();
        private Dictionary<MyTask, Task> mapaInverted = new Dictionary<MyTask, Task>();

        public MyTaskScheduler(int maxDegreeOfParallelism)
        {
            if (maxDegreeOfParallelism < 1) throw new Exception();
            MaxDegreeOfParallelism = maxDegreeOfParallelism;

            myThreadPool = new Thread[maxDegreeOfParallelism];
            for (int i = 0; i < maxDegreeOfParallelism; i++)
            {
                myThreadPool[i] = new Thread(RunTask)
                {
                    IsBackground = false
                };
                myThreadPool[i].Start();
                Console.WriteLine("Thread"+myThreadPool[i].ManagedThreadId +" start. " );
            }

            /** <summary> U slucaju preventivnog zaustavljanja, pokrece se nit koja ce da pauzira izvrsavanje
             * zadatka najmanjeg prioriteta, i na toj niti iz thread pool-a pokrene izvrsavanje novog zadatka,
             * ukoliko je njegov prioritet veci od prioriteta pauziranog. Nit konstatno radi i provjerava da li
             * je doslo do preuzimanja. </summary>  
             * **/
            if (preemption)
            {
                Thread pauseTask = new Thread(() =>
                {
                    Console.WriteLine("pauseTask started");

                    while (true)
                    {
                        Thread.Sleep(500);

                       /** <summary> 
                        * 
                        * U slucaju da sve dostupne niti iz bazena niti izvrsavaju neki zadatak,
                        * a pojavio se novi zadatak koji ceka na izvrsavanje, vrsice se provjera da li medju zadacima koji 
                        * se trenutno izvrsavaju ima neki koji nije pauziran, i ciji prioritet je manji od prioriteta novog
                        * zadatka. Ukoliko je pronadjen takav zadatak, dolazi do preuzimanja.
                        * Buduci da je samo izvrsavanje zadatka definisano na opisan nacin, preuzimanje ce se izvrsiti
                        * tako sto ce se zadatku koji se izvrsava proslijediti zadatak koji ga preuzima, sa potrebnim
                        * argumentima, a on ce biti pauziran. Zadatak koji preuzima ce biti uklonjen iz liste pendingTasks,
                        * kako bi se sprijecilo da se izvrsi ponovo na nekoj drugoj niti.
                        * Dakle, ovaj zadatak ce biti izvrsen na niti sada pauziranog zadatka, a po njegovom zavrsetku, bice
                        * postavljeni odredjeni flegovi koji ce omoguciti nastavak izvrsavanja pauziranog zadatka.
                        * 
                        * </summary>
                        * **/
                        if (currentlyRunning.Count == maxDegreeOfParallelism)
                        {                           
                            if (novi != null)
                            {
                                if (mapa.TryGetValue(novi, out MyTask mt))
                                {
                                    //Console.WriteLine("currently running: "+currentlyRunning.Count );

                                    List<MyTask> miniList = new List<MyTask>();
                                    for(int i=0;i<currentlyRunning.Count;i++)
                                    {
                                        if (!currentlyRunning[i].isPaused)
                                            miniList.Add(currentlyRunning[i]);
                                    }

                                    MyTask min = miniList.Aggregate((i1, i2) => (i1.taskPriority < i2.taskPriority) ? i1 : i2);
                                                                       
                                    novi = null; 
                                    if (min.isPaused)
                                        continue;
                                    
                                    if (min.taskPriority < mt.taskPriority)
                                    {
                                        Console.WriteLine(" {0} => {1}", mt.taskPriority, min.taskPriority);
                                        min.executeNextInfo = mt;
                                        min.executeNext = mt.taskToExecute;
                                        min.Pause();
                                        lock (pendingTasks)
                                        {
                                            pendingTasks.RemoveAt(mt.position);
                                            allTasks.RemoveAt(mt.position);                                            
                                        }
                                    }

                                }

                            }

                        }
                    }
                }); pauseTask.Start();
            }
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return pendingTasks.ToArray();
        }

        protected override void QueueTask(Task task)
        {
            lock (pendingTasks)
            {
                pendingTasks.Insert(position, task);               
            }
        }       

        private void RunTask()
        {
            while (true)
            {
                Thread.Sleep(500);
                try
                {
                    Task task;
                    lock (pendingTasks)
                    {
                        if (pendingTasks.Count == 0)
                        {
                            continue;
                        }

                        task = pendingTasks[0];
                        pendingTasks.RemoveAt(0);
                        allTasks.RemoveAt(0);
                    }

                    /** <summary>
                     * 
                     * Do ovog trenutka, trenutna nit iz naseg thread pool-a je iz liste pendingTasks uzela zadatak 
                     * koji ce izvrsiti.
                     * Na osnovu dobijenog zadatka, iz mape pronalazimo podatke o njemu.
                     * Konkretno, trazimo definisano maksimalno vrijeme izvrsavanja, nakon kojeg zadatak treba da se 
                     * zavrsi. 
                     * Ovdje se pokrece nova nit, koja ce prvo da odspava taj period, a potom da tom zadatku postavi
                     * isCancelled fleg na true, cime ce oznaciti da se taj zadatak prekida, usljed prekoracenja
                     * vremenskog roka izvrsenja. 
                     * 
                     * </summary> 
                     * **/

                    if (mapa.TryGetValue(task, out MyTask taskToTerminate))
                    {
                        currentlyRunning.Add(taskToTerminate);
                        Thread callback = new Thread(() =>
                        {                            
                            Thread.Sleep(TimeSpan.FromSeconds(taskToTerminate.maxTime));
                            taskToTerminate.Cancel();
                        });
                        callback.Start();
                    }
                             
                    /// Trenutak u kome se zadatak zaista pokrece na trenutnoj niti.
                    TryExecuteTask(task);                
                    
                } catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            //Console.WriteLine("ne daj boze");
            return false;
        }        

        public void AddTask(MyTask myTask)
        {
            allTasks.Add(myTask);
            allTasks.Sort((x, y) => x.taskPriority.CompareTo(y.taskPriority));
            position = allTasks.FindIndex(a => a.task.Equals(myTask.task));

           // Console.WriteLine(position);
            myTask.position = position;

            if(currentlyRunning.Count == MaxDegreeOfParallelism)
                novi = myTask.task;

            myTask.task.Start(this);

            mapa.Add(myTask.task, myTask);
            mapaInverted.Add(myTask, myTask.task);
            taskovi.Add(myTask.task);

            foreach(var x in taskovi)
            {
               if( mapa.TryGetValue(x, out MyTask t))
               {
                    Console.Write(t.taskPriority+" ");
               }
            }
            Console.WriteLine("\n");

            //Console.WriteLine("allTasks:");
            //foreach (var x in allTasks)
            //    Console.Write(x.taskPriority  + " | ");

            //Console.WriteLine("\n");


            //Console.WriteLine("pendingTasks:");

            //Console.WriteLine("pending tasks count:" + pendingTasks.Count);
        }

        public static void setPreemption()
        {
            preemption = true;
        }
    }

    public class MyTask
    {
        public int taskPriority;
        public bool isCancelled;
        public bool isPaused;
        public bool isDone;

        public int position;
        public Task task;

        public TaskToExecute taskToExecute;

        public TaskToExecute executeNext;
        public MyTask executeNextInfo;

        public int maxTime;

        public MyTask (int taskPriority, TaskToExecute taskToExecute, int maxTime)
        {
            this.taskPriority = taskPriority;
            this.maxTime = maxTime;
            this.taskToExecute = taskToExecute;

           task = new Task(() =>
           {
               taskToExecute(this);
           });            
        }

        public void Cancel() => isCancelled = true;
        public void Pause() => isPaused = true;
        public void Resume() => isPaused = false;

        public  string toSting()
        {
            return "Prioritet: " + taskPriority;
        }
    }
    

    public class MyResource
    {
        public object resource;
        public int requesterID;
        public int holderID;

        public MyResource()
        {

        }


        
    }
}
