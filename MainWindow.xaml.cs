using PuntoVenta2.Views;
using System.Windows;

namespace PuntoVenta2
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Content = new LoginView();
        }

        // Métodos de navegación públicos
        public void NavegarALogin()
        {
            Content = new LoginView();
        }

        public void NavegarAInicioCorte()
        {
            Content = new InicioCorteView();
        }

        public void NavegarAMenuPrincipal()
        {
            Content = new MenuPrincipalView();
        }

        public void NavegarAOrdenes()
        {
            Content = new OrdenesView();
        }

        public void NavegarATiempos()
        {
            Content = new TiemposView();
        }

        public void NavegarAResumen()
        {
            Content = new ResumenView(); // Vista principal de resúmenes
        }

        // NUEVO: Método para navegar a Finanzas
        public void NavegarAResumenFinanzas()
        {
            Content = new ResumenFinanzasView();
        }

        // Opcional: Si tienes otros métodos de navegación, mantenlos
        public void NavegarAResumenTiempos()
        {
            Content = new ResumenTiemposView();
        }

        public void NavegarAResumenProductos()
        {
            Content = new ResumenProductosView();
        }

        public void NavegarAResumenCaja()
        {
            Content = new ResumenCajaView();
        }

        public void NavegarACuenta()
        {
            Content = new ResumenCuentaView();
        }

    }

}