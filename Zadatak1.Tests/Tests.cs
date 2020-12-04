using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Zadatak1.Tests
{
    [TestClass]
    public class MyTaskSchedulerTest
    {
        [TestMethod]
        public void ScheduleTasks()
        {
            const int numOfThreads = 4;
            MyTaskScheduler myTaskScheduler = new MyTaskScheduler(numOfThreads);

            Assert.AreEqual(numOfThreads, myTaskScheduler.GetNumberOfThreadsInThreadPool());           
            
        }
        [TestMethod]
        public void AllTasksExecutedNP()
        {
            Demo.Demo.NonPreemptiveDemo();
            Assert.AreEqual(7, MyTaskScheduler.taskovi.Count);
        }

        [TestMethod]
        public void AllTasksExecutedP()
        {
            Demo.Demo.PreemptiveDemo();
            Assert.AreEqual(14, MyTaskScheduler.taskovi.Count); 
            // 14 jer je polje taskovi static, iz prve testne metode 7, i iz ove 7 :$
        }
      

      
    }
}
