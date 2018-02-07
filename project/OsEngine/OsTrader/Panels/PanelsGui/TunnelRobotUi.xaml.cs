/*
 *Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Windows;
using static OsEngine.OsTrader.Panels.PanelCreator;

namespace OsEngine.OsTrader.Panels.PanelsGui
{
    /// <summary>
    /// Логика взаимодействия для RobotUi.xaml
    /// </summary>
    public partial class TunnelRobotUi
    {
        private TunnelRobot robot;
        public TunnelRobotUi(TunnelRobot robot)
        {
            InitializeComponent();
            this.robot = robot;

            //this.ProfitBox.Text = this.robot.Profit.ValueDecimal.ToString();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //this.robot.Profit.ValueDecimal = Convert.ToDecimal(this.ProfitBox.Text);
            Close();
        }
    }
}