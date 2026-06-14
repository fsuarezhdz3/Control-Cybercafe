using PuntoVenta2.Data;
using System.Windows;
using System.Windows.Controls;

namespace PuntoVenta2.Views
{
    public partial class ResumenView : UserControl
    {
        public ResumenView()
        {
            InitializeComponent();
            ConfigurarPermisos();
        }

        private void ConfigurarPermisos()
        {
            // Verificar si el usuario es admin (rol = 0)
            bool esAdmin = Sesion.Rol == 0;

            // Mostrar/ocultar botones según el rol
            PanelAdmin.Visibility = esAdmin ? Visibility.Visible : Visibility.Collapsed;

            if (!esAdmin)
            {
                TxtMensajeUsuario.Text = $"Bienvenido {Sesion.Usuario}\nSolo los administradores pueden ver Inventario, Finanzas y Gastos";
                TxtMensajeUsuario.Visibility = Visibility.Visible;
            }
        }

        // ========== EVENTOS DE LOS BOTONES DE RESUMEN ==========

        private void BtnResumenTiempo_Click(object sender, RoutedEventArgs e)
        {
            // Navegar a ResumenTiemposView
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.Content = new ResumenTiemposView();
            }
        }

        private void BtnResumenProductos_Click(object sender, RoutedEventArgs e)
        {
            // Navegar a ResumenProductosView
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.Content = new ResumenProductosView();
            }
        }

        private void BtnResumenCaja_Click(object sender, RoutedEventArgs e)
        {
            // Navegar a ResumenCajaView
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.Content = new ResumenCajaView();
            }
        }

        // NUEVO MÉTODO: Botón Finanzas
        private void BtnResumenFinanzas_Click(object sender, RoutedEventArgs e)
        {
            // Navegar a ResumenFinanzasView
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.Content = new ResumenFinanzasView();
            }
        }

        // NUEVO MÉTODO: Botón Gastos
        private void BtnResumenGastos_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.Content = new ResumenGastosView();
            }
        }

        private void BtnResumenInventario_Click(object sender, RoutedEventArgs e)
        {
            // Navegar a ResumenInventarioView
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.Content = new ResumenInventarioView();
            }
        }

        // ========== NAVEGACIÓN ==========

        private void BtnOrdenes_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
                mainWindow.NavegarAOrdenes();
        }

        private void BtnTiempos_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
                mainWindow.NavegarATiempos();
        }

        private void BtnResumen_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
                mainWindow.NavegarAResumen();
        }

        private void BtnCuenta_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
                mainWindow.NavegarACuenta();
        }
    }
}