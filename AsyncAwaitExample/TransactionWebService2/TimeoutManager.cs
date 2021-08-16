using System;
using System.Timers;

namespace TransactionWebService2
{
    public class TimeoutManager
    {
        public delegate void TryReset(bool cancel);

        public static TryReset RunActionAfter(TimeSpan after, Action action)
        {
            var timer = new Timer(after.TotalMilliseconds);

            bool actionMarkedToBeExecuted = false;

            timer.Elapsed += (o, e) =>
            {
                lock (timer)
                {
                    if (actionMarkedToBeExecuted) //Already executed before
                        return;
                        
                    actionMarkedToBeExecuted = true;
                }

                action();
            };

            timer.AutoReset = false;

            timer.Start();

            return (cancel) =>
            {
                lock (timer)
                {
                    if (actionMarkedToBeExecuted)
                        return;

                    timer.Stop();

                    if (!cancel)
                    {
                        timer.Start();
                    }
                }
            };

        }
    }
}