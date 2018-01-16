using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OsEngine.Entity;

namespace OsEngine.Charts.CandleChart.Indicators
{
    public class Tunnel : IIndicatorCandle
    {
        /// <summary>
        /// Название индикатора
        /// </summary>
        public static string IndicatorName = "Tunnel";

        /// <summary>
        /// конструктор с параметрами. Индикатор будет сохраняться
        /// </summary>
        /// <param name="unqueName">уникальное имя</param>
        /// <param name="canDelete">можно ли пользователю удалить индикатор с графика вручную</param>
        public Tunnel(string unqueName, bool canDelete)
        {
            CanDelete = canDelete;
            Name = unqueName;
            TypeIndicator = IndicatorOneCandleChartType.Line;
            TypePointsToSearch = PriceTypePoints.Close;
            ColorBase = Color.DeepSkyBlue;
            Lenght = 13;
            Width = 34;
            PaintOn = true;
            CanDelete = canDelete;

            Values = new List<decimal>();
            TunnelUp = new List<decimal>();
            TunnelDown = new List<decimal>();

            Load();
        }

        /// <summary>
        /// конструктор без параметров. Индикатор не будет сохраняться
        /// используется ТОЛЬКО для создания составных индикаторов
        /// не используйте его из слоя создания роботов!
        /// </summary>
        /// <param name="canDelete">можно ли пользователю удалить индикатор с графика вручную</param>
        public Tunnel(bool canDelete)
        {
            CanDelete = canDelete;
            Name = Guid.NewGuid().ToString();
            TypeIndicator = IndicatorOneCandleChartType.Line;
            TypePointsToSearch = PriceTypePoints.Close;
            ColorBase = Color.DeepSkyBlue;
            Lenght = 13;
            Width = 34;
            PaintOn = true;
            CanDelete = canDelete;

            Values = new List<decimal>();
            TunnelUp = new List<decimal>();
            TunnelDown = new List<decimal>();
        }

        public IndicatorOneCandleChartType TypeIndicator { get; set; }

        /// <summary>
        /// цвета для индикатора
        /// </summary>
        public List<Color> Colors
        {
            get
            {
                List<Color> colors = new List<Color>();
                colors.Add(ColorBase);
                colors.Add(ColorBase);
                return colors;
            }
        }

        /// <summary>
        /// все значения индикатора
        /// </summary>
        public List<List<decimal>> ValuesToChart
        {
            get
            {
                TunnelUp = Values.Select(v => decimal.Add(v, Width / (decimal)2)).ToList();
                TunnelDown = Values.Select(v => decimal.Subtract(v, Width / (decimal)2)).ToList();

                List<List<decimal>> list = new List<List<decimal>>();
                list.Add(TunnelUp);
                list.Add(TunnelDown);
                return list;
            }
        }

        public bool CanDelete { get; set; }
        public string NameSeries { get; set; }
        public string NameArea { get; set; }
        public string Name { get; set; }
        public bool PaintOn { get; set; }

        /// <summary>
        /// цвет индикатора
        /// </summary>
        public Color ColorBase { get; set; }

        /// <summary>
        /// длинна расчёта индикатора
        /// </summary>
        public int Lenght { get; set; }

        /// <summary>
        /// ширина туннеля индикатора
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// тип скользящей средней
        /// </summary>
        public MovingAverageTypeCalculation TypeCalculationAverage;

        /// <summary>
        /// по какой точке средняя будет строиться. По Open Close ...
        /// </summary>
        public PriceTypePoints TypePointsToSearch;

        /// <summary>
        /// скользящая средняя
        /// </summary>
        public List<decimal> Values { get; set; }

        /// <summary>
        /// верхняя линия туннеля
        /// </summary>
        public List<decimal> TunnelUp { get; set; }

        /// <summary>
        /// нижняя линия туннеля
        /// </summary>
        public List<decimal> TunnelDown { get; set; }

        /// <summary>
        /// сохранить настройки в файл
        /// </summary>
        public void Save()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(@"Engine\" + Name + @".txt", false))
                {
                    writer.WriteLine(ColorBase.ToArgb());
                    writer.WriteLine(Lenght);
                    writer.WriteLine(Width);
                    writer.WriteLine(PaintOn);
                    writer.WriteLine(TypeCalculationAverage);
                    writer.WriteLine(TypePointsToSearch);

                    writer.Close();
                }
            }
            catch (Exception)
            {
                // отправить в лог
            }
        }

        /// <summary>
        /// загрузить настройки из файла
        /// </summary>
        public void Load()
        {
            if (!File.Exists(@"Engine\" + Name + @".txt"))
            {
                return;
            }
            try
            {
                using (StreamReader reader = new StreamReader(@"Engine\" + Name + @".txt"))
                {
                    ColorBase = Color.FromArgb(Convert.ToInt32(reader.ReadLine()));
                    Lenght = Convert.ToInt32(reader.ReadLine());
                    Width = Convert.ToInt32(reader.ReadLine());
                    PaintOn = Convert.ToBoolean(reader.ReadLine());
                    Enum.TryParse(reader.ReadLine(), true, out TypeCalculationAverage);
                    Enum.TryParse(reader.ReadLine(), true, out TypePointsToSearch);

                    reader.ReadLine();

                    reader.Close();
                }
            }
            catch (Exception)
            {
                // отправить в лог
            }
        }

        /// <summary>
        /// удалить файл с настройками
        /// </summary>
        public void Delete()
        {
            if (File.Exists(@"Engine\" + Name + @".txt"))
            {
                File.Delete(@"Engine\" + Name + @".txt");
            }
        }

        /// <summary>
        /// удалить данные
        /// </summary>
        public void Clear()
        {
            if (Values != null)
            {
                Values.Clear();
                TunnelUp.Clear();
                TunnelDown.Clear();
            }
            _myCandles = null;
        }

        public void ShowDialog()
        {
            TunnelUi ui = new TunnelUi(this);
            ui.ShowDialog();

            if (ui.IsChange)
            {
                Reload();
            }
        }

        public void Reload()
        {
            if (_myValues != null)
            {
                ProcessAll(_myValues);
            }
            if (_myCandles != null)
            {
                ProcessAll(_myCandles);
            }

            if (NeadToReloadEvent != null)
            {
                NeadToReloadEvent(this);
            }
        }

        public event Action<IIndicatorCandle> NeadToReloadEvent;

        /// <summary>
        /// свечи по которым строится индикатор
        /// </summary>
        private List<decimal> _myValues;

        /// <summary>
        /// свечи по которым строится индикатор
        /// </summary>
        private List<Candle> _myCandles;

        /// <summary>
        /// прогрузить новыми свечками
        /// </summary>
        public void Process(List<Candle> candles)
        {
            if (candles == null)
            {
                return;
            }
            _myCandles = candles;
            if (Values != null &&
                Values.Count + 1 == candles.Count)
            {
                ProcessOne(candles);
            }
            else if (Values != null &&
                     Values.Count == candles.Count)
            {
                ProcessLast(candles);
            }
            else
            {
                ProcessAll(candles);
            }
        }

        /// <summary>
        /// прогрузить только последнюю свечку
        /// </summary>
        private void ProcessOne(List<Candle> candles)
        {
            if (candles == null)
            {
                return;
            }
            if (Values == null)
            {
                Values = new List<decimal>();
            }

            if (TypeCalculationAverage == MovingAverageTypeCalculation.Simple)
            {
                Values.Add(GetValueSimple(candles, candles.Count - 1));
            }
            if (TypeCalculationAverage == MovingAverageTypeCalculation.Exponential)
            {
                Values.Add(GetValueExponential(candles, candles.Count - 1));
            }
            if (TypeCalculationAverage == MovingAverageTypeCalculation.Weighted)
            {
                Values.Add(GetValueWeighted(candles, candles.Count - 1));
            }
            if (TypeCalculationAverage == MovingAverageTypeCalculation.Radchenko)
            {
                Values.Add(GetValueRadchenko(Values, candles, candles.Count - 1));
            }
            if (TypeCalculationAverage == MovingAverageTypeCalculation.VolumeWeighted)
            {
                Values.Add(GetValueVolumeWeighted(candles, candles.Count - 1));
            }
        }

        /// <summary>
        /// прогрузить с самого начала
        /// </summary>
        private void ProcessAll(List<Candle> candles)
        {
            if (candles == null)
            {
                return;
            }
            Values = new List<decimal>();

            for (int i = 0; i < candles.Count; i++)
            {
                if (TypeCalculationAverage == MovingAverageTypeCalculation.Simple)
                {
                    Values.Add(GetValueSimple(candles, i));
                }
                if (TypeCalculationAverage == MovingAverageTypeCalculation.Exponential)
                {
                    Values.Add(GetValueExponential(candles, i));
                }
                if (TypeCalculationAverage == MovingAverageTypeCalculation.Weighted)
                {
                    Values.Add(GetValueWeighted(candles, i));
                }
                if (TypeCalculationAverage == MovingAverageTypeCalculation.Radchenko)
                {
                    Values.Add(GetValueRadchenko(Values, candles, i));
                }
                if (TypeCalculationAverage == MovingAverageTypeCalculation.VolumeWeighted)
                {
                    Values.Add(GetValueVolumeWeighted(candles, i));
                }
            }
        }

        /// <summary>
        /// перегрузить последнее значение
        /// </summary>
        private void ProcessLast(List<Candle> candles)
        {
            if (candles == null)
            {
                return;
            }
            if (TypeCalculationAverage == MovingAverageTypeCalculation.Simple)
            {
                Values[Values.Count - 1] = GetValueSimple(candles, candles.Count - 1);
            }
            if (TypeCalculationAverage == MovingAverageTypeCalculation.Exponential)
            {
                Values[Values.Count - 1] = GetValueExponential(candles, candles.Count - 1);
            }
            if (TypeCalculationAverage == MovingAverageTypeCalculation.Weighted)
            {
                Values[Values.Count - 1] = GetValueWeighted(candles, candles.Count - 1);
            }
            if (TypeCalculationAverage == MovingAverageTypeCalculation.Radchenko)
            {
                Values[Values.Count - 1] = GetValueRadchenko(Values, candles, candles.Count - 2);
            }
            if (TypeCalculationAverage == MovingAverageTypeCalculation.VolumeWeighted)
            {
                Values[Values.Count - 1] = GetValueVolumeWeighted(candles, candles.Count - 1);
            }
        }

        /// <summary>
        /// прогрузить новыми свечками
        /// </summary>
        public void Process(List<decimal> values)
        {
            if (values == null)
            {
                return;
            }
            _myValues = values;
            if (Values != null &&
                Values.Count + 1 == values.Count)
            {
                ProcessOne(values);
            }
            else if (Values != null &&
                     Values.Count == values.Count)
            {
                ProcessLast(values);
            }
            else
            {
                ProcessAll(values);
            }
        }

        /// <summary>
        /// прогрузить только последнюю свечку
        /// </summary>
        private void ProcessOne(List<decimal> values)
        {
            if (values == null)
            {
                return;
            }
            if (Values == null)
            {
                Values = new List<decimal>();
            }

            if (TypeCalculationAverage == MovingAverageTypeCalculation.VolumeWeighted)
            { // по объёму взвесить массив данных не выйдет. Ставим другой признак
                TypeCalculationAverage = MovingAverageTypeCalculation.Simple;
            }

            if (TypeCalculationAverage == MovingAverageTypeCalculation.Simple)
            {
                Values.Add(GetValueSimple(values, values.Count - 1));
            }
            if (TypeCalculationAverage == MovingAverageTypeCalculation.Exponential)
            {
                Values.Add(GetValueExponential(values, values.Count - 1));
            }
            if (TypeCalculationAverage == MovingAverageTypeCalculation.Weighted)
            {
                Values.Add(GetValueWeighted(values, values.Count - 1));
            }
            if (TypeCalculationAverage == MovingAverageTypeCalculation.Radchenko)
            {
                Values.Add(GetValueRadchenko(Values, values, values.Count - 1));
            }
        }

        /// <summary>
        /// прогрузить с самого начала
        /// </summary>
        private void ProcessAll(List<decimal> values)
        {
            if (values == null)
            {
                return;
            }
            Values = new List<decimal>();

            for (int i = 0; i < values.Count; i++)
            {
                if (TypeCalculationAverage == MovingAverageTypeCalculation.Simple)
                {
                    Values.Add(GetValueSimple(values, i));
                }
                if (TypeCalculationAverage == MovingAverageTypeCalculation.Exponential)
                {
                    Values.Add(GetValueExponential(values, i));
                }
                if (TypeCalculationAverage == MovingAverageTypeCalculation.Weighted ||
                    TypeCalculationAverage == MovingAverageTypeCalculation.VolumeWeighted)
                {
                    Values.Add(GetValueWeighted(values, i));
                }
                if (TypeCalculationAverage == MovingAverageTypeCalculation.Radchenko)
                {
                    Values.Add(GetValueRadchenko(Values, values, i));
                }
            }
        }

        /// <summary>
        /// перегрузить последнее значение
        /// </summary>
        private void ProcessLast(List<decimal> values)
        {
            if (values == null)
            {
                return;
            }
            if (TypeCalculationAverage == MovingAverageTypeCalculation.Simple)
            {
                Values[Values.Count - 1] = GetValueSimple(values, values.Count - 1);
            }
            if (TypeCalculationAverage == MovingAverageTypeCalculation.Exponential)
            {
                Values[Values.Count - 1] = GetValueExponential(values, values.Count - 1);
            }
            if (TypeCalculationAverage == MovingAverageTypeCalculation.Weighted)
            {
                Values[Values.Count - 1] = GetValueWeighted(values, values.Count - 1);
            }
            if (TypeCalculationAverage == MovingAverageTypeCalculation.Radchenko)
            {
                Values[Values.Count - 1] = GetValueRadchenko(Values, values, values.Count - 2);
            }
        }

        /// <summary>
        /// взять значение индикаторм по индексу
        /// </summary>
        private decimal GetValueSimple(List<decimal> values, int index)
        {
            if (index - Lenght <= 0)
            {
                return 0;
            }

            decimal average = 0;

            for (int i = index; i > index - Lenght; i--)
            {
                average += values[i];
            }

            average = average / Lenght;

            return Math.Round(average, 6);
        }

        /// <summary>
        /// экспонента
        /// </summary>
        private decimal GetValueExponential(List<decimal> values, int index)
        {
            decimal result = 0;

            if (index == Lenght)
            { // это первое значение. Рассчитываем как простую машку
                decimal lastMoving = 0;

                for (int i = index - Lenght + 1; i < index + 1; i++)
                {
                    lastMoving += values[i];
                }

                lastMoving = lastMoving / Lenght;

                result = lastMoving;
            }
            else if (index > Lenght)
            {
                // decimal a = 2.0m / (length * 2 - 0.15m);

                decimal a = Math.Round(2.0m / (Lenght + 1), 6);

                decimal emaLast = Values[index - 1];

                decimal p = values[index];
                //ЕМА(i) = ЕМА(i - 1) + ( К • [ Close(i) - ЕМА (i -1) ] ), 
                result = emaLast + (a * (p - emaLast));
                //result = a*p + (1 - a)*emaLast;
            }

            return Math.Round(result, 8);
        }

        /// <summary>
        /// взвешенная
        /// </summary>
        private decimal GetValueWeighted(List<decimal> values, int index)
        {
            if (index - Lenght <= 0)
            {
                return 0;
            }
            decimal average = 0;

            int weights = 0;

            for (int i = index, weight = Lenght; i > index - Lenght; i--, weight--)
            {
                average += values[i] * weight;
                weights += weight;
            }
            if (weights == 0)
            {
                return 0;
            }
            average = average / weights;

            return Math.Round(average, 8);

        }

        /// <summary>
        /// радченко
        /// </summary>
        private decimal GetValueRadchenko(List<decimal> lastValues, List<decimal> values, int index)
        {
            if (index - Lenght <= 0)
            {
                return 0;
            }

            decimal average = 0;

            for (int i = index; i > index - Lenght; i--)
            {
                average += values[i];
            }

            average = average / Lenght;

            int radchenkoFaze = 0; // 0 - ничего -1 - только вниз +1 только вверх

            if (values[index] > average)
            {
                radchenkoFaze = 1;
            }
            else if (values[index] < average)
            {
                radchenkoFaze = -1;
            }

            if (radchenkoFaze == 0)
            {
                return average;
            }

            if (values.Count == 0 || values[values.Count - 1] == 0)
            {
                return average;
            }

            if (radchenkoFaze == -1)
            {
                decimal lastPoint = lastValues[index - 1];

                if (average < lastPoint)
                {
                    return average;
                }
                else
                {
                    return lastPoint;
                }
            }

            if (radchenkoFaze == 1)
            {
                decimal lastPoint = lastValues[index - 1];

                if (average > lastPoint)
                {
                    return average;
                }
                else
                {
                    return lastPoint;
                }
            }

            return Math.Round(average, 8);
        }

        private decimal GetValueSimple(List<Candle> candles, int index)
        {
            if (index - Lenght <= 0)
            {
                return 0;
            }

            decimal average = 0;

            for (int i = index; i > index - Lenght; i--)
            {
                average += GetPoint(candles, i);
            }

            average = average / Lenght;

            return Math.Round(average, 6);
        }

        /// <summary>
        /// взять значения точки для рассчёта данных
        /// </summary>
        /// <param name="candles">свечи</param>
        /// <param name="index">индекс</param>
        /// <returns>значение точки по индексу</returns>
        private decimal GetPoint(List<Candle> candles, int index)
        {
            if (TypePointsToSearch == PriceTypePoints.Close)
            {
                return candles[index].Close;
            }
            else if (TypePointsToSearch == PriceTypePoints.High)
            {
                return candles[index].High;
            }
            else if (TypePointsToSearch == PriceTypePoints.Low)
            {
                return candles[index].Low;
            }
            else if (TypePointsToSearch == PriceTypePoints.Open)
            {
                return candles[index].Open;
            }
            else if (TypePointsToSearch == PriceTypePoints.Median)
            {
                return (candles[index].High + candles[index].Low) / 2;
            }
            else if (TypePointsToSearch == PriceTypePoints.Typical)
            {
                return (candles[index].High + candles[index].Low + candles[index].Close) / 3;
            }
            return 0;
        }

        /// <summary>
        /// экспонента
        /// </summary>
        private decimal GetValueExponential(List<Candle> candles, int index)
        {
            decimal result = 0;

            if (index == Lenght)
            { // это первое значение. Рассчитываем как простую машку
                decimal lastMoving = 0;

                for (int i = index - Lenght + 1; i < index + 1; i++)
                {
                    lastMoving += GetPoint(candles, i);
                }

                lastMoving = lastMoving / Lenght;

                result = lastMoving;
            }
            else if (index > Lenght)
            {
                // decimal a = 2.0m / (length * 2 - 0.15m);

                decimal a = Math.Round(2.0m / (Lenght + 1), 6);

                decimal emaLast = Values[index - 1];

                decimal p = GetPoint(candles, index);
                //ЕМА(i) = ЕМА(i - 1) + ( К • [ Close(i) - ЕМА (i -1) ] ), 
                result = emaLast + (a * (p - emaLast));
                //result = a*p + (1 - a)*emaLast;
            }

            return Math.Round(result, 8);
        }

        /// <summary>
        /// взвешенная
        /// </summary>
        private decimal GetValueWeighted(List<Candle> candles, int index)
        {
            if (index - Lenght <= 0)
            {
                return 0;
            }
            decimal average = 0;

            int weights = 0;

            for (int i = index, weight = Lenght; i > index - Lenght; i--, weight--)
            {
                average += GetPoint(candles, i) * weight;
                weights += weight;
            }

            average = average / weights;

            return Math.Round(average, 8);

        }

        /// <summary>
        /// радченко
        /// </summary>
        private decimal GetValueRadchenko(List<decimal> lastValues, List<Candle> candles, int index)
        {
            if (index - Lenght <= 0)
            {
                return 0;
            }

            decimal average = 0;

            for (int i = index; i > index - Lenght; i--)
            {
                average += GetPoint(candles, i);
            }

            average = average / Lenght;

            int radchenkoFaze = 0; // 0 - ничего -1 - только вниз +1 только вверх

            if (candles[index].Close > average)
            {
                radchenkoFaze = 1;
            }
            else if (candles[index].Close < average)
            {
                radchenkoFaze = -1;
            }

            if (radchenkoFaze == 0)
            {
                return average;
            }

            if (lastValues == null || lastValues.Count == 0 || lastValues[lastValues.Count - 1] == 0)
            {
                return average;
            }

            if (radchenkoFaze == -1)
            {
                decimal lastPoint = lastValues[lastValues.Count - 1];

                if (average < lastPoint)
                {
                    return average;
                }
                else
                {
                    return lastPoint;
                }
            }

            if (radchenkoFaze == 1)
            {
                decimal lastPoint = lastValues[lastValues.Count - 1];

                if (average > lastPoint)
                {
                    return average;
                }
                else
                {
                    return lastPoint;
                }
            }

            return Math.Round(average, 8);
        }

        /// <summary>
        /// взвешенная по объёму
        /// </summary>
        private decimal GetValueVolumeWeighted(List<Candle> candles, int index)
        {
            if (index - Lenght <= 0)
            {
                return 0;
            }
            decimal average = 0;

            decimal weights = 0;

            for (int i = index; i > index - Lenght; i--)
            {
                average += GetPoint(candles, i) * candles[i].Volume;
                weights += candles[i].Volume;
            }

            average = average / weights;

            return Math.Round(average, 8);

        }
    }
}
