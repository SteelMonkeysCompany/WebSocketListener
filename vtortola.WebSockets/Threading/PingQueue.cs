﻿using System;
using System.Threading.Tasks;
using vtortola.WebSockets.Tools;
using PingSubscriptionList = System.Collections.Concurrent.ConcurrentBag<vtortola.WebSockets.WebSocket>;

namespace vtortola.WebSockets.Threading
{
    public class PingQueue : TimedQueue<PingSubscriptionList>
    {
        private readonly ObjectPool<PingSubscriptionList> listPool;

        /// <inheritdoc />
        public PingQueue(TimeSpan period) : base(period)
        {
            this.listPool = new ObjectPool<PingSubscriptionList>(() => new PingSubscriptionList(), 20);
        }

        /// <inheritdoc />
        protected override PingSubscriptionList CreateNewSubscriptionList()
        {
            return this.listPool.Take();
        }
        /// <inheritdoc />
        protected override async void NotifySubscribers(PingSubscriptionList list)
        {
            await Task.Yield();

            var webSocket = default(WebSocket);
            while (list.TryTake(out webSocket))
            {
                if (!webSocket.IsConnected)
                    continue;

                try
                {
                    await webSocket.SendPingAsync(null, 0, 0).ConfigureAwait(false);

                }
                catch (Exception pingError)
                {
                    DebugLogger.Instance.Warning("An error occurred while sending ping.", pingError);
                }

                if (webSocket.IsConnected)
                    this.GetSubscriptionList().Add(webSocket);
            }
        }
    }


}