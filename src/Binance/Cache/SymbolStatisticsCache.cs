﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Binance.Api;
using Binance.Cache.Events;
using Binance.Market;
using Binance.WebSocket;
using Binance.WebSocket.Events;
using Microsoft.Extensions.Logging;

namespace Binance.Cache
{
    public sealed class SymbolStatisticsCache : WebSocketClientCache<ISymbolStatisticsWebSocketClient, SymbolStatisticsEventArgs, SymbolStatisticsCacheEventArgs>, ISymbolStatisticsCache
    {
        #region Public Properties

        public IEnumerable<SymbolStatistics> Statistics
        {
            get { lock (_sync) { return _symbols.Select(s => _statistics[s]).ToArray(); } }
        }

        #endregion Public Properties

        #region Private Fields

        private readonly IList<string> _symbols = new List<string>();

        private readonly IDictionary<string, SymbolStatistics> _statistics
            = new Dictionary<string, SymbolStatistics>();

        private readonly object _sync = new object();

        #endregion Private Fields

        #region Constructors

        public SymbolStatisticsCache(IBinanceApi api, ISymbolStatisticsWebSocketClient client, ILogger<SymbolStatisticsCache> logger = null)
            : base(api, client, logger)
        { }

        #endregion Constructors

        #region Public Methods

        public SymbolStatistics GetStatistics(string symbol)
        {
            Throw.IfNullOrWhiteSpace(symbol, nameof(symbol));

            symbol = symbol.FormatSymbol();

            lock (_sync)
            {
                return _statistics.ContainsKey(symbol) ? _statistics[symbol] : null;
            }
        }

        public IEnumerable<SymbolStatistics> GetStatistics(params string[] symbols)
        {
            Throw.IfNull(symbols, nameof(symbols));

            lock (_sync)
            {
                foreach (var symbol in symbols)
                    yield return GetStatistics(symbol);
            }
        }

        public void Subscribe(Action<SymbolStatisticsCacheEventArgs> callback)
        {
            base.LinkTo(Client, callback);

            Client.Subscribe(ClientCallback);
        }

        public void Subscribe(Action<SymbolStatisticsCacheEventArgs> callback, params string[] symbols)
        {
            Throw.IfNull(symbols, nameof(symbols));

            base.LinkTo(Client, callback);

            foreach (var symbol in symbols)
            {
                Throw.IfNullOrWhiteSpace(symbol, nameof(symbol));

                if (_symbols.Contains(symbol))
                    continue;

                _symbols.Add(symbol);

                Client.Subscribe(symbol, ClientCallback);
            }
        }

        public override void LinkTo(ISymbolStatisticsWebSocketClient client, Action<SymbolStatisticsCacheEventArgs> callback = null)
        {
            // Confirm client is subscribed to only one stream.
            if (client.WebSocket.IsCombined)
                throw new InvalidOperationException($"{nameof(SymbolStatisticsCache)} can only link to {nameof(ISymbolStatisticsWebSocketClient)} events from a single stream (not combined streams).");

            base.LinkTo(client, callback);
            Client.StatisticsUpdate += OnClientEvent;
        }

        public override void UnLink()
        {
            Client.StatisticsUpdate -= OnClientEvent;
            base.UnLink();
        }

        #endregion Public Methods

        #region Protected Methods

        protected override async Task<SymbolStatisticsCacheEventArgs> OnAction(SymbolStatisticsEventArgs @event)
        {
            if (_statistics.Count == 0)
            {
                Logger?.LogInformation($"{nameof(SymbolStatisticsCache)}: Initializing symbol statistics...");

                if (!_symbols.Any())
                {
                    var statistics = await Api.Get24HourStatisticsAsync(@event.Token)
                        .ConfigureAwait(false);

                    lock (_sync)
                    {
                        foreach (var stats in statistics)
                        {
                            _statistics[stats.Symbol] = stats;
                        }
                    }
                }
            }

            lock (_sync)
            {
                foreach (var stats in @event.Statistics)
                {
                    _statistics[stats.Symbol] = stats;
                }

                return new SymbolStatisticsCacheEventArgs(_symbols.Select(s => _statistics[s]).ToArray());
            }
        }

        #endregion Protected Methods
    }
}
