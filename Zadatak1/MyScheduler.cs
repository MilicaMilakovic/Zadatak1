using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Zadatak1
{
    /// <summary>
    /// Delegat na funkciju koju korisnik prosljedjuje na rasporedjivac.
    /// Pokazuje na <c>printFunction</c>, definisanu u sklopu Zadatak1.Demo fajla.
    /// </summary>
    /// <param name="mt">Objekat tipa MyTask, unutar koga su sadrzane sve informacije o tasku koji se izvrsava.</param>
    public delegate void TaskToExecute(MyTask mt);

    ///<summary>Klasa <c>MyTaskScheduler</c> 
    /// predstavlja klasu koja modeluje rasporedjivac zadataka. 
    ///Ova klasa nasljedjuje klasu <c>TaskScheduler</c>. </summary> 
    
    public class MyTaskScheduler : TaskScheduler
    {
        /// <summary>Niti u bazenu.</summary>
        private Thread[] myThreadPool;

        /// <summary>Koliko taskova moze istovremeno da se izvrsava, odnosno broj niti u thread pool-u.</summary>
        private int MaxDegreeOfParallelism;

        /// <summary>Pozicija na koju se dodaje novi zadatak u listi allTasks.</summary>
        private int position;

        /// <summary>Svi taskovi. </summary>
        public static List<MyTask> allTasks = new List<MyTask>();
        
        public static List<Task> taskovi = new List<Task>();

        /// <summary>Taskovi koji treba da se izvrse.</summary>
        public static List<Task> pendingTasks = new List<Task>();

        /// <summary>Taskovi koji se trenutno izvrsavaju.</summary>
        public static List<MyTask> currentlyRunning=new List<MyTask>();

        /// <summary>Flag pomocu kojeg se bira preventivno ili nepreventivno rasporedjivanje.</summary>
        private static bool preemption = false;

        /// <summary>Zadatak koji ceka na izvrsavanje, zbog zauzetosti svih niti.</summary>
        Task novi;

        private Dictionary<Task, MyTask> mapa = new Dictionary<Task, MyTask>();
        private Dictionary<MyTask, Task> mapaInverted = new Dictionary<MyTask, Task>();

        public static List<MyResource> resources = new List<MyResource>();

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
            Console.WriteLine();
            /** <summary> U slucaju preventivnog zaustavljanja, pokrece se nit koja ce da pauzira izvrsavanje
             * zadatka najmanjeg prioriteta, i na toj niti iz thread pool-a pokrene izvrsavanje novog zadatka,
             * ukoliko je njegov prioritet veci od prioriteta pauziranog. Nit konstatno radi i provjerava da li
             * je doslo do preuzimanja. </summary>  
             * **/
            if (preemption)
            {
                Thread pauseTask = new Thread(() =>
                {
                    Console.WriteLine("=>   pauseTask started");
                    Console.WriteLine();

                    while (true)
                    {
                        Thread.Sleep(500);

                       /** <summary> 
                        * 
                        * U slucaju da sve dostupne niti iz bazena niti izvrsavaju neki zadatak,
                        * a pojavio se novi zadatak koji ceka na izvrsavanje, vrsice se provjera da li medju zadacima koji 
                        * se trenutno izvrsavaju ima neki koji nije pauziran, i ciji prioritet je manji od prioriteta novog
                        * zadatka. Ukoliko je pronadjen takav zadatak, dolazi do preuzimanja.
                        * Buduci da je izvrsavanje zadatka definisano na opisan nacin, preuzimanje ce se izvrsiti
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

            /// <summary>
            /// 
            /// Nit koja vrsi detekciju deadlock-a, na nacin da prolazi kroz listu resursa.
            /// Deadlock ce se desiti u slucaju da se nadju dva resursa takva da je holder jednog requester 
            /// drugog, i obrnuto.
            /// 
            /// Ako su pronadjena takva dva resursa, nit je detektovala deadlock, koji ce razrijesiti na nacin
            /// sto ce uzeti zadatak holder sa manjim prioritetom, i terminirati ga.
            /// 
            /// </summary>
            
            Thread deadlockDetector = new Thread ( () =>
            {
                while(true)
                {
                    Thread.Sleep(500);

                    if(resources.Count > 0)
                    {
                        for(int i=0; i < resources.Count-1; i++)
                        {
                            MyResource resource = resources[i];

                            for(int j=1; j < resources.Count; j++)
                            {
                                if (resource.holder != null && resource.requester == resources[j].holder && resource.holder == resources[j].requester)
                                {
                                    Console.WriteLine("Detektovan deadlock!");

                                    // razrjesenje - task sa manjim prioritetom se terminira

                                    MyTask endTask = resource.holder.taskPriority < resources[j].holder.taskPriority ? resource.holder : resources[j].holder;
                                    endTask.Cancel();
                                }
                            }
                        }
                    }
                }

            });
            deadlockDetector.Start();
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
        /** <summary>
                    * 
                    * Do ovog trenutka, trenutna nit iz naseg thread pool-a je iz liste pendingTasks uzela zadatak 
                    * koji ce izvrsiti.
                    * Na osnovu dobijenog zadatka, iz mape pronalazimo podatke o njemu.
                    * Konkretno, trazimo definisano maksimalno vrijeme izvrsavanja, nakon kojeg zadatak treba da se 
                    * zavrsi. 
                    * Ovdje se pokrece nova nit <c>callBack</c>, koja ce prvo da odspava taj period, a potom da tom zadatku postavi
                    * isCancelled fleg na true, cime ce oznaciti da se taj zadatak prekida, usljed prekoracenja
                    * vremenskog roka izvrsenja. 
                    * 
                    * </summary> 
                    * **/
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

        /// <summary>
        /// Zadatak se prvo dodaje u listu <c>allTasks</c>, koja se potom sortira. Unutar te liste 
        /// pronalazi se indeks pristiglog zadatka u odnosu na koji cemo odrediti poziciju unutar 
        /// <c>pendingTasks</c> liste. 
        /// Ako su trenutno sve niti zauzete, u slucaju preventivnog rasporedjivanja, task postaje kandidat
        /// za preuzimanje.
        /// Task se potom startuje na nasem rasporedjivacu, i dodaje se u potrebne liste i mape.
        /// </summary>
        /// <param name="myTask">Zadatak koji se dodaje u rasporedjivac.</param>
        
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

            //foreach(var x in taskovi)
            //{
            //   if( mapa.TryGetValue(x, out MyTask t))
            //   {
            //        Console.Write(t.taskPriority+" ");
            //   }
            //}
            //Console.WriteLine("\n");

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

    /**
     * <summary>
     * 
     * Klasa <c>MyTask</c> predstavlja klasu unutar koje su sadrzane sve potrebne informacije u vezi sa 
     * zadatkom koji se rasporedjuje za izvrsavanje.
     * Ove informacije odnose se na prioritet zadatka, njegovu poziciju unutar liste zadataka koji se 
     * rasporedjuju, maksimalno trajanje, te statusne informacije koje govore o tome da li je zadatak pauziran,
     * prekinut ili zavrsen.
     * 
     * Takodje, unutar klase definisan je delegat na funkciju koju korisnik prosljedjuje, kao i Task koji bi trebalo
     * da izvrsi tu funkciju, na niti iz bazena koja ga je preuzela iz liste pendingTasks.
     * 
     * <c>executeNext</c> i <c>executeNextInfo</c> predstavljaju polja koja su od znacaja u slucaju preventivnog 
     * rasporedjivanja, u slucaju da je zadatak pauziran jer je naisao zadatak sa vecim prioritetom, koji treba da 
     * se izvrsi prije njega.
     * 
     * Ideja je da se u navedenom slucaju pokrene izvrsavanje <c>executeNext</c>, sa proslijedjenim argumentom
     * <c>executeNextInfo</c> na istoj niti, a po zavrsetku da se zadatak ukloni iz pendingTasks, i nastavi izvrsavanje
     * pauziranog zadatka.
     * 
     * Definisane metode u sklopu klase sluze samo za postavljanje navedenih statusnih flegova.
     * 
     * </summary>
     * */

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

    /// <summary>
    /// Klasa <c>MyResource</c> predstavlja model resursa koji zadaci koriste.
    /// Unutar nje nalazi se stvarni objekat koji ce sluziti za zakljucavanje.
    /// 
    /// Ideja je da za svaki resurs koji ce se koristiti znamo koji task ga 
    /// pokusava zakljucati, i koji task ga trenutno drzi.
    /// Kada task zakljuca resurs, on postavi sebe za holdera, zavrsava posao, i uklanja holdera.
    /// 
    /// </summary>

    public class MyResource
    {
        public object resource = new object();
        public MyTask requester;
        public MyTask holder;
        private static int count;
        int id;

        public MyResource()
        {
            id = count;
            ++count;
        }
                
        public void TryGetLock(MyTask mt)
        {
             requester = mt;
             Console.WriteLine("try get lock " + id + " requester: " + mt.taskPriority);
             if (holder == null)
             {
                 LockResource(mt);
             }

        }

        private void LockResource(MyTask mt)
        {
            lock (resource)
            {
                holder = mt;
                Console.WriteLine("zakljucan " + id+" drzi ga:" + holder.taskPriority + " req "+ requester.taskPriority);

                requester = null;

                // Simulate work
                Thread.Sleep(5000);
                holder = null;
            }          
        }
    }
}
