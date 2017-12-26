﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Binance.Api.WebSocket.Events;

namespace Binance.Api.WebSocket
{
    public interface ISymbolStatisticsWebSocketClient : IBinanceWebSocketClient
    {
        #region Public Events

        /// <summary>
        /// The symbol statistics event.
        /// </summary>
        event EventHandler<SymbolStatisticsEventArgs> Statistics;

        #endregion Public Events

        #region Public Methods

        /// <summary>
        /// Subscribe to the specified symbol and begin receiving events.
        /// Awaiting this method will not return until the token
        /// is canceled, this <see cref="ISymbolStatisticsWebSocketClient"/> is disposed,
        /// or an exception occurs.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <param name="callback">An event callback.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns><see cref="Task"/></returns>
        Task SubscribeAsync(string symbol, Action<SymbolStatisticsEventArgs> callback, CancellationToken token);

        #endregion Public Methods
    }
}
