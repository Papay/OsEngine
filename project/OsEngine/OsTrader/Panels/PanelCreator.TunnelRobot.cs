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
            private readonly BotTabSimple bot2;

            private readonly Tunnel tunnel;
            private readonly Tunnel tunnel2;

            private readonly int Volume1;
            private readonly int Volume2;
            private readonly int Volume3;
            private readonly int Volume4;
            private bool[] OpenVolume;

            private BotTradeRegime regime;
            private BotTradeRegime regime2;

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

                TabCreate(BotTabType.Simple);
                this.bot2 = this.TabsSimple[1];

                this.tunnel2 = new Tunnel(name + Tunnel.IndicatorName + "2", false);
                this.tunnel2.Lenght = this.TunnelLength.ValueInt;
                this.tunnel2.Width = this.TunnelWidth.ValueInt;

                this.tunnel2 = (Tunnel)this.bot2.CreateCandleIndicator(this.tunnel2, "Prime");
                this.tunnel2.Save();

                this.Volume1 = 1;
                this.Volume2 = 1;
                this.Volume3 = 1;
                this.Volume4 = 1;

                this.OpenVolume = new bool[] {false, false, false, false };
                
                
                this.regime = BotTradeRegime.Off;
                this.regime2 = BotTradeRegime.On;

                this.bot.CandleFinishedEvent += this.OnCandleFinishedEvent;
                this.bot2.CandleFinishedEvent += this.OnCandleFinishedEvent2;

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

                    this.tunnel2.Lenght = this.TunnelLength.ValueInt;
                    this.tunnel2.Width = this.TunnelWidth.ValueInt;
                    this.tunnel2.Save();
                    this.tunnel2.Reload();
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

            private void OnCandleFinishedEvent2(List<Candle> candles)
            {
                if (this.regime2 == BotTradeRegime.Off)
                    return;

                if (ServerMaster.StartProgram == ServerStartProgramm.IsOsTrader
                    && (DateTime.Now.Hour < 9 || DateTime.Now.Hour > 23))
                {
                    return;
                }

                if (candles.Count <= this.tunnel2.Lenght + 1)
                    return;

                if (this.tunnel2.ValuesUp?.Count < candles.Count)
                    return;

                List<Position> openPositions = this.bot2.PositionsOpenAll;

                if (openPositions?.Count == 0)
                {
                    this.LogicOpenPosition2(candles, openPositions);
                }
                else
                {
                    for (int i = 0; i < openPositions?.Count; i++)
                    {
                        this.LogicClosePosition2(candles, openPositions[i]);
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
                decimal tunnelUp = this.tunnel.ValuesUp[this.tunnel.ValuesUp.Count - 1];
                decimal tunnelDown = this.tunnel.ValuesDown[this.tunnel.ValuesDown.Count - 1];
                decimal slippage = this.Slippage.ValueInt * this.bot.Securiti.PriceStep;

                if (lastCandle.High >= tunnelUp)
                {
                    this.bot.BuyAtLimit(this.Volume1 + this.Volume2 + this.Volume3 + this.Volume4, tunnelUp + slippage);
                    this.OpenVolume = new bool[] { true, true, true, true };
                }
                    
                if (lastCandle.Low <= tunnelDown)
                {
                    this.bot.SellAtLimit(this.Volume1 + this.Volume2 + this.Volume3 + this.Volume4, tunnelDown - slippage);
                    this.OpenVolume = new bool[] { true, true, true, true };
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
                decimal profit2 = profit * 2.2m;
                decimal profit3 = profit * 3.3m;
                decimal profit4 = profit * 4.4m;

                switch (openPosition.Direction)
                {
                    case Side.Buy:
                        if (lastCandle.Close <= tunnelDown && openPosition.OpenVolume > 0)
                        {
                            this.bot.CloseAtMarket(openPosition, openPosition.OpenVolume);

                            this.LogicOpenPosition(candles, null);
                        }
                        else
                        {
                            if (this.OpenVolume[3])
                            {
                                if (lastCandle.High >= tunnelUp + profit4)
                                {
                                    this.bot.CloseAtLimit(openPosition, tunnelUp + profit4 - slippage, this.Volume4);
                                    this.OpenVolume[3] = false;
                                }
                            }
                            if (this.OpenVolume[2])
                            {
                                if (lastCandle.High >= tunnelUp + profit3)
                                {
                                    this.bot.CloseAtLimit(openPosition, tunnelUp + profit3 - slippage, this.Volume3);
                                    this.OpenVolume[2] = false;
                                }
                            }
                            if (OpenVolume[1])
                            {
                                if (lastCandle.High >= tunnelUp + profit2)
                                {
                                    this.bot.CloseAtLimit(openPosition, tunnelUp + profit2 - slippage, this.Volume2);
                                    this.OpenVolume[1] = false;
                                }
                            }
                            if (OpenVolume[0])
                            {
                                if (lastCandle.High >= tunnelUp + profit1)
                                {
                                    this.bot.CloseAtLimit(openPosition, tunnelUp + profit1 - slippage, this.Volume1);
                                    this.OpenVolume[0] = false;
                                }
                            }
                            else
                            {
                                this.bot.CloseAtTrailingStop(openPosition, openPosition.EntryPrice, openPosition.EntryPrice);
                            }
                        }
                        break;
                    case Side.Sell:
                        if (lastCandle.Close >= tunnelUp && openPosition.OpenVolume > 0)
                        {
                            this.bot.CloseAtMarket(openPosition, openPosition.OpenVolume);
                            
                            this.LogicOpenPosition(candles, null);
                        }
                        else
                        {
                            if (this.OpenVolume[3])
                            {
                                if (lastCandle.Low <= tunnelDown - profit4)
                                {
                                    this.bot.CloseAtLimit(openPosition, tunnelDown - profit4 + slippage, this.Volume4);
                                    this.OpenVolume[3] = false;
                                }
                            }
                            if (OpenVolume[2])
                            {
                                if (lastCandle.Low <= tunnelDown - profit3)
                                {
                                    this.bot.CloseAtLimit(openPosition, tunnelDown - profit3 + slippage, this.Volume3);
                                    this.OpenVolume[2] = false;
                                }
                            }
                            if (OpenVolume[1])
                            {
                                if (lastCandle.Low <= tunnelDown - profit2)
                                {
                                    this.bot.CloseAtLimit(openPosition, tunnelDown - profit2 + slippage, this.Volume2);
                                    this.OpenVolume[1] = false;
                                }
                            }
                            if (OpenVolume[0])
                            {
                                if (lastCandle.Low <= tunnelDown - profit1)
                                {
                                    this.bot.CloseAtLimit(openPosition, tunnelDown - profit1 + slippage, this.Volume1);
                                    this.OpenVolume[0] = false;
                                }
                            }
                            else
                            {
                                this.bot.CloseAtTrailingStop(openPosition, openPosition.EntryPrice, openPosition.EntryPrice);
                            }
                        }
                        break;
                }
            }

            /// <summary>
            /// логика открытия позиции
            /// </summary>
            /// <param name="candles"></param>
            /// <param name="openPositions"></param>
            private void LogicOpenPosition2(List<Candle> candles, List<Position> openPositions)
            {
                Candle lastCandle = candles[candles.Count - 1];
                decimal tunnelUp = this.tunnel2.ValuesUp[this.tunnel2.ValuesUp.Count - 1];
                decimal tunnelDown = this.tunnel2.ValuesDown[this.tunnel2.ValuesDown.Count - 1];
                decimal slippage = this.Slippage.ValueInt * this.bot2.Securiti.PriceStep;

                decimal profit = decimal.Multiply(lastCandle.Close, this.Profit.ValueDecimal);
                decimal profit1 = profit * 0.9m;

                if (lastCandle.High > tunnelUp + profit1)
                {
                    int volume = this.Volume1 + this.Volume2 + this.Volume3 + this.Volume4;
                    this.bot2.SellAtStop(volume, tunnelUp + profit1 - slippage, tunnelUp + profit1, StopActivateType.LowerOrEqyal);
                }

                if (lastCandle.Low < tunnelDown - profit1)
                {
                    int volume = this.Volume1 + this.Volume2 + this.Volume3 + this.Volume4;
                    this.bot2.BuyAtStop(volume, tunnelDown - profit1 + slippage, tunnelDown - profit1, StopActivateType.HigherOrEqual);
                }
            }

            /// <summary>
            /// логика закрытия позиции
            /// </summary>
            /// <param name="candles"></param>
            /// <param name="openPosition"></param>
            private void LogicClosePosition2(List<Candle> candles, Position openPosition)
            {
                Candle lastCandle = candles[candles.Count - 1];
                decimal tunnelUp = this.tunnel2.ValuesUp[this.tunnel2.ValuesUp.Count - 1];
                decimal tunnelDown = this.tunnel2.ValuesDown[this.tunnel2.ValuesDown.Count - 1];
                decimal slippage = this.Slippage.ValueInt * this.bot2.Securiti.PriceStep;

                decimal profit = decimal.Multiply(lastCandle.Close, this.Profit.ValueDecimal);
                decimal profit1 = profit * 0.9m;

                switch (openPosition.Direction)
                {
                    case Side.Buy:
                        this.bot2.CloseAtProfit(openPosition, tunnelUp, tunnelUp + slippage);
                        this.bot2.CloseAtStop(openPosition, openPosition.EntryPrice - profit1 * 0.5m, openPosition.EntryPrice + profit1 * 0.5m - slippage);
                        break;
                    case Side.Sell:
                        this.bot2.CloseAtProfit(openPosition, tunnelDown, tunnelDown - slippage);
                        this.bot2.CloseAtStop(openPosition, openPosition.EntryPrice + profit1 * 0.5m, openPosition.EntryPrice + profit1 * 0.5m + slippage);
                        break;
                }
            }
        }
    }
}
