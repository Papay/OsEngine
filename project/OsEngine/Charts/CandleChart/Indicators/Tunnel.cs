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
        /// конструктор с параметрами. Индикатор будет сохраняться
        /// </summary>
        /// <param name="unqueName">уникальное имя</param>
        /// <param name="canDelete">можно ли пользователю удалить индикатор с графика вручную</param>
        public Tunnel(bool canDelete, string unqueName)
        {
            CanDelete = canDelete;
            Name = unqueName;
            TypeIndicator = IndicatorOneCandleChartType.Line;
            TypePointsToSearch = PriceTypePoints.Close;
            ColorBase = Color.DeepSkyBlue;
            Lenght = 13;
            PaintOn = true;
            CanDelete = canDelete;

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
            Range = 21;
            PaintOn = true;
            CanDelete = canDelete;
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
        public int Range { get; set; }

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
                    writer.WriteLine(Range);
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
                    Range = Convert.ToInt32(reader.ReadLine());
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
        }

        public void ShowDialog()
        {
        }

        public event Action<IIndicatorCandle> NeadToReloadEvent;

        /// <summary>
        /// прогрузить новыми свечками
        /// </summary>
        public void Process(List<Candle> candles)
        {
            if (candles == null)
            {
                return;
            }
        }
    }
}
