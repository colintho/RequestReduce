﻿using System;
using System.Collections.Concurrent;
using System.Threading;
using RequestReduce.Reducer;
using RequestReduce.Utilities;

namespace RequestReduce.Module
{
    public class ReducingQueue : IReducingQueue
    {
        protected readonly IReducer reducer;
        protected readonly IReductionRepository reductionRepository;
        protected ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
        protected Thread backgroundThread;
        protected bool isRunning = true;
        protected Action<Exception> CaptureErrorAction;

        public ReducingQueue(IReducer reducer, IReductionRepository reductionRepository)
        {
            this.reducer = reducer;
            this.reductionRepository = reductionRepository;
            backgroundThread = new Thread(new ThreadStart(ProcessQueue)) { IsBackground = true };
            backgroundThread.Start();
        }

        public void Enqueue(string urls)
        {
            queue.Enqueue(urls);
        }

        public int Count
        {
            get { return queue.Count; }
        }

        public void CaptureError(Action<Exception> captureAction)
        {
            CaptureErrorAction = captureAction;
        }

        private void ProcessQueue()
        {
            while(isRunning)
            {
                ProcessQueuedItem();
                Thread.Sleep(100);
            }
        }

        protected void ProcessQueuedItem()
        {
            try
            {
                string urlsToReduce = null;
                if (queue.TryDequeue(out urlsToReduce) && reductionRepository.FindReduction(urlsToReduce) == null)
                {
                    var key = Hasher.Hash(urlsToReduce);
                    var reducedUrl = reducer.Process(key, urlsToReduce);
                    reductionRepository.AddReduction(key, reducedUrl);
                }
            }
            catch(Exception e)
            {
                if (CaptureErrorAction != null)
                    CaptureErrorAction(e);
            }
        }
    }
}
