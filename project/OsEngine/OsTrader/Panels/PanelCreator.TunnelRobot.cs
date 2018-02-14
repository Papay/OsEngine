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
            private StrategyParameterString Regime; // BotTradeRegime

            private readonly MovingAverage sma;
            private readonly Atr atr;

            private StrategyParameterInt Volume1;
            private StrategyParameterInt Volume2;
            private StrategyParameterInt Volume3;
            private StrategyParameterInt Volume4;
        
            public StrategyParameterDecimal Profit1;
            public StrategyParameterDecimal Profit2;
            public StrategyParameterDecimal Profit3;

            public StrategyParameterInt TunnelLength;
            public StrategyParameterInt TunnelWidth;
            public StrategyParameterInt Slippage;
            public StrategyParameterInt Stoploss;

            public StrategyParameterInt AtrLength;
            public StrategyParameterInt AtrFilter;

            public TunnelRobot(string name)
                : base(name)
            {
                this.Regime = CreateParameter("Regime", Enum.GetName(typeof(BotTradeRegime), BotTradeRegime.Off), Enum.GetNames(typeof(BotTradeRegime)));
                this.Profit1 = CreateParameter("Profit1", 0.2m, 0.10m, 1.0m, 0.01m);
                this.Profit2 = CreateParameter("Profit2", 0.3m, 0.10m, 1.0m, 0.01m);
                this.Profit3 = CreateParameter("Profit3", 0.4m, 0.10m, 1.0m, 0.01m);
                this.Slippage = CreateParameter("Slippage", 1, 1, 100, 1);
                this.TunnelLength = CreateParameter("Tunnel.Length", 30, 10, 200, 5);
                this.TunnelWidth = CreateParameter("Tunnel.Width", 60, 20, 300, 10);
                this.Stoploss = CreateParameter("Stoploss", 90, 5, 100, 5);
                this.Volume1 = CreateParameter("Volume1", 1, 1, 3, 1);
                this.Volume2 = CreateParameter("Volume2", 1, 1, 3, 1);
                this.Volume3 = CreateParameter("Volume3", 1, 1, 3, 1);
                this.Volume4 = CreateParameter("Volume4", 2, 1, 3, 1);

                this.AtrLength = CreateParameter("Atr.Length", 14, 5, 50, 1);
                this.AtrFilter = CreateParameter("Atr.Filter", 10, 10, 50, 1);

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

                this.atr = new Atr(name + "ATR", false)
                {
                    Lenght = this.AtrLength.ValueInt
                };
                this.atr = (Atr)this.bot.CreateCandleIndicator(this.atr, "AtrAre");
                this.atr.Save();

                this.bot.CandleFinishedEvent += this.OnCandleFinishedEvent;
                this.bot.PositionOpeningSuccesEvent += this.OnPositionOpeningSuccesEvent;

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

                if (this.atr.Lenght != this.AtrLength.ValueInt)
                {
                    this.atr.Lenght = this.AtrLength.ValueInt;
                    this.atr.Save();
                    this.atr.Reload();
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
                        reader.Close();
                    }
                }
                catch (Exception)
                {
                    // отправить в лог
                }
            }

            public override string GetNameStrategyType()
            {
                return RobotName;
            }

            public override void ShowIndividualSettingsDialog()
            {
            }

            private void OnPositionOpeningSuccesEvent(Position position)
            {
                decimal smaValue = this.sma.Values.Last();
                decimal tunnelUp = smaValue + this.TunnelWidth.ValueInt * 0.5m;
                decimal tunnelDown = smaValue - this.TunnelWidth.ValueInt * 0.5m;
                decimal slippage = this.Slippage.ValueInt * this.bot.Securiti.PriceStep;

                switch (position.Direction)
                {
                    case Side.Buy:
                        decimal longStopPrice = tunnelUp - this.TunnelWidth.ValueInt * (this.Stoploss.ValueInt / 100m);
                        this.bot.CloseAtStop(position, longStopPrice, longStopPrice - slippage);
                        break;
                    case Side.Sell:
                        decimal shortStopPrice = tunnelDown + this.TunnelWidth.ValueInt * (this.Stoploss.ValueInt / 100m);
                        this.bot.CloseAtStop(position, shortStopPrice, shortStopPrice + slippage);
                        break;
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
                if (this.Regime.ValueString == Enum.GetName(typeof(BotTradeRegime), BotTradeRegime.Off))
                    return;

                if (ServerMaster.StartProgram == ServerStartProgramm.IsOsTrader
                    && (DateTime.Now.Hour < 9 || DateTime.Now.Hour > 23))
                {
                    return;
                }

                if (candles.Count <= this.sma.Lenght + 1)
                    return;

                List<Position> openPositions = this.bot.PositionsOpenAll;

                if (openPositions == null || openPositions.Count == 0)
                {
                    if (this.Regime.ValueString == Enum.GetName(typeof(BotTradeRegime), BotTradeRegime.OnlyClosePosition))
                        return;

                    this.LogicOpenPosition(candles, openPositions);
                }
                else
                {
                    for (int i = 0; i < openPositions.Count; i++)
                    {
                        this.LogicClosePosition(candles, openPositions[i]);
                    }
                }
            }

            /// <summary>
            /// логика открытия позиции
            /// </summary>
            /// <param name="candles"></param>
            /// <param name="openPositions"></param>
            private void LogicOpenPosition(List<Candle> candles, List<Position> openPositions)
            {
                Candle lastCandle = candles.Last();
                decimal smaValue = this.sma.Values.Last();
                decimal tunnelUp = smaValue + this.TunnelWidth.ValueInt * 0.5m;
                decimal tunnelDown = smaValue - this.TunnelWidth.ValueInt * 0.5m;
                decimal slippage = this.Slippage.ValueInt * this.bot.Securiti.PriceStep;

                decimal profit1 = decimal.Multiply(smaValue, this.Profit1.ValueDecimal / 100m);
                decimal profit2 = decimal.Multiply(smaValue, this.Profit2.ValueDecimal / 100m);
                decimal profit3 = decimal.Multiply(smaValue, this.Profit3.ValueDecimal / 100m);

                decimal atrValue = this.atr.Values.Last();

                if (atrValue <= this.AtrFilter.ValueInt)
                    return;

                if (lastCandle.High >= tunnelUp)
                {
                    if (this.Regime.ValueString == Enum.GetName(typeof(BotTradeRegime), BotTradeRegime.OnlyShort))
                        return;

                    if (lastCandle.Close < tunnelUp + profit1)
                    {
                        this.bot.BuyAtMarket(this.Volume1.ValueInt, "L1");
                    }
                    if (lastCandle.Close < tunnelUp + profit2)
                    {
                        this.bot.BuyAtMarket(this.Volume2.ValueInt, "L2");
                        this.bot.BuyAtMarket(this.Volume3.ValueInt, "L3");
                        this.bot.BuyAtMarket(this.Volume4.ValueInt, "L4");
                    }
                }
                    
                if (lastCandle.Low <= tunnelDown)
                {
                    if (this.Regime.ValueString == Enum.GetName(typeof(BotTradeRegime), BotTradeRegime.OnlyLong))
                        return;

                    if (lastCandle.Close > tunnelDown - profit1)
                    {
                        this.bot.SellAtMarket(this.Volume1.ValueInt, "L1");
                    }
                    if (lastCandle.Close > tunnelDown - profit2)
                    {
                        this.bot.SellAtMarket(this.Volume2.ValueInt, "L2");
                        this.bot.SellAtMarket(this.Volume3.ValueInt, "L3");
                        this.bot.SellAtMarket(this.Volume4.ValueInt, "L4");
                    }
                }
            }

            /// <summary>
            /// логика закрытия позиции
            /// </summary>
            /// <param name="candles"></param>
            /// <param name="position"></param>
            private void LogicClosePosition(List<Candle> candles, Position position)
            {
                Candle lastCandle = candles.Last();
                decimal smaValue = this.sma.Values.Last();
                decimal tunnelUp = smaValue + this.TunnelWidth.ValueInt * 0.5m;
                decimal tunnelDown = smaValue - this.TunnelWidth.ValueInt * 0.5m;
                decimal slippage = this.Slippage.ValueInt * this.bot.Securiti.PriceStep;

                decimal profit1 = decimal.Multiply(smaValue, this.Profit1.ValueDecimal / 100m);
                decimal profit2 = decimal.Multiply(smaValue, this.Profit2.ValueDecimal / 100m);
                decimal profit3 = decimal.Multiply(smaValue, this.Profit3.ValueDecimal / 100m);

                switch (position.Direction)
                {
                    case Side.Buy:
                        decimal longStopPrice = tunnelUp - this.TunnelWidth.ValueInt * (this.Stoploss.ValueInt / 100m);
                        switch (position.SignalTypeOpen)
                        {
                            case "L1":
                                if (lastCandle.High >= tunnelUp + profit1)
                                    this.bot.CloseAtTrailingStop(position, tunnelUp + profit1, tunnelUp + profit1 - slippage);
                                else
                                    this.bot.CloseAtStop(position, longStopPrice, longStopPrice - slippage);
                                break;
                            case "L2":
                                if (lastCandle.High >= tunnelUp + profit2)
                                    this.bot.CloseAtTrailingStop(position, tunnelUp + profit2, tunnelUp + profit2 - slippage);
                                else
                                    this.bot.CloseAtStop(position, longStopPrice, longStopPrice - slippage);
                                break;
                            case "L3":
                                if (lastCandle.High >= tunnelUp + profit3)
                                    this.bot.CloseAtTrailingStop(position, tunnelUp + profit3, tunnelUp + profit3 - slippage);
                                else
                                    this.bot.CloseAtStop(position, longStopPrice, longStopPrice - slippage);
                                break;
                            default:
                                this.bot.CloseAtStop(position, longStopPrice, longStopPrice - slippage);
                                break;
                        }
                        break;
                    case Side.Sell:
                        decimal shortStopPrice = tunnelDown + this.TunnelWidth.ValueInt * (this.Stoploss.ValueInt / 100m);
                        switch (position.SignalTypeOpen)
                        {
                            case "L1":
                                if (lastCandle.Low < tunnelDown - profit1)
                                    this.bot.CloseAtTrailingStop(position, tunnelDown - profit1, tunnelDown - profit1 + slippage);
                                else
                                    this.bot.CloseAtStop(position, shortStopPrice, shortStopPrice + slippage);
                                break;
                            case "L2":
                                if (lastCandle.Low < tunnelDown - profit2)
                                    this.bot.CloseAtTrailingStop(position, tunnelDown - profit2, tunnelDown - profit2 + slippage);
                                else
                                    this.bot.CloseAtStop(position, shortStopPrice, shortStopPrice + slippage);
                                break;
                            case "L3":
                                if (lastCandle.Low < tunnelDown - profit3)
                                    this.bot.CloseAtTrailingStop(position, tunnelDown - profit3, tunnelDown - profit3 + slippage);
                                else
                                    this.bot.CloseAtStop(position, shortStopPrice, shortStopPrice + slippage);
                                break;
                            default:
                                this.bot.CloseAtStop(position, shortStopPrice, shortStopPrice + slippage);
                                break;
                        }
                        break;
                }
            }
        }
    }
}
