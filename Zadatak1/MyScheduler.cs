using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Zadatak1
{
   
    public class MyTaskScheduler : TaskScheduler
    {
         // svi taskovi 
         private List<(int ,Task)> allTasks = new List<(int,Task)>();

         // taskovi koji treba da se izvrse
         public LinkedList<Task> pendingTasks = new LinkedList<Task>(); 

        

        // koliko taskova moze istovremeno da se izvrsava, broj niti u thread pool-u
        private int MaxDegreeOfParallelism;

        // broj taskova koji se trenutno izvrsavaju
        private int currentlyRunning = 0;

        public MyTaskScheduler(int maxDegreeOfParallelism)
        {
            if (maxDegreeOfParallelism < 1) throw new Exception();
            MaxDegreeOfParallelism = maxDegreeOfParallelism;
            RunTask();
        }
        

        // override
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return pendingTasks;
        }

        
        protected override void QueueTask(Task task)
        {
            lock (pendingTasks)
            {
                pendingTasks.AddLast(task);

                if (currentlyRunning < MaxDegreeOfParallelism)
                {
                    ++currentlyRunning;
                    RunTask();
                }
            }
            
        }

        private void RunTask()
        {
            ThreadPool.QueueUserWorkItem(_ =>
          {
              while(true)
              {
                  Task task;
                  lock(pendingTasks)
                  {
                      if(pendingTasks.Count == 0)
                      {
                          --currentlyRunning;
                          break;
                      }

                      task = pendingTasks.First.Value;
                      pendingTasks.RemoveFirst();
                      allTasks.RemoveAt(0);
                  }

                  base.TryExecuteTask(task);
              }
          });
        }


        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {         
            return false;
        }


        public void AddTask(int priority, Task task)
        {
            allTasks.Add((priority, task));
            allTasks.Sort((x, y) => x.Item1.CompareTo(y.Item1));

            lock(pendingTasks)
            {
                pendingTasks.Clear();
                foreach( var a in allTasks)
                {
                    QueueTask(a.Item2);
                }
            }
        }
    }
}
