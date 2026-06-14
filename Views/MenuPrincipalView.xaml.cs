using System.Windows;
using System.Windows.Controls;

namespace PuntoVenta2.Views
{
    public partial class MenuPrincipalView : UserControl
    {
        public MenuPrincipalView()
        {
            InitializeComponent();
        }

        // 🟦 ÓRDENES
        private void BtnOrdenes_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.NavegarAOrdenes(); // ✅ Ahora funciona
            }
        }

        // 🟦 TIEMPOS
        private void BtnTiempos_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.NavegarATiempos(); // Temporal: va a MenuPrincipal
            }
        }

        // 🟦 RESUMEN
        private void BtnResumen_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.NavegarAResumen(); // Temporal: va a MenuPrincipal
            }
        }

        // 🟦 CUENTA
        private void BtnCuenta_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.NavegarACuenta(); // Temporal: va a MenuPrincipal
            }
        }
    }
}