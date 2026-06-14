using PuntoVenta2.Data;
using MySql.Data.MySqlClient;
using System.Windows;
using System.Windows.Controls;

namespace PuntoVenta2.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string usuario = txtUsuario.Text;
            string password = txtPassword.Password;

            using (MySqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();

                string sql = "SELECT CONTRASENA, BLOQUEO, INTENTOS, ROL " +
                             "FROM CUENTAS WHERE USUARIO = @u";

                MySqlCommand cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@u", usuario);

                MySqlDataReader dr = cmd.ExecuteReader();

                if (!dr.Read())
                {
                    txtError.Text = "Usuario o contraseña incorrectos";
                    return;
                }

                string passBD = dr.GetString(0);
                bool bloqueado = dr.GetBoolean(1);
                int intentos = dr.GetInt32(2);
                int rol = dr.GetInt32(3);

                dr.Close();

                if (bloqueado)
                {
                    txtError.Text = "Cuenta bloqueada";
                    return;
                }

                if (password != passBD)
                {
                    string upd = "UPDATE CUENTAS SET INTENTOS = INTENTOS + 1 WHERE USUARIO = @u";
                    MySqlCommand up = new MySqlCommand(upd, con);
                    up.Parameters.AddWithValue("@u", usuario);
                    up.ExecuteNonQuery();

                    txtError.Text = "Usuario o contraseña incorrectos";
                    return;
                }

                // ✔ Login correcto
                string reset = "UPDATE CUENTAS SET INTENTOS = 0 WHERE USUARIO = @u";
                MySqlCommand r = new MySqlCommand(reset, con);
                r.Parameters.AddWithValue("@u", usuario);
                r.ExecuteNonQuery();

                Sesion.Usuario = usuario;
                Sesion.Rol = rol;

                // Navegar a InicioCorte
                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    mainWindow.NavegarAInicioCorte();
                }
            }
        }
    }
}