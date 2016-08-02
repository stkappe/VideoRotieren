using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoRotieren
{
    using System;
    using System.Threading;
    using System.Collections.Generic;
    public class PCQueue
    {
        readonly object _locker = new object();
        Thread[] _workers;
        Queue<Action> _itemQ = new Queue<Action>();

        public PCQueue(int workerCount)
        {
            _workers = new Thread[workerCount];

            // Create and start a separate thread for each worker
            Thread t1 = new Thread(Consume);
            t1.Name = "Alpha";
            _workers[0] = t1;
            t1.Start();

            Thread t2 = new Thread(Consume);
            t2.Name = "Beta";
            _workers[1] = t2;
            t2.Start();

            /*for (int i = 0; i < workerCount; i++)
            {
                (_workers[i] = new Thread(Consume)).Start();
            }*/

        }

        public void Shutdown(bool waitForWorkers)
        {
            // Enqueue one null item per worker to make each exit.
            foreach (Thread worker in _workers)
                EnqueueItem(null);

            // Wait for workers to finish
            /*if (waitForWorkers)
                foreach (Thread worker in _workers)
                {
                    worker.Join();
                    Console.WriteLine("Thread " + worker.Name + " has ended");
                }*/

        }

        public void EnqueueItem(Action item)
        {
            lock (_locker)
            {
                _itemQ.Enqueue(item);           // We must pulse because we're
                Monitor.Pulse(_locker);         // changing a blocking condition.
            }
        }

        void Consume()
        {
            while (true)                        // Keep consuming until
            {                                   // told otherwise.
                Action item;
                lock (_locker)
                {
                    while (_itemQ.Count == 0) Monitor.Wait(_locker);
                    item = _itemQ.Dequeue();
                }
                if (item == null)
                {
                    Console.WriteLine("Thread " + Thread.CurrentThread.Name + " has ended");
                    return;                       // This signals our exit.
                }
                item();                           // Execute item.
            }
        }

        static void Main()
        {

            PCQueue inst = new PCQueue(2);


            Console.WriteLine("Log: Enqueuing 10 items...");

            for (int i = 0; i < 11; i++)
            {
                int itemNumber = i;      // To avoid the captured variable trap

                Action item = delegate () {
                    Thread.Sleep(3000);          // Simulate time-consuming work
                    Console.WriteLine("Log:  Task" + itemNumber);
                };
                inst.EnqueueItem(item);

                Console.WriteLine("Log: enqueued item with number " + itemNumber);
            }

            inst.Shutdown(true);
            Console.WriteLine();

            //Console.WriteLine("Log: Workers complete!");

        }

    }




}
