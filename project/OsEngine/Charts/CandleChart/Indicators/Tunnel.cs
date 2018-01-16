using System;
using System.Collections.Generic;
using System.Drawing;
using OsEngine.Entity;

namespace OsEngine.Charts.CandleChart.Indicators
{
    public class Tunnel : IIndicatorCandle
    {
        public Tunnel(bool canDelete, string name)
        {
            CanDelete = canDelete;
            Name = name;
        }

        public Tunnel(bool canDelete)
        {
            CanDelete = canDelete;
        }

        public IndicatorOneCandleChartType TypeIndicator { get; set; }
        public List<Color> Colors { get; }
        public List<List<decimal>> ValuesToChart { get; }
        public bool CanDelete { get; set; }
        public string NameSeries { get; set; }
        public string NameArea { get; set; }
        public string Name { get; set; }
        public bool PaintOn { get; set; }
        public void Save()
        {
            throw new NotImplementedException();
        }

        public void Load()
        {
            throw new NotImplementedException();
        }

        public void Delete()
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public void ShowDialog()
        {
            throw new NotImplementedException();
        }

        public event Action<IIndicatorCandle> NeadToReloadEvent;
        public void Process(List<Candle> candles)
        {
            throw new NotImplementedException();
        }
    }
}
