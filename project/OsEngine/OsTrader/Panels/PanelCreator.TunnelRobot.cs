/*
 *Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
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

            private BotTabSimple bot;

            private Tunnel tunnel;

            private int Volume;
            private BotTradeRegime regime;

            public StrategyParameterDecimal Profit;
            public StrategyParameterInt TunnelLength;
            public StrategyParameterInt TunnelWidth;
            private StrategyParameterInt Slippage;

            public TunnelRobot(string name)
                : base(name)
            {
                this.Profit = CreateParameter("Profit", 0.0020m, 0.002m, 0.010m, 0.001m);
                this.Slippage = CreateParameter("Slippage", 1, 0, 10, 1);
                this.TunnelLength = CreateParameter("Tunnel.Length", 80, 20, 200, 5);
                this.TunnelWidth = CreateParameter("Tunnel.Width", 80, 10, 100, 10);

                TabCreate(BotTabType.Simple);
                this.bot = this.TabsSimple[0];

                this.tunnel = new Tunnel(name + Tunnel.IndicatorName, false)
                {
                    Lenght = this.TunnelLength.ValueInt,
                    Width = this.TunnelWidth.ValueInt
                };
                this.tunnel = (Tunnel)this.bot.CreateCandleIndicator(this.tunnel, "Prime");
                this.tunnel.Save();

                this.Volume = 1;

                this.regime = BotTradeRegime.Off;

                this.bot.CandleFinishedEvent += this.OnCandleFinishedEvent;

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
                    this.tunnel.Reload();
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
                        this.Volume = Convert.ToInt32(reader.ReadLine());
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

            public override string GetNameStrategyType()
            {
                return RobotName;
            }

            public override void ShowIndividualSettingsDialog()
            {
                TunnelRobotUi dialog = new TunnelRobotUi(this);
                dialog.ShowDialog();
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

                this.bot.BuyAtStop(this.Volume, tunnelUp + slippage, tunnelUp, StopActivateType.HigherOrEqual);
                this.bot.SellAtStop(this.Volume, tunnelDown, tunnelDown - slippage, StopActivateType.LowerOrEqyal);
            }

            /// <summary>
            /// логика закрытия позиции
            /// </summary>
            /// <param name="candles"></param>
            /// <param name="openPosition"></param>
            private void LogicClosePosition(List<Candle> candles, Position openPosition)
            {
                Candle lastCandle = candles.Last();
                decimal profit = decimal.Multiply(lastCandle.Close, this.Profit.ValueDecimal);
                decimal tunnelUp = this.tunnel.ValuesUp.Last();
                decimal tunnelDown = this.tunnel.ValuesDown.Last();
                decimal slippage = this.Slippage.ValueInt * this.bot.Securiti.PriceStep;

                switch (openPosition.Direction)
                {
                    case Side.Buy:
                        if (lastCandle.High >= tunnelUp + profit)
                        {
                            this.bot.CloseAtLimit(openPosition, tunnelUp + profit - slippage, openPosition.OpenVolume);
                        }
                        if (lastCandle.Close <= tunnelDown)
                        {
                            this.bot.CloseAtMarket(openPosition, openPosition.OpenVolume);
                        }
                        break;
                    case Side.Sell:
                        if (lastCandle.Low <= tunnelDown - profit)
                        {
                            this.bot.CloseAtLimit(openPosition, tunnelDown - profit + slippage, openPosition.OpenVolume);
                        }
                        if (lastCandle.Close >= tunnelUp)
                        {
                            this.bot.CloseAtMarket(openPosition, openPosition.OpenVolume);
                        }
                        break;
                }
            }
        }
    }
}
