using System;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Forms.TextBox;

namespace OsEngine.Charts.CandleChart.Indicators
{
    /// <summary>
    /// Логика взаимодействия для MovingAverageUi.xaml
    /// </summary>
    public partial class TunnelUi 
    {
        /// <summary>
        /// индикатор который мы настраиваем
        /// </summary>
        private Tunnel tunnel;

        /// <summary>
        /// изменился ли индикатор
        /// </summary>
        public bool IsChange;

        /// <summary>
        /// конструктор
        /// </summary>
        /// <param name="mA">индикатор для настройки</param>
        public TunnelUi(Tunnel t)
        {
            InitializeComponent();
            tunnel = t;

            TextBoxLenght.Text = tunnel.Lenght.ToString();
            TextBoxWidth.Text = tunnel.Width.ToString();
            HostColor.Child = new TextBox();
            HostColor.Child.BackColor = tunnel.ColorBase;

            ComboBoxMovingType.Items.Add(MovingAverageTypeCalculation.Exponential);
            ComboBoxMovingType.Items.Add(MovingAverageTypeCalculation.Simple);
            ComboBoxMovingType.Items.Add(MovingAverageTypeCalculation.Weighted);
            ComboBoxMovingType.Items.Add(MovingAverageTypeCalculation.VolumeWeighted);
            ComboBoxMovingType.SelectionChanged += ComboBoxMovingType_SelectionChanged;

            ComboBoxMovingType.SelectedItem = tunnel.TypeCalculationAverage;
            

            CheckBoxPaintOnOff.IsChecked = tunnel.PaintOn;
            ComboBoxPriceField.Items.Add(PriceTypePoints.Open);
            ComboBoxPriceField.Items.Add(PriceTypePoints.High);
            ComboBoxPriceField.Items.Add(PriceTypePoints.Low);
            ComboBoxPriceField.Items.Add(PriceTypePoints.Close);
            ComboBoxPriceField.Items.Add(PriceTypePoints.Median);
            ComboBoxPriceField.Items.Add(PriceTypePoints.Typical);

            ComboBoxPriceField.SelectedItem = tunnel.TypePointsToSearch;

            Width = 460;
        }

        void ComboBoxMovingType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
        }

        /// <summary>
        /// кнопка принять
        /// </summary>
        private void ButtonAccept_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Convert.ToInt32(TextBoxLenght.Text) <= 0 ||
                    Convert.ToDecimal(TextBoxWidth.Text) <= 0 /*||
                    Convert.ToInt32(TextBoxProfit.Text) <= 0*/)
                {
                    throw new Exception("error");
                }
                Enum.TryParse(ComboBoxMovingType.SelectedItem.ToString(), true, out tunnel.TypeCalculationAverage);
            }
            catch (Exception)
            {
                MessageBox.Show("Процесс сохранения прерван. В одном из полей недопустимые значения");
                return;
            }

            tunnel.ColorBase = HostColor.Child.BackColor;
            tunnel.Lenght = Convert.ToInt32(TextBoxLenght.Text);
            tunnel.Width = Convert.ToDecimal(TextBoxWidth.Text);
            tunnel.PaintOn = CheckBoxPaintOnOff.IsChecked.Value;

            ComboBoxMovingType.Items.Add(MovingAverageTypeCalculation.Exponential);
            ComboBoxMovingType.Items.Add(MovingAverageTypeCalculation.Simple);
            Enum.TryParse(ComboBoxMovingType.SelectedItem.ToString(), true, out tunnel.TypeCalculationAverage);

            Enum.TryParse(ComboBoxPriceField.SelectedItem.ToString(), true, out tunnel.TypePointsToSearch);

            tunnel.Save();
            IsChange = true;
            Close();
        }

        /// <summary>
        /// кнопка изменить цвет
        /// </summary>
        private void ButtonColor_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog dialog = new ColorDialog();
            dialog.Color = HostColor.Child.BackColor;
            dialog.ShowDialog();

            HostColor.Child.BackColor = dialog.Color;
        }

    }
}
