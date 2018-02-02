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
        public class InTunnelRobot : BotPanel
        {
            public static string RobotName = "InTunnel Robot";

            private readonly BotTabSimple bot;
            private BotTradeRegime regime;

            private readonly MovingAverage sma;

            private StrategyParameterInt TunnelLength;
            private StrategyParameterInt TunnelWidth;
            private StrategyParameterInt Slippage;
            private StrategyParameterInt Volume;
            private StrategyParameterDecimal Stoploss;

            public InTunnelRobot(string name)
                : base(name)
            {
                TabCreate(BotTabType.Simple);
                this.bot = this.TabsSimple[0];

                this.TunnelLength = CreateParameter("Tunnel.Length", 30, 10, 200, 5);
                this.TunnelWidth = CreateParameter("Tunnel.Width", 80, 1, 500, 1);
                this.Slippage = CreateParameter("Slippage", 1, 1, 100, 1);
                this.Stoploss = CreateParameter("Stoploss", 0.11m, 0.11m, 10m, 0.01m);
                this.Volume = CreateParameter("Volume", 1, 1, 100, 1);

                this.sma = new MovingAverage(name + MovingAverage.IndicatorName, false)
                {
                    TypeCalculationAverage = MovingAverageTypeCalculation.Simple,
                    TypeIndicator = IndicatorOneCandleChartType.Line,
                    TypePointsToSearch = PriceTypePoints.Close,
                    Lenght = this.TunnelLength.ValueInt
                };
                this.sma = (MovingAverage)this.bot.CreateCandleIndicator(this.sma, "Prime");
                this.sma.Save();

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

            private void OnDeleteEvent()
            {
                if (File.Exists(@"Engine\" + this.NameStrategyUniq + @"SettingsBot.txt"))
                {
                    File.Delete(@"Engine\" + this.NameStrategyUniq + @"SettingsBot.txt");
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

                if (openPositions == null || openPositions.Count == 0)
                {
                    if (this.regime == BotTradeRegime.OnlyClosePosition)
                        return;

                    this.LogicOpenPosition(candles);
                }
                else
                {
                    for (int i = 0; i < openPositions.Count; i++)
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
            }

            private void LogicOpenPosition(List<Candle> candles)
            {
                Candle lastCandel = candles.Last();

                decimal smaValue = this.sma.Values.Last();
                decimal tunnelUp = smaValue + this.TunnelWidth.ValueInt * 0.5m;
                decimal tunnelDown = smaValue - this.TunnelWidth.ValueInt * 0.5m;
                decimal slippage = this.Slippage.ValueInt * this.bot.Securiti.PriceStep;
                decimal stoploss = smaValue * decimal.Divide(this.Stoploss.ValueDecimal, 100m);

                if (lastCandel.Close > tunnelUp + stoploss)
                {
                    this.bot.SellAtStop(this.Volume.ValueInt, tunnelUp + slippage, tunnelUp, StopActivateType.LowerOrEqyal);
                }
                if (lastCandel.Close < tunnelDown - stoploss)
                {
                    this.bot.BuyAtStop(this.Volume.ValueInt, tunnelDown + slippage, tunnelDown, StopActivateType.HigherOrEqual);
                }
            }

            private void LogicClosePosition(List<Candle> candles, Position position)
            {
                Candle lastCandel = candles.Last();

                decimal smaValue = this.sma.Values.Last();
                decimal tunnelUp = smaValue + this.TunnelWidth.ValueInt * 0.5m;
                decimal tunnelDown = smaValue - this.TunnelWidth.ValueInt * 0.5m;
                decimal slippage = this.Slippage.ValueInt * this.bot.Securiti.PriceStep;
                decimal stoploss = smaValue * decimal.Divide(this.Stoploss.ValueDecimal, 100m);

                switch (position.Direction)
                {
                    case Side.Buy:
                        if (lastCandel.High > tunnelUp)
                        {
                            this.bot.CloseAtTrailingStop(position, tunnelUp, tunnelUp - slippage);
                        }
                        else
                        {
                            decimal longStopPrice = position.EntryPrice - stoploss;
                            this.bot.CloseAtStop(position, longStopPrice, longStopPrice - slippage);
                        }
                        break;

                    case Side.Sell:
                        if (lastCandel.Low < tunnelDown)
                        {
                            this.bot.CloseAtTrailingStop(position, tunnelDown, tunnelDown + slippage);
                        }
                        else
                        {
                            decimal longShortPrice = position.EntryPrice + stoploss;
                            this.bot.CloseAtStop(position, longShortPrice, longShortPrice + slippage);
                        }
                        break;
                }
            }
        }
    }
}