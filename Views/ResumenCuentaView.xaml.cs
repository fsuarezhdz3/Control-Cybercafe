using PuntoVenta2.Data;
using MySql.Data.MySqlClient;
using System.Windows;
using System.Windows.Controls;

namespace PuntoVenta2.Views
{
    public partial class ResumenCuentaView : UserControl
    {
        public ResumenCuentaView()
        {
            InitializeComponent();
            CargarInformacionUsuario();
        }

        private void CargarInformacionUsuario()
        {
            try
            {
                using (MySqlConnection con = ConexionBD.ObtenerConexion())
                {
                    con.Open();

                    string sql = @"
                        SELECT USUARIO, NOMBRE, APELLIDO, ROL
                        FROM CUENTAS 
                        WHERE USUARIO = @usuario";

                    MySqlCommand cmd = new MySqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@usuario", Sesion.Usuario);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string usuario = reader.GetString("USUARIO");
                            string nombre = reader.GetString("NOMBRE");
                            string apellido = reader.GetString("APELLIDO");
                            int rol = reader.GetInt32("ROL");

                            // Mostrar nombre completo
                            TxtNombreUsuario.Text = $"{nombre} {apellido}";

                            // Información adicional
                            string rolTexto = rol == 0 ? "Administrador" : "Usuario";
                            TxtInfoAdicional.Text = $"Usuario: {usuario}\nRol: {rolTexto}";
                        }
                    }
                }
            }
            catch
            {
                TxtNombreUsuario.Text = Sesion.Usuario;
                TxtInfoAdicional.Text = "Usuario activo";
            }
        }

        private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "¿Estás seguro de que deseas cerrar sesión?",
                "Confirmar cierre de sesión",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Limpiar sesión
                Sesion.Usuario = null;
                Sesion.Rol = 0;

                // Navegar al login
                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    mainWindow.NavegarALogin();
                }
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
            // Ya estamos en la vista de cuenta
        }
    }
}