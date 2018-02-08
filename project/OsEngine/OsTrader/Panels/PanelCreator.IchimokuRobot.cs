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
        public class IchimokuRobot : BotPanel
        {
            public static string RobotName = "Ichimoku Robot";

            private readonly BotTabSimple bot;
            private StrategyParameterString Regime; // BotTradeRegime

            private StrategyParameterInt Volume;
            public StrategyParameterInt Slippage;
            public StrategyParameterDecimal Stoploss;

            private readonly Ichimoku ichimoku;

            public IchimokuRobot(string name)
                : base(name)
            {
                TabCreate(BotTabType.Simple);
                this.bot = this.TabsSimple[0];

                this.Regime = CreateParameter("Regime", Enum.GetName(typeof(BotTradeRegime), BotTradeRegime.Off), Enum.GetNames(typeof(BotTradeRegime)));
                this.Slippage = CreateParameter("Slippage", 1, 1, 100, 1);
                this.Stoploss = CreateParameter("Stoploss", 0.11m, 0.10m, 10m, 0.01m);
                this.Volume = CreateParameter("Volume", 1, 1, 100, 1);

                this.ichimoku = new Ichimoku(name + Ichimoku.IndicatorName, false) { };
                this.ichimoku = (Ichimoku)this.bot.CreateCandleIndicator(this.ichimoku, "Prime");
                this.ichimoku.Save();

                this.bot.CandleFinishedEvent += this.OnCandleFinishedEvent;
                this.bot.PositionOpeningSuccesEvent += this.OnPositionOpeningSuccesEvent;

                this.DeleteEvent += this.OnDeleteEvent;
                this.ParametrsChangeByUser += this.OnParametrsChangeByUser;
            }

            private void OnPositionOpeningSuccesEvent(Position position)
            {
                decimal slippage = this.Slippage.ValueInt * this.bot.Securiti.PriceStep;
                decimal stoploss = position.EntryPrice * decimal.Divide(this.Stoploss.ValueDecimal, 100m);

                switch(position.Direction)
                {
                    case Side.Buy:
                        decimal longStopPrice = position.EntryPrice - stoploss;
                        this.bot.CloseAtStop(position, longStopPrice, longStopPrice - slippage);
                        break;
                    case Side.Sell:
                        decimal longShortPrice = position.EntryPrice + stoploss;
                        this.bot.CloseAtStop(position, longShortPrice, longShortPrice + slippage);
                        break;
                }

            }

            private void OnParametrsChangeByUser()
            {
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
                if (this.Regime.ValueString == Enum.GetName(typeof(BotTradeRegime), BotTradeRegime.Off))
                    return;

                if (ServerMaster.StartProgram == ServerStartProgramm.IsOsTrader
                    && (DateTime.Now.Hour < 9 || DateTime.Now.Hour > 23))
                {
                    return;
                }

                if (candles.Count <= this.ichimoku.LenghtFirst + 1)
                    return;

                List<Position> openPositions = this.bot.PositionsOpenAll;

                if (openPositions == null || openPositions.Count == 0)
                {
                    if (this.Regime.ValueString == Enum.GetName(typeof(BotTradeRegime), BotTradeRegime.OnlyClosePosition))
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
                Candle lastCandle = candles.Last();
                decimal slippage = this.Slippage.ValueInt * this.bot.Securiti.PriceStep;
                decimal stoploss = lastCandle.Close * decimal.Divide(this.Stoploss.ValueDecimal, 100m);
                decimal senkou1 = this.ichimoku.ValuesLineFirst_Senkkou_span_A.Last();
                decimal senkou2 = this.ichimoku.ValuesLineSecond_Senkou_span_B.Last();

                if (lastCandle.Close >= senkou1 && lastCandle.Close >= senkou2)
                {
                    this.bot.BuyAtMarket(this.Volume.ValueInt, "L1");
                    this.bot.BuyAtMarket(this.Volume.ValueInt, "L2");
                }
                if (lastCandle.Close <= senkou1 && lastCandle.Close <= senkou2)
                {
                    this.bot.SellAtMarket(this.Volume.ValueInt, "L1");
                    this.bot.SellAtMarket(this.Volume.ValueInt, "L2");
                }
            }

            private void LogicClosePosition(List<Candle> candles, Position position)
            {
                Candle lastCandle = candles.Last();

                decimal slippage = this.Slippage.ValueInt * this.bot.Securiti.PriceStep;
                decimal stoploss = position.EntryPrice * decimal.Divide(this.Stoploss.ValueDecimal, 100m);
                decimal senkou1 = this.ichimoku.ValuesLineFirst_Senkkou_span_A.Last();
                decimal senkou2 = this.ichimoku.ValuesLineSecond_Senkou_span_B.Last();
                decimal tenkan = this.ichimoku.ValuesLineRounded_Teken_sen.Last();

                switch (position.Direction)
                {
                    case Side.Buy:
                        switch(position.SignalTypeOpen)
                        {
                            case "L1":
                                if (lastCandle.Close > tenkan)
                                {
                                    this.bot.CloseAtTrailingStop(position, tenkan, tenkan - slippage);
                                }
                                else
                                if (lastCandle.Close < senkou1 && lastCandle.Close > senkou2 ||
                                    lastCandle.Close > senkou1 && lastCandle.Close < senkou2)
                                {
                                    this.bot.CloseAtMarket(position, position.OpenVolume);
                                }
                                break;
                            case "L2":
                                if (lastCandle.Close < senkou1 && lastCandle.Close > senkou2 ||
                                    lastCandle.Close > senkou1 && lastCandle.Close < senkou2)
                                {
                                    this.bot.CloseAtMarket(position, position.OpenVolume);
                                }
                                break;
                        }
                        break;

                    case Side.Sell:
                        switch (position.SignalTypeOpen)
                        {
                            case "L1":
                                if (lastCandle.Close < tenkan)
                                {
                                    this.bot.CloseAtTrailingStop(position, tenkan, tenkan + slippage);
                                }
                                else
                                if (lastCandle.Close < senkou1 && lastCandle.Close > senkou2 ||
                                    lastCandle.Close > senkou1 && lastCandle.Close < senkou2)
                                {
                                    this.bot.CloseAtMarket(position, position.OpenVolume);
                                }
                                break;
                            case "L2":
                                if (lastCandle.Close < senkou1 && lastCandle.Close > senkou2 ||
                                    lastCandle.Close > senkou1 && lastCandle.Close < senkou2)
                                {
                                    this.bot.CloseAtMarket(position, position.OpenVolume);
                                }
                                break;
                        }
                        break;
                }
            }
        }
    }
}
