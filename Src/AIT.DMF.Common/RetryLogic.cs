// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RetryLogic.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Defines the retry logic for downloaders.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.Common
{
    using System;
    using System.Threading;
    using AIT.DMF.Contracts.Exceptions;

    public class RetryLogic
    {
        private int _numRetriesOverall;

        /// <summary>
        /// Creates an instance and initializes maxRetries
        /// </summary>
        /// <param name="maxRetries">The maximum number of retries for all actions</param>
        public RetryLogic(int maxRetries)
        {
            _numRetriesOverall = maxRetries;
        }

        public int RetryAction(Action action, int numRetries, int retryTimeout)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action"); // slightly safer...
            }

            var retries = 0;
            do
            {
                try
                {
                    action();
                    return retries;
                }
                catch (Exception ex)
                {
                    // improved to avoid silent failure
                    if (retries >= numRetries || _numRetriesOverall == 0)
                    {
                        throw new DependencyDownloaderException(string.Format("Download retry limit of {0} exceeded (Exception message: {1})", numRetries, ex.Message));
                    }

                    Thread.Sleep(retryTimeout);
                }
            }
            while (retries++ < numRetries && _numRetriesOverall-- >= 0);

            return retries;
        }
    }
}
