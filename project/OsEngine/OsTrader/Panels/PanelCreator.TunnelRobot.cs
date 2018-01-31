/*
 *Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Shapes;
using OsEngine.Alerts;
using OsEngine.Charts;
using OsEngine.Charts.CandleChart.Elements;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Logging;
using OsEngine.Market;
using OsEngine.Market.Servers;
using OsEngine.Market.Servers.Optimizer;
using OsEngine.Market.Servers.Tester;
using OsEngine.OsTrader.Panels.PanelsGui;
using OsEngine.OsTrader.Panels.Tab;

namespace OsEngine.OsTrader.Panels
{
    public partial class PanelCreator
    {
        /// <summary>
        /// Трендовая стратегия основана на использовании индикатора Tunnel
        /// </summary>
        public class TunnelRobot : BotPanel
        {
            public static string RobotName = "Tunnel Robot";

            private readonly BotTabSimple bot;
        
            //private readonly Tunnel tunnel;
            private readonly MovingAverage sma;

            private readonly int Volume1;
            private readonly int Volume2;
            private readonly int Volume3;
            private readonly int Volume4;

            private BotTradeRegime regime;
        
            public StrategyParameterDecimal Profit;
            public StrategyParameterInt TunnelLength;
            public StrategyParameterInt TunnelWidth;
            public StrategyParameterInt Slippage;
            public StrategyParameterInt Stoploss;

            public TunnelRobot(string name)
                : base(name)
            {
                this.Profit = CreateParameter("Profit", 0.12m, 0.10m, 1.0m, 0.01m);
                this.Slippage = CreateParameter("Slippage", 2, 1, 100, 1);
                this.TunnelLength = CreateParameter("Tunnel.Length", 30, 20, 200, 5);
                this.TunnelWidth = CreateParameter("Tunnel.Width", 40, 20, 300, 10);
                this.Stoploss = CreateParameter("Stoploss", 75, 5, 100, 5);

                TabCreate(BotTabType.Simple);
                this.bot = this.TabsSimple[0];

                this.sma = new MovingAverage(name + MovingAverage.IndicatorName, false)
                {
                    TypeCalculationAverage = MovingAverageTypeCalculation.Simple,
                    TypeIndicator = IndicatorOneCandleChartType.Line,
                    TypePointsToSearch = PriceTypePoints.Close,
                    Lenght = this.TunnelLength.ValueInt
                };
                this.sma = (MovingAverage)this.bot.CreateCandleIndicator(this.sma, "Prime");
                this.sma.Save();

                this.Volume1 = 2;
                this.Volume2 = 1;
                this.Volume3 = 1;
                this.Volume4 = 2;

                this.regime = BotTradeRegime.On;

                this.bot.CandleFinishedEvent += this.OnCandleFinishedEvent;

                this.DeleteEvent += this.OnDeleteEvent;
                this.ParametrsChangeByUser += this.OnParametrsChangeByUser;
            }

            private void OnParametrsChangeByUser()
            {
                if (this.sma.Lenght != this.TunnelLength.ValueInt)
                {
                    this.sma.Lenght = this.TunnelLength.ValueInt;
                    this.sma.Save();
                    this.sma.Reload();
                }
            }

            /// <summary>
            /// загрузить настройки
            /// </summary>
            private void Load()
            {
                if (!File.Exists(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt"))
                {
                    return;
                }
                try
                {
                    using (StreamReader reader = new StreamReader(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt"))
                    {
                        Enum.TryParse(reader.ReadLine(), true, out this.regime);

                        reader.Close();
                    }
                }
                catch (Exception)
                {
                    // отправить в лог
                }
            }

            private void OnDeleteEvent()
            {
                if (File.Exists(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt"))
                {
                    File.Delete(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt");
                }
            }

            private void OnCandleFinishedEvent(List<Candle> candles)
            {
                if (this.regime == BotTradeRegime.Off)
                    return;

                if (ServerMaster.StartProgram == ServerStartProgramm.IsOsTrader 
                    && (DateTime.Now.Hour < 9 || DateTime.Now.Hour > 23))
                {
                    return;
                }

                if (candles.Count <= this.sma.Lenght + 1)
                    return;

                List<Position> openPositions = this.bot.PositionsOpenAll;

                if (openPositions?.Count == 0)
                {
                    if (this.regime == BotTradeRegime.OnlyClosePosition)
                        return;

                    this.LogicOpenPosition(candles, openPositions);
                }
                else
                {
                    for (int i = 0; i < openPositions?.Count; i++)
                    {
                        this.LogicClosePosition(candles, openPositions[i]);
                    }
                }
            }

            public override string GetNameStrategyType()
            {
                return RobotName;
            }

            public override void ShowIndividualSettingsDialog()
            {
                //TunnelRobotUi dialog = new TunnelRobotUi(this);
                //dialog.ShowDialog();
            }

            /// <summary>
            /// логика открытия позиции
            /// </summary>
            /// <param name="candles"></param>
            /// <param name="openPositions"></param>
            private void LogicOpenPosition(List<Candle> candles, List<Position> openPositions)
            {
                Candle lastCandle = candles[candles.Count - 1];
                decimal smaValue = this.sma.Values[this.sma.Values.Count - 1];
                decimal tunnelUp = smaValue + this.TunnelWidth.ValueInt * 0.5m;
                decimal tunnelDown = smaValue - this.TunnelWidth.ValueInt * 0.5m;
                decimal slippage = this.Slippage.ValueInt * this.bot.Securiti.PriceStep;

                decimal profit = decimal.Multiply(lastCandle.Close, this.Profit.ValueDecimal / 100m);
                decimal profit1 = profit * 1.0m;
                decimal profit2 = profit * 2.0m;
                decimal profit3 = profit * 4.0m;
                
                if (lastCandle.High >= tunnelUp)
                {
                    if (this.regime == BotTradeRegime.OnlyShort)
                        return;

                    if (lastCandle.Close < tunnelUp + profit1)
                        this.bot.BuyAtMarket(this.Volume1, "L1");
                    if (lastCandle.Close < tunnelUp + profit2)
                    {
                        this.bot.BuyAtMarket(this.Volume2, "L2");
                        this.bot.BuyAtMarket(this.Volume3, "L3");
                        this.bot.BuyAtMarket(this.Volume4, "L4");
                    }
                }
                    
                if (lastCandle.Low <= tunnelDown)
                {
                    if (this.regime == BotTradeRegime.OnlyLong)
                        return;

                    if (lastCandle.Close > tunnelDown - profit1)
                        this.bot.SellAtMarket(this.Volume1, "L1");
                    if (lastCandle.Close > tunnelDown - profit2)
                    {
                        this.bot.SellAtMarket(this.Volume2, "L2");
                        this.bot.SellAtMarket(this.Volume3, "L3");
                        this.bot.SellAtMarket(this.Volume4, "L4");
                    }
                }
            }

            /// <summary>
            /// логика закрытия позиции
            /// </summary>
            /// <param name="candles"></param>
            /// <param name="openPosition"></param>
            private void LogicClosePosition(List<Candle> candles, Position openPosition)
            {
                Candle lastCandle = candles.Last();
                decimal smaValue = this.sma.Values[this.sma.Values.Count - 1];
                decimal tunnelUp = smaValue + this.TunnelWidth.ValueInt * 0.5m;
                decimal tunnelDown = smaValue - this.TunnelWidth.ValueInt * 0.5m;
                decimal slippage = this.Slippage.ValueInt * this.bot.Securiti.PriceStep;

                decimal profit = decimal.Multiply(lastCandle.Close, this.Profit.ValueDecimal / 100m);
                decimal profit1 = profit * 1.0m;
                decimal profit2 = profit * 2.0m;
                decimal profit3 = profit * 4.0m;

                switch (openPosition.Direction)
                {
                    case Side.Buy:
                        decimal longStopPrice = tunnelUp - this.TunnelWidth.ValueInt * (this.Stoploss.ValueInt / 100m);
                        switch (openPosition.SignalTypeOpen)
                        {
                            case "L1":
                                if (lastCandle.High >= tunnelUp + profit1)
                                    this.bot.CloseAtTrailingStop(openPosition, tunnelUp + profit1, tunnelUp + profit1 - slippage);
                                else
                                    this.bot.CloseAtStop(openPosition, longStopPrice, longStopPrice - slippage);
                                break;
                            case "L2":
                                if (lastCandle.High >= tunnelUp + profit2)
                                    this.bot.CloseAtTrailingStop(openPosition, tunnelUp + profit2, tunnelUp + profit2 - slippage);
                                else
                                    this.bot.CloseAtStop(openPosition, longStopPrice, longStopPrice - slippage);
                                break;
                            case "L3":
                                if (lastCandle.High >= tunnelUp + profit3)
                                    this.bot.CloseAtTrailingStop(openPosition, tunnelUp + profit3, tunnelUp + profit3 - slippage);
                                else
                                    this.bot.CloseAtStop(openPosition, longStopPrice, longStopPrice - slippage);
                                break;
                            case "L4":
                                this.bot.CloseAtStop(openPosition, longStopPrice, longStopPrice - slippage);
                                break;
                            default:
                                throw new InvalidOperationException(
                                    $"Unexpectted long position with signal type open: {openPosition.SignalTypeOpen}");
                        }
                        break;
                    case Side.Sell:
                        decimal shortStopPrice = tunnelDown + this.TunnelWidth.ValueInt * (this.Stoploss.ValueInt / 100m);
                        switch (openPosition.SignalTypeOpen)
                        {
                            case "L1":
                                if (lastCandle.Low < tunnelDown - profit1)
                                    this.bot.CloseAtTrailingStop(openPosition, tunnelDown - profit1, tunnelDown - profit1 + slippage);
                                else
                                    this.bot.CloseAtStop(openPosition, shortStopPrice, shortStopPrice + slippage);
                                break;
                            case "L2":
                                if (lastCandle.Low < tunnelDown - profit2)
                                    this.bot.CloseAtTrailingStop(openPosition, tunnelDown - profit2, tunnelDown - profit2 + slippage);
                                else
                                    this.bot.CloseAtStop(openPosition, shortStopPrice, shortStopPrice + slippage);
                                break;
                            case "L3":
                                if (lastCandle.Low < tunnelDown - profit3)
                                    this.bot.CloseAtTrailingStop(openPosition, tunnelDown - profit3, tunnelDown - profit3 + slippage);
                                else
                                    this.bot.CloseAtStop(openPosition, shortStopPrice, shortStopPrice + slippage);
                                break;
                            case "L4":
                                this.bot.CloseAtStop(openPosition, shortStopPrice, shortStopPrice + slippage);
                                break;
                            default:
                                throw new InvalidOperationException(
                                    $"Unexpectted short position with signal type open: {openPosition.SignalTypeOpen}");
                        }
                        break;
                }
            }
        }
    }
}
