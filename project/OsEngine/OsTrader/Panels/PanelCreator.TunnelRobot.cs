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
            private StrategyParameterDecimal Slippage;

            public TunnelRobot(string name)
                : base(name)
            {
                TabCreate(BotTabType.Simple);
                this.bot = this.TabsSimple[0];

                Tunnel indicator = new Tunnel(name + Tunnel.IndicatorName, false)
                {
                    Lenght = 100,
                    Width = 500
                };
                this.tunnel = (Tunnel)this.bot.CreateCandleIndicator(indicator, "Prime");
                this.tunnel.Save();

                this.Profit = new StrategyParameterDecimal("Profit", 0.010m, 0.002m, 0.010m, 0.001m);
                this.Slippage = new StrategyParameterDecimal("Slippage", 0.0001m, 0.0001m, 0.0005m, 0.0001m);

                this.Volume = 2;
                this.regime = BotTradeRegime.Off;

                this.bot.CandleFinishedEvent += this.OnCandleFinishedEvent;
                this.bot.CandleUpdateEvent += this.OnCandleUpdateEvent;
                this.DeleteEvent += this.OnDeleteEvent;

                this.Load();
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

            private void OnCandleUpdateEvent(List<Candle> candles)
            {
            }

            private void OnCandleFinishedEvent(List<Candle> candles)
            {
                if (candles.Count <= this.tunnel.Lenght + 1)
                    return;

                if (this.tunnel.ValuesUp?.Count < candles.Count)
                    return;

                List<Position> openPositions = this.bot.PositionsOpenAll;

                foreach (Position openPosition in openPositions)
                {
                    this.LogicClosePosition(candles, openPosition);
                }

                if (openPositions?.Count == 0)
                {
                    this.LogicOpenPosition(candles, openPositions);
                }
            }

            public override string GetNameStrategyType()
            {
                return RobotName;
            }

            public override void ShowIndividualSettingsDialog()
            {
                // TODO
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

                // лонг
                if (lastCandle.Close < tunnelUp)
                {
                    this.bot.BuyAtStop(this.Volume, tunnelUp, tunnelUp, StopActivateType.HigherOrEqual);
                }
                // шорт
                if (lastCandle.Close > tunnelDown)
                {
                    this.bot.SellAtStop(this.Volume, tunnelDown, tunnelDown, StopActivateType.LowerOrEqyal);
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
                decimal profit = decimal.Multiply(lastCandle.Close, this.Profit.ValueDecimal);
                decimal tunnelUp = tunnel.ValuesUp.Last();
                decimal tunnelDown = tunnel.ValuesDown.Last();

                switch (openPosition.Direction)
                {
                    case Side.Buy:
                        if (lastCandle.Low <= tunnelDown)
                        {
                            this.bot.CloseAtMarket(openPosition, openPosition.OpenVolume, "stoploss");
                        }
                        else if (lastCandle.High >= tunnelUp + profit && openPosition.OpenVolume > this.Volume / 2)
                        {
                            this.bot.CloseAtLimit(openPosition, tunnelUp + profit, this.Volume / 2, "takeprofit");
                        }
                        //else if (lastCandle.High >= tunnelUp + profit * 2)
                        //{
                        //    this.bot.CloseAtLimit(openPosition, tunnelUp + profit * 2, openPosition.OpenVolume);
                        //}
                        break;
                    case Side.Sell:
                        if (lastCandle.High >= tunnelUp)
                        {
                            this.bot.CloseAtMarket(openPosition, openPosition.OpenVolume, "stoploss");
                        }
                        else if (lastCandle.Low <= tunnelDown - profit && openPosition.OpenVolume > this.Volume / 2)
                        {
                            this.bot.CloseAtLimit(openPosition, tunnelDown - profit, this.Volume / 2, "takeprofit");
                        }
                        //else if (lastCandle.Low <= tunnelDown - profit * 2)
                        //{
                        //    this.bot.CloseAtLimit(openPosition, tunnelDown - profit * 2, this.Volume / 2);
                        //}
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
