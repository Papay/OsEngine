/*
 *Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
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
            //private readonly BotTabSimple bot2;

            private readonly Tunnel tunnel;
            //private readonly Tunnel tunnel2;

            private readonly int Volume1;
            private readonly int Volume2;
            private readonly int Volume3;
            private readonly int Volume4;
            private readonly int Volume5;

            private BotTradeRegime regime;
            //private BotTradeRegime regime2;

            public StrategyParameterDecimal Profit;
            public StrategyParameterInt TunnelLength;
            public StrategyParameterInt TunnelWidth;
            private StrategyParameterInt Slippage;

            public TunnelRobot(string name)
                : base(name)
            {
                this.Profit = CreateParameter("Profit", 0.0012m, 0.001m, 0.10m, 0.0001m);
                this.Slippage = CreateParameter("Slippage", 1, 0, 10, 1);
                this.TunnelLength = CreateParameter("Tunnel.Length", 190, 20, 300, 5);
                this.TunnelWidth = CreateParameter("Tunnel.Width", 60, 10, 500, 10);

                TabCreate(BotTabType.Simple);
                this.bot = this.TabsSimple[0];

                this.tunnel = new Tunnel(name + Tunnel.IndicatorName + "1", false);
                this.tunnel.Lenght = this.TunnelLength.ValueInt;
                this.tunnel.Width = this.TunnelWidth.ValueInt;

                this.tunnel = (Tunnel)this.bot.CreateCandleIndicator(this.tunnel, "Prime");
                this.tunnel.Save();

                //TabCreate(BotTabType.Simple);
                //this.bot2 = this.TabsSimple[1];

                //this.tunnel2 = new Tunnel(name + Tunnel.IndicatorName + "2", false);
                //this.tunnel2.Lenght = this.TunnelLength.ValueInt;
                //this.tunnel2.Width = this.TunnelWidth.ValueInt;

                //this.tunnel2 = (Tunnel)this.bot2.CreateCandleIndicator(this.tunnel2, "Prime");
                //this.tunnel2.Save();

                this.Volume1 = 1;
                this.Volume2 = 1;
                this.Volume3 = 1;
                this.Volume4 = 1;
                this.Volume5 = 2;

                this.regime = BotTradeRegime.On;
                //this.regime2 = BotTradeRegime.On;

                this.bot.CandleFinishedEvent += this.OnCandleFinishedEvent;
                //this.bot2.CandleFinishedEvent += this.OnCandleFinishedEvent2;

                this.DeleteEvent += this.OnDeleteEvent;
                this.ParametrsChangeByUser += this.OnParametrsChangeByUser;
                //this.Load();
            }

            private void OnParametrsChangeByUser()
            {
                if (this.tunnel.Lenght != this.TunnelLength.ValueInt || this.tunnel.Width != this.TunnelWidth.ValueInt)
                {
                    this.tunnel.Lenght = this.TunnelLength.ValueInt;
                    this.tunnel.Width = this.TunnelWidth.ValueInt;
                    this.tunnel.Save();
                    this.tunnel.Reload();

                    //this.tunnel2.Lenght = this.TunnelLength.ValueInt;
                    //this.tunnel2.Width = this.TunnelWidth.ValueInt;
                    //this.tunnel2.Save();
                    //this.tunnel2.Reload();
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

                if (candles.Count <= this.tunnel.Lenght + 1)
                    return;

                if (this.tunnel.ValuesUp?.Count < candles.Count)
                    return;

                List<Position> openPositions = this.bot.PositionsOpenAll;

                if (openPositions?.Count == 0)
                {
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

            //private void OnCandleFinishedEvent2(List<Candle> candles)
            //{
            //    if (this.regime2 == BotTradeRegime.Off)
            //        return;

            //    if (ServerMaster.StartProgram == ServerStartProgramm.IsOsTrader
            //        && (DateTime.Now.Hour < 9 || DateTime.Now.Hour > 23))
            //    {
            //        return;
            //    }

            //    if (candles.Count <= this.tunnel2.Lenght + 1)
            //        return;

            //    if (this.tunnel2.ValuesUp?.Count < candles.Count)
            //        return;

            //    List<Position> openPositions = this.bot2.PositionsOpenAll;

            //    if (openPositions?.Count == 0)
            //    {
            //        this.LogicOpenPosition2(candles, openPositions);
            //    }
            //    else
            //    {
            //        for (int i = 0; i < openPositions?.Count; i++)
            //        {
            //            this.LogicClosePosition2(candles, openPositions[i]);
            //        }
            //    }
            //}

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
                decimal tunnelUp = this.tunnel.ValuesUp[this.tunnel.ValuesUp.Count - 1];
                decimal tunnelDown = this.tunnel.ValuesDown[this.tunnel.ValuesDown.Count - 1];
                decimal slippage = this.Slippage.ValueInt * this.bot.Securiti.PriceStep;

                if (lastCandle.High >= tunnelUp)
                {
                    this.bot.BuyAtMarket(this.Volume1, "L1");
                    this.bot.BuyAtMarket(this.Volume2, "L2");
                    this.bot.BuyAtMarket(this.Volume3, "L3");
                    this.bot.BuyAtMarket(this.Volume4, "L4");
                    this.bot.BuyAtMarket(this.Volume5, "L5");

                    //this.bot.BuyAtLimit(this.Volume1, tunnelUp + slippage, "L1");
                    //this.bot.BuyAtLimit(this.Volume2, tunnelUp + slippage, "L2");
                    //this.bot.BuyAtLimit(this.Volume3, tunnelUp + slippage, "L3");
                    //this.bot.BuyAtLimit(this.Volume4, tunnelUp + slippage, "L4");
                }
                    
                if (lastCandle.Low <= tunnelDown)
                {
                    this.bot.SellAtMarket(this.Volume1, "L1");
                    this.bot.SellAtMarket(this.Volume2, "L2");
                    this.bot.SellAtMarket(this.Volume3, "L3");
                    this.bot.SellAtMarket(this.Volume4, "L4");
                    this.bot.SellAtMarket(this.Volume5, "L5");

                    //this.bot.SellAtLimit(this.Volume1, tunnelDown - slippage, "L1");
                    //this.bot.SellAtLimit(this.Volume2, tunnelDown - slippage, "L2");
                    //this.bot.SellAtLimit(this.Volume3, tunnelDown - slippage, "L3");
                    //this.bot.SellAtLimit(this.Volume4, tunnelDown - slippage, "L4");
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
                decimal tunnelUp = this.tunnel.ValuesUp.Last();
                decimal tunnelDown = this.tunnel.ValuesDown.Last();
                decimal slippage = this.Slippage.ValueInt * this.bot.Securiti.PriceStep;

                decimal profit = decimal.Multiply(lastCandle.Close, this.Profit.ValueDecimal);
                decimal profit1 = profit * 1.0m;
                decimal profit2 = profit * 3.0m;
                decimal profit3 = profit * 5.0m;
                decimal profit4 = profit * 7.0m;

                switch (openPosition.Direction)
                {
                    case Side.Buy:
                        switch (openPosition.SignalTypeOpen)
                        {
                            case "L1":
                                this.bot.CloseAtProfit(openPosition, tunnelUp + profit1, tunnelUp + profit1 - slippage);
                                this.bot.CloseAtStop(openPosition, tunnelDown, tunnelDown - slippage);
                                break;
                            case "L2":
                                this.bot.CloseAtProfit(openPosition, tunnelUp + profit2, tunnelUp + profit2 - slippage);
                                this.bot.CloseAtStop(openPosition, tunnelDown, tunnelDown - slippage);
                                break;
                            case "L3":
                                this.bot.CloseAtProfit(openPosition, tunnelUp + profit3, tunnelUp + profit3 - slippage);
                                this.bot.CloseAtStop(openPosition, tunnelDown, tunnelDown - slippage);
                                break;
                            case "L4":
                                this.bot.CloseAtProfit(openPosition, tunnelUp + profit4, tunnelUp + profit4 - slippage);
                                this.bot.CloseAtStop(openPosition, tunnelDown, tunnelDown - slippage);
                                break;
                            case "L5":
                                if (this.bot.PositionsOpenAll.All(position => position.SignalTypeOpen == "L5"))
                                {
                                    decimal stopPrice = (tunnelUp + tunnelDown) * 0.5m;
                                    this.bot.CloseAtStop(openPosition, stopPrice, stopPrice - slippage);
                                }
                                else
                                {
                                    this.bot.CloseAtStop(openPosition, tunnelDown, tunnelDown - slippage);
                                }
                                break;
                            default:
                                throw new InvalidOperationException($"Unexpectted long position with signal type open: {openPosition.SignalTypeOpen}");
                        }
                        break;
                    case Side.Sell:
                        switch (openPosition.SignalTypeOpen)
                        {
                            case "L1":
                                this.bot.CloseAtProfit(openPosition, tunnelDown - profit1, tunnelDown - profit1 + slippage);
                                this.bot.CloseAtStop(openPosition, tunnelUp, tunnelUp + slippage);
                                break;
                            case "L2":
                                this.bot.CloseAtProfit(openPosition, tunnelDown - profit2, tunnelDown - profit2 + slippage);
                                this.bot.CloseAtStop(openPosition, tunnelUp, tunnelUp + slippage);
                                break;
                            case "L3":
                                this.bot.CloseAtProfit(openPosition, tunnelDown - profit3, tunnelDown - profit3 + slippage);
                                this.bot.CloseAtStop(openPosition, tunnelUp, tunnelUp + slippage);
                                break;
                            case "L4":
                                this.bot.CloseAtProfit(openPosition, tunnelDown - profit4, tunnelDown - profit4 + slippage);
                                this.bot.CloseAtStop(openPosition, tunnelUp, tunnelUp + slippage);
                                break;
                            case "L5":
                                if (this.bot.PositionsOpenAll.All(position => position.SignalTypeOpen == "L5"))
                                {
                                    decimal stopPrice = (tunnelUp + tunnelDown) * 0.5m;
                                    this.bot.CloseAtStop(openPosition, stopPrice, stopPrice + slippage);
                                }
                                else
                                {
                                    this.bot.CloseAtStop(openPosition, tunnelUp, tunnelUp + slippage);
                                }
                                break;
                            default:
                                throw new InvalidOperationException($"Unexpectted short position with signal type open: {openPosition.SignalTypeOpen}");
                        }
                        break;
                }
            }

            /// <summary>
            /// логика открытия позиции
            /// </summary>
            /// <param name="candles"></param>
            /// <param name="openPositions"></param>
            //private void LogicOpenPosition2(List<Candle> candles, List<Position> openPositions)
            //{
            //    Candle lastCandle = candles[candles.Count - 1];
            //    decimal tunnelUp = this.tunnel2.ValuesUp[this.tunnel2.ValuesUp.Count - 1];
            //    decimal tunnelDown = this.tunnel2.ValuesDown[this.tunnel2.ValuesDown.Count - 1];
            //    decimal slippage = this.Slippage.ValueInt * this.bot2.Securiti.PriceStep;

            //    decimal profit = decimal.Multiply(lastCandle.Close, this.Profit.ValueDecimal);
            //    decimal profit1 = profit * 0.5m;
            //    decimal profit2 = profit * 5.5m;

            //    if (lastCandle.High > tunnelUp + profit1)
            //    {
            //        int volume = this.Volume1 + this.Volume2 + this.Volume3 + this.Volume4;
            //        this.bot2.SellAtStop(volume, tunnelUp + profit1 - slippage, tunnelUp + profit1, StopActivateType.LowerOrEqyal);
            //    }
            //    if (lastCandle.High > tunnelUp + profit2)
            //    {
            //        int volume = this.Volume1 + this.Volume2 + this.Volume3 + this.Volume4;
            //        this.bot2.SellAtStop(volume, tunnelUp + profit2 - slippage, tunnelUp + profit2, StopActivateType.LowerOrEqyal);
            //    }

            //    if (lastCandle.Low < tunnelDown - profit1)
            //    {
            //        int volume = this.Volume1 + this.Volume2 + this.Volume3 + this.Volume4;
            //        this.bot2.BuyAtStop(volume, tunnelDown - profit1 + slippage, tunnelDown - profit1, StopActivateType.HigherOrEqual);
            //    }
            //    if (lastCandle.Low < tunnelDown - profit2)
            //    {
            //        int volume = this.Volume1 + this.Volume2 + this.Volume3 + this.Volume4;
            //        this.bot2.BuyAtStop(volume, tunnelDown - profit2 + slippage, tunnelDown - profit2, StopActivateType.HigherOrEqual);
            //    }
            //}

            /// <summary>
            /// логика закрытия позиции
            /// </summary>
            /// <param name="candles"></param>
            /// <param name="openPosition"></param>
            //private void LogicClosePosition2(List<Candle> candles, Position openPosition)
            //{
            //    Candle lastCandle = candles[candles.Count - 1];
            //    decimal tunnelUp = this.tunnel2.ValuesUp[this.tunnel2.ValuesUp.Count - 1];
            //    decimal tunnelDown = this.tunnel2.ValuesDown[this.tunnel2.ValuesDown.Count - 1];
            //    decimal slippage = this.Slippage.ValueInt * this.bot2.Securiti.PriceStep;

            //    decimal profit = decimal.Multiply(lastCandle.Close, this.Profit.ValueDecimal);

            //    switch (openPosition.Direction)
            //    {
            //        case Side.Buy:
            //            this.bot2.CloseAtProfit(openPosition, tunnelUp, tunnelUp + slippage);
            //            this.bot2.CloseAtStop(openPosition, openPosition.EntryPrice - profit * 0.5m, openPosition.EntryPrice + profit * 0.5m - slippage);
            //            break;
            //        case Side.Sell:
            //            this.bot2.CloseAtProfit(openPosition, tunnelDown, tunnelDown - slippage);
            //            this.bot2.CloseAtStop(openPosition, openPosition.EntryPrice + profit * 0.5m, openPosition.EntryPrice + profit * 0.5m + slippage);
            //            break;
            //    }
            //}
        }
    }
}
