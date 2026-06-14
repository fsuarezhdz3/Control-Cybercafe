using PuntoVenta2.Data;
using MySql.Data.MySqlClient;
using System;
using System.Windows;
using System.Windows.Controls;

namespace PuntoVenta2.Views
{
    public partial class InicioCorteView : UserControl
    {
        public InicioCorteView()
        {
            InitializeComponent();
        }

        private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            // 1️⃣ Validar monto
            if (!int.TryParse(txtInicioCorte.Text, out int inicioCorte) || inicioCorte < 0)
            {
                MessageBox.Show("Ingresa un monto válido", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2️⃣ Obtener fecha y hora LOCAL del equipo
            DateTime fechaHoraLocal = DateTime.Now;
            DateTime soloFecha = fechaHoraLocal.Date;

            // 3️⃣ Insertar corte
            using (MySqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();

                string sql = @"INSERT INTO CORTES
                               (FECHA, H_INICIO, INICIO_CORTE, USUARIO)
                               VALUES
                               (@fecha, @hora, @inicio, @usuario)";

                MySqlCommand cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@fecha", soloFecha);
                cmd.Parameters.AddWithValue("@hora", fechaHoraLocal);
                cmd.Parameters.AddWithValue("@inicio", inicioCorte);
                cmd.Parameters.AddWithValue("@usuario", Sesion.Usuario);

                cmd.ExecuteNonQuery();
            }

            // 4️⃣ Navegar al menú principal
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.NavegarAMenuPrincipal();
            }
        }
    }
}