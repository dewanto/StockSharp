#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleTwime.SampleTwimePublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleTwime
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Messages;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Localization;
	using StockSharp.Plaza;
	using StockSharp.Twime;

	public partial class MainWindow
	{
		private bool _isInitialized;

		public TwimeTrader Trader = new TwimeTrader();

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly TradesWindow _tradesWindow = new TradesWindow();
		private readonly MyTradesWindow _myTradesWindow = new MyTradesWindow();
		private readonly OrdersWindow _ordersWindow = new OrdersWindow();
		private readonly PortfoliosWindow _portfoliosWindow = new PortfoliosWindow();

		private readonly LogManager _logManager = new LogManager();

		public MainWindow()
		{
			InitializeComponent();

			Title = Title.Put("TWIME");

			_ordersWindow.MakeHideable();
			_myTradesWindow.MakeHideable();
			_tradesWindow.MakeHideable();
			_securitiesWindow.MakeHideable();
			_portfoliosWindow.MakeHideable();

			var mdAdapter = new PlazaMessageAdapter(Trader.TransactionIdGenerator)
			{
				IsCGate = true,
			};
			mdAdapter.RemoveTransactionalSupport();
			Trader.Adapter.InnerAdapters.Add(mdAdapter);

			Instance = this;

			Trader.LogLevel = LogLevels.Debug;

			_logManager.Sources.Add(Trader);
			_logManager.Listeners.Add(new FileLogListener { LogDirectory = "StockSharp_Twime" });

			TransactionAddress.EndPoint = Trader.TransactionAddress;
			RecoveryAddress.EndPoint = Trader.RecoveryAddress;
			Login.Text = Trader.Login;
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_ordersWindow.DeleteHideable();
			_myTradesWindow.DeleteHideable();
			_tradesWindow.DeleteHideable();
			_securitiesWindow.DeleteHideable();
			_portfoliosWindow.DeleteHideable();
			
			_securitiesWindow.Close();
			_tradesWindow.Close();
			_myTradesWindow.Close();
			_ordersWindow.Close();
			_portfoliosWindow.Close();

			Trader.Dispose();

			base.OnClosing(e);
		}

		public static MainWindow Instance { get; private set; }

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (!_isInitialized)
			{
				_isInitialized = true;

				Trader.Restored += () => this.GuiAsync(() =>
				{
					// update gui labes
					ChangeConnectStatus(true);
					MessageBox.Show(this, LocalizedStrings.Str2958);
				});

				// subscribe on connection successfully event
				Trader.Connected += () =>
				{
					this.GuiAsync(() => ChangeConnectStatus(true));
				};

				Trader.Disconnected += () => this.GuiAsync(() => ChangeConnectStatus(false));

				// subscribe on connection error event
				Trader.ConnectionError += error => this.GuiAsync(() =>
				{
					// update gui labes
					ChangeConnectStatus(false);

					MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);
				});

				// subscribe on error event
				Trader.Error += error => this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

				// subscribe on error of market data subscription event
				Trader.MarketDataSubscriptionFailed += (security, type, error) => this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(type, security)));

				Trader.NewSecurities += securities => _securitiesWindow.SecurityPicker.Securities.AddRange(securities);
				Trader.NewMyTrades += trades => _myTradesWindow.TradeGrid.Trades.AddRange(trades);
				Trader.NewTrades += trades => _tradesWindow.TradeGrid.Trades.AddRange(trades);
				Trader.NewOrders += orders => _ordersWindow.OrderGrid.Orders.AddRange(orders);

				Trader.NewPortfolios += portfolios =>
				{
					// subscribe on portfolio updates
					//portfolios.ForEach(Trader.RegisterPortfolio);

					_portfoliosWindow.PortfolioGrid.Portfolios.AddRange(portfolios);
				};
				Trader.NewPositions += positions => _portfoliosWindow.PortfolioGrid.Positions.AddRange(positions);

				// subscribe on error of order registration event
				Trader.OrdersRegisterFailed += OrdersFailed;
				// subscribe on error of order cancelling event
				Trader.OrdersCancelFailed += OrdersFailed;

				// subscribe on error of stop-order registration event
				Trader.StopOrdersRegisterFailed += OrdersFailed;
				// subscribe on error of stop-order cancelling event
				Trader.StopOrdersCancelFailed += OrdersFailed;

				// set market data provider
				_securitiesWindow.SecurityPicker.MarketDataProvider = Trader;

				ShowSecurities.IsEnabled = ShowTrades.IsEnabled = ShowMyTrades.IsEnabled = ShowOrders.IsEnabled = ShowPortfolios.IsEnabled = true;
			}

			((PlazaMessageAdapter)Trader.MarketDataAdapter).IsDemo = IsDemo.IsChecked == true;

			switch (Trader.ConnectionState)
			{
				case ConnectionStates.Failed:
				case ConnectionStates.Disconnected:
					Trader.TransactionAddress = TransactionAddress.EndPoint;
					Trader.RecoveryAddress = RecoveryAddress.EndPoint;
					Trader.Login = Login.Text;

					Trader.Connect();
					break;
				case ConnectionStates.Connected:
					Trader.Disconnect();
					break;
			}
		}

		private void OrdersFailed(IEnumerable<OrderFail> fails)
		{
			this.GuiAsync(() =>
			{
				foreach (var fail in fails)
					MessageBox.Show(this, fail.Error.ToString(), LocalizedStrings.Str2960);
			});
		}

		private void ChangeConnectStatus(bool isConnected)
		{
			ConnectBtn.Content = isConnected ? LocalizedStrings.Disconnect : LocalizedStrings.Connect;
		}

		private void ShowSecuritiesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_securitiesWindow);
		}

		private void ShowTradesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_tradesWindow);
		}

		private void ShowMyTradesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_myTradesWindow);
		}

		private void ShowOrdersClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_ordersWindow);
		}

		private void ShowPortfoliosClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_portfoliosWindow);
		}

		private static void ShowOrHide(Window window)
		{
			if (window == null)
				throw new ArgumentNullException(nameof(window));

			if (window.Visibility == Visibility.Visible)
				window.Hide();
			else
				window.Show();
		}
	}
}