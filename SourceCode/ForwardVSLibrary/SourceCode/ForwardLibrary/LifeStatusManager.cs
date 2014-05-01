using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ForwardLibrary
{
    namespace ApplicationLife
    {
        public enum LifeStatus : int { Starting = 0, Running, Terminating };

        public delegate void StatusCallback(LifeStatus NewStat);

        

        public class LifeStatusManager
        {                                    
            /// <summary>
            /// The status variable
            /// </summary>
            public LifeStatus Status
            {
                get { return _status; }
            }


            LifeStatus _status = LifeStatus.Starting;
            private Queue<StatusCallback> callbacks = new Queue<StatusCallback>();

            private static Random randObj = new Random(1);
            private StringBuilder theLock = new StringBuilder(randObj.Next().ToString());

            /// <summary>
            /// Add a callback to the life status change list
            /// </summary>
            /// <param name="callback"></param>
            public void AddCallback(StatusCallback callback)
            {
                lock (theLock)
                {
                    callbacks.Enqueue(callback);
                }
            }

            /// <summary>
            /// Remove a callback from the LifeStatus list
            /// </summary>
            /// <param name="callback"></param>
            public void RemoveCallback(StatusCallback callback)
            {
                lock (theLock)
                {
                    Queue<StatusCallback> tcallbacks = new Queue<StatusCallback>();
                    while (callbacks.Count > 0)
                    {                        
                        StatusCallback t = callbacks.Dequeue();
                        if (t != callback)
                            tcallbacks.Enqueue(t);
                    }
                    callbacks = tcallbacks;
                }
            }

            /// <summary>
            /// Call this to change the life status. This function will block
            /// until all callback functions have returned
            /// </summary>
            /// <param name="NewStat"></param>
            public void ChangeStatus(LifeStatus NewStat)
            {
                Queue<FinishCall> q = new Queue<FinishCall>();
                lock (theLock)
                {
                    //call them all
                    foreach (StatusCallback b in callbacks)
                    {
                        FinishCall tcall = new FinishCall();
                        try
                        {
                            tcall.result = b.BeginInvoke(_status, null, null);
                        }
                        catch { }
                        tcall.callback = b;
                        q.Enqueue(tcall);
                    }
                    
                    //wait for them all to complete:
                    foreach(FinishCall tcall in q)
                    {
                        try
                        {
                            tcall.callback.EndInvoke(tcall.result);
                        }
                        catch { }
                    }
                }
            }

            class FinishCall
            {
                public IAsyncResult result;
                public StatusCallback callback;
                public FinishCall()
                { }
                public FinishCall(IAsyncResult res, StatusCallback cb)
                {
                    result = res;
                    callback = cb;
                }
            }
        }
    }
}
