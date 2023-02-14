﻿namespace Azure.Functions.Testing.Cli.Telemetry.PersistenceChannel
{
    /// <summary>
    ///     Implements throttled and persisted transmission of telemetry to Application Insights.
    /// </summary>
    internal class PersistenceTransmitter : IDisposable
    {
        /// <summary>
        ///     The number of times this object was disposed.
        /// </summary>
        private int _disposeCount;

        /// <summary>
        ///     A list of senders that sends transmissions.
        /// </summary>
        private readonly List<Sender> _senders = new();

        /// <summary>
        ///     Initializes a new instance of the <see cref="PersistenceTransmitter" /> class.
        /// </summary>
        /// <param name="storage">The transmissions storage.</param>
        /// <param name="sendersCount">The number of senders to create.</param>
        /// <param name="createSenders">
        ///     A boolean value that indicates if this class should try and create senders. This is a
        ///     workaround for unit tests purposes only.
        /// </param>
        internal PersistenceTransmitter(BaseStorageService storage, int sendersCount, bool createSenders = true)
        {
            if (createSenders)
            {
                for (int i = 0; i < sendersCount; i++)
                {
                    _senders.Add(new Sender(storage, this));
                }
            }
        }

        /// <summary>
        ///     Gets or sets the interval between each successful sending.
        /// </summary>
        internal TimeSpan? SendingInterval { get; set; }

        /// <summary>
        ///     Disposes the object.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Increment(ref _disposeCount) == 1)
            {
                StopSenders();
            }
        }

        /// <summary>
        ///     Stops the senders.
        /// </summary>
        /// <remarks>As long as there is no Start implementation, this method should only be called from Dispose.</remarks>
        private void StopSenders()
        {
            var stoppedTasks = new List<Task>();
            foreach (Sender sender in _senders)
            {
                stoppedTasks.Add(sender.StopAsync());
            }

            Task.WaitAll(stoppedTasks.ToArray());
        }
    }
}
