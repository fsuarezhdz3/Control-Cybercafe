using PuntoVenta2.Data;
using MySql.Data.MySqlClient;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Data;
using System.Windows.Media;

namespace PuntoVenta2.Views
{
    public partial class ResumenTiemposView : UserControl
    {
        // Modelo para los datos del resumen
        public class ResumenTiempo
        {
            public string Consola { get; set; }
            public TimeSpan TiempoTotal { get; set; }
            public string TiempoTotalStr => TiempoTotal.ToString(@"hh\:mm\:ss");
            public int Dinero { get; set; }
            public DateTime? Fecha { get; set; }
        }

        public ObservableCollection<ResumenTiempo> Resumenes { get; set; }
        private DateTime _fechaFiltro;

        public ResumenTiemposView()
        {
            InitializeComponent();
            Resumenes = new ObservableCollection<ResumenTiempo>();
            GridResumenTiempos.ItemsSource = Resumenes;

            ConfigurarPermisos();
            CargarResumenHoy();
            ActualizarBotonActivo(BtnHoy);
        }

        private void ConfigurarPermisos()
        {
            // Ocultar filtros si no es admin (rol = 0)
            bool esAdmin = Sesion.Rol == 0;
            PanelFiltrosAdmin.Visibility = esAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        // ========== MÉTODOS DE CARGA DE DATOS ==========

        private void CargarResumenHoy()
        {
            _fechaFiltro = DateTime.Today;
            CargarResumenPorFecha(_fechaFiltro);
            TxtPeriodo.Text = "Hoy";
            ColFecha.Visibility = Visibility.Collapsed;
        }

        private void CargarResumenAyer()
        {
            _fechaFiltro = DateTime.Today.AddDays(-1);
            CargarResumenPorFecha(_fechaFiltro);
            TxtPeriodo.Text = "Ayer";
            ColFecha.Visibility = Visibility.Collapsed;
        }

        private void CargarResumenSemana()
        {
            DateTime inicioSemana = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
            if (inicioSemana > DateTime.Today) inicioSemana = inicioSemana.AddDays(-7);

            CargarResumenPorRango(inicioSemana, DateTime.Today);
            TxtPeriodo.Text = "Esta semana";
            ColFecha.Visibility = Visibility.Visible;
        }

        private void CargarResumenMes()
        {
            DateTime inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            CargarResumenPorRango(inicioMes, DateTime.Today);
            TxtPeriodo.Text = "Este mes";
            ColFecha.Visibility = Visibility.Visible;
        }

        private void CargarResumenAnio()
        {
            DateTime inicioAnio = new DateTime(DateTime.Today.Year, 1, 1);
            CargarResumenPorRango(inicioAnio, DateTime.Today);
            TxtPeriodo.Text = "Este año";
            ColFecha.Visibility = Visibility.Visible;
        }

        // ========== CONSULTAS A LA BD ==========

        private void CargarResumenPorFecha(DateTime fecha)
        {
            try
            {
                Resumenes.Clear();

                using (MySqlConnection con = ConexionBD.ObtenerConexion())
                {
                    con.Open();

                    string sql = @"
                        SELECT 
                            CONSOLA,
                            SUM(COSTO) as DINERO_TOTAL,
                            DATE(H_INICIO) as FECHA,
                            SUM(
                                CASE 
                                    WHEN T_TIEMPO IS NOT NULL THEN TIME_TO_SEC(T_TIEMPO)
                                    ELSE TIME_TO_SEC(TIMEDIFF(H_FINAL, H_INICIO))
                                END
                            ) as SEGUNDOS_TOTALES
                        FROM TIEMPOS 
                        WHERE H_FINAL IS NOT NULL 
                          AND DATE(H_INICIO) = @fecha
                          AND COSTO > 0
                        GROUP BY CONSOLA, DATE(H_INICIO)
                        ORDER BY DINERO_TOTAL DESC";

                    MySqlCommand cmd = new MySqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@fecha", fecha.Date);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            TimeSpan tiempoTotal = TimeSpan.Zero;

                            if (!reader.IsDBNull(reader.GetOrdinal("SEGUNDOS_TOTALES")))
                            {
                                try
                                {
                                    long segundosTotales = reader.GetInt64("SEGUNDOS_TOTALES");
                                    tiempoTotal = TimeSpan.FromSeconds(segundosTotales);
                                }
                                catch (InvalidCastException)
                                {
                                    int segundos = reader.GetInt32("SEGUNDOS_TOTALES");
                                    tiempoTotal = TimeSpan.FromSeconds(segundos);
                                }
                            }

                            var resumen = new ResumenTiempo
                            {
                                Consola = reader.GetString("CONSOLA"),
                                TiempoTotal = tiempoTotal,
                                Dinero = reader.IsDBNull(reader.GetOrdinal("DINERO_TOTAL"))
                                    ? 0
                                    : reader.GetInt32("DINERO_TOTAL"),
                                Fecha = reader.GetDateTime("FECHA")
                            };
                            Resumenes.Add(resumen);
                        }
                    }
                }

                CalcularTotales();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar resumen de tiempos: {ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void CargarResumenPorRango(DateTime fechaInicio, DateTime fechaFin)
        {
            try
            {
                Resumenes.Clear();

                using (MySqlConnection con = ConexionBD.ObtenerConexion())
                {
                    con.Open();

                    string sql = @"
                        SELECT 
                            CONSOLA,
                            SUM(COSTO) as DINERO_TOTAL,
                            DATE(H_INICIO) as FECHA,
                            SUM(
                                CASE 
                                    WHEN T_TIEMPO IS NOT NULL THEN TIME_TO_SEC(T_TIEMPO)
                                    ELSE TIME_TO_SEC(TIMEDIFF(H_FINAL, H_INICIO))
                                END
                            ) as SEGUNDOS_TOTALES
                        FROM TIEMPOS 
                        WHERE H_FINAL IS NOT NULL 
                          AND DATE(H_INICIO) BETWEEN @inicio AND @fin
                          AND COSTO > 0
                        GROUP BY CONSOLA, DATE(H_INICIO)
                        ORDER BY FECHA DESC, DINERO_TOTAL DESC";

                    MySqlCommand cmd = new MySqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@inicio", fechaInicio.Date);
                    cmd.Parameters.AddWithValue("@fin", fechaFin.Date);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            TimeSpan tiempoTotal = TimeSpan.Zero;

                            if (!reader.IsDBNull(reader.GetOrdinal("SEGUNDOS_TOTALES")))
                            {
                                try
                                {
                                    long segundosTotales = reader.GetInt64("SEGUNDOS_TOTALES");
                                    tiempoTotal = TimeSpan.FromSeconds(segundosTotales);
                                }
                                catch (InvalidCastException)
                                {
                                    int segundos = reader.GetInt32("SEGUNDOS_TOTALES");
                                    tiempoTotal = TimeSpan.FromSeconds(segundos);
                                }
                            }

                            var resumen = new ResumenTiempo
                            {
                                Consola = reader.GetString("CONSOLA"),
                                TiempoTotal = tiempoTotal,
                                Dinero = reader.IsDBNull(reader.GetOrdinal("DINERO_TOTAL"))
                                    ? 0
                                    : reader.GetInt32("DINERO_TOTAL"),
                                Fecha = reader.GetDateTime("FECHA")
                            };
                            Resumenes.Add(resumen);
                        }
                    }
                }

                CalcularTotales();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar resumen de tiempos: {ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void CalcularTotales()
        {
            // Calcular total de tiempo
            TimeSpan tiempoTotal = TimeSpan.Zero;
            foreach (var item in Resumenes)
            {
                tiempoTotal = tiempoTotal.Add(item.TiempoTotal);
            }
            TxtTiempoTotal.Text = tiempoTotal.ToString(@"hh\:mm\:ss");

            // Calcular total de dinero
            int dineroTotal = Resumenes.Sum(item => item.Dinero);
            TxtDineroTotal.Text = $"${dineroTotal}";
        }

        // ========== EVENTOS DE FILTROS ==========

        private void BtnFiltrarHoy_Click(object sender, RoutedEventArgs e)
        {
            CargarResumenHoy();
            ActualizarBotonActivo(sender as Button);
        }

        private void BtnFiltrarAyer_Click(object sender, RoutedEventArgs e)
        {
            CargarResumenAyer();
            ActualizarBotonActivo(sender as Button);
        }

        private void BtnFiltrarSemana_Click(object sender, RoutedEventArgs e)
        {
            CargarResumenSemana();
            ActualizarBotonActivo(sender as Button);
        }

        private void BtnFiltrarMes_Click(object sender, RoutedEventArgs e)
        {
            CargarResumenMes();
            ActualizarBotonActivo(sender as Button);
        }

        private void BtnFiltrarAnio_Click(object sender, RoutedEventArgs e)
        {
            CargarResumenAnio();
            ActualizarBotonActivo(sender as Button);
        }

        private void ActualizarBotonActivo(Button botonActivo)
        {
            var botones = new[] { BtnHoy, BtnAyer, BtnSemana, BtnMes, BtnAnio };
            foreach (var btn in botones)
            {
                if (btn != null)
                {
                    btn.Background = new SolidColorBrush(Color.FromRgb(44, 44, 44));
                    btn.Foreground = Brushes.White;
                    btn.BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85));
                }
            }

            if (botonActivo != null)
            {
                botonActivo.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                botonActivo.Foreground = Brushes.White;
                botonActivo.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
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