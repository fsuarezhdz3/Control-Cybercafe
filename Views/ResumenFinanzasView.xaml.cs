using PuntoVenta2.Data;
using MySql.Data.MySqlClient;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PuntoVenta2.Views
{
    public partial class ResumenFinanzasView : UserControl
    {
        private enum PeriodoFiltro { Todo, Hoy, Ayer, Semana, Mes, Anio }
        private PeriodoFiltro _filtroActual;

        public ResumenFinanzasView()
        {
            InitializeComponent();
            ConfigurarPermisos();
            CargarDatosTodo();
            ActualizarBotonActivo(BtnTodo);
        }

        private void ConfigurarPermisos()
        {
            bool esAdmin = Sesion.Rol == 0;
            PanelFiltrosAdmin.Visibility = esAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        // ========== MÉTODOS DE CARGA CON FILTROS ==========

        private void CargarDatosTodo()
        {
            _filtroActual = PeriodoFiltro.Todo;
            CargarDatosFinancieros(null, null);
            TxtPeriodo.Text = "Todo (histórico)";
        }

        private void CargarDatosHoy()
        {
            _filtroActual = PeriodoFiltro.Hoy;
            DateTime hoy = DateTime.Today;
            CargarDatosFinancieros(hoy, hoy);
            TxtPeriodo.Text = "Hoy";
        }

        private void CargarDatosAyer()
        {
            _filtroActual = PeriodoFiltro.Ayer;
            DateTime ayer = DateTime.Today.AddDays(-1);
            CargarDatosFinancieros(ayer, ayer);
            TxtPeriodo.Text = "Ayer";
        }

        private void CargarDatosSemana()
        {
            _filtroActual = PeriodoFiltro.Semana;
            DateTime inicioSemana = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
            if (inicioSemana > DateTime.Today) inicioSemana = inicioSemana.AddDays(-7);

            CargarDatosFinancieros(inicioSemana, DateTime.Today);
            TxtPeriodo.Text = "Esta semana";
        }

        private void CargarDatosMes()
        {
            _filtroActual = PeriodoFiltro.Mes;
            DateTime inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            CargarDatosFinancieros(inicioMes, DateTime.Today);
            TxtPeriodo.Text = "Este mes";
        }

        private void CargarDatosAnio()
        {
            _filtroActual = PeriodoFiltro.Anio;
            DateTime inicioAnio = new DateTime(DateTime.Today.Year, 1, 1);
            CargarDatosFinancieros(inicioAnio, DateTime.Today);
            TxtPeriodo.Text = "Este año";
        }

        // ========== MÉTODO PRINCIPAL CON FILTROS (CORREGIDO) ==========

        private void CargarDatosFinancieros(DateTime? fechaInicio, DateTime? fechaFin)
        {
            try
            {
                using (MySqlConnection con = ConexionBD.ObtenerConexion())
                {
                    con.Open();

                    string filtroVentas = "";
                    string filtroTiempos = "";
                    string filtroGastos = "";
                    string filtroCortes = "";

                    // Construir filtros según las fechas
                    if (fechaInicio.HasValue && fechaFin.HasValue)
                    {
                        filtroVentas = "WHERE DATE(V.FECHA) BETWEEN @inicio AND @fin";
                        filtroTiempos = "WHERE DATE(T.H_INICIO) BETWEEN @inicio AND @fin";
                        filtroGastos = "WHERE DATE(G.FECHA) BETWEEN @inicio AND @fin";
                        filtroCortes = "WHERE DATE(C.FECHA) BETWEEN @inicio AND @fin AND C.H_FINAL IS NOT NULL";
                    }
                    else if (fechaInicio.HasValue)
                    {
                        filtroVentas = "WHERE DATE(V.FECHA) = @inicio";
                        filtroTiempos = "WHERE DATE(T.H_INICIO) = @inicio";
                        filtroGastos = "WHERE DATE(G.FECHA) = @inicio";
                        filtroCortes = "WHERE DATE(C.FECHA) = @inicio AND C.H_FINAL IS NOT NULL";
                    }
                    else
                    {
                        // Sin filtro - todo histórico
                        filtroVentas = "";
                        filtroTiempos = "";
                        filtroGastos = "";
                        filtroCortes = "WHERE C.H_FINAL IS NOT NULL";
                    }

                    // 1. Calcular ventas de productos (tabla VENTAS)
                    string sqlVentas;
                    if (string.IsNullOrEmpty(filtroVentas))
                    {
                        sqlVentas = @"
                            SELECT COALESCE(SUM(V.MONTO), 0) as TOTAL_VENTAS
                            FROM VENTAS V";
                    }
                    else
                    {
                        sqlVentas = $@"
                            SELECT COALESCE(SUM(V.MONTO), 0) as TOTAL_VENTAS
                            FROM VENTAS V
                            {filtroVentas}";
                    }

                    MySqlCommand cmdVentas = new MySqlCommand(sqlVentas, con);
                    if (fechaInicio.HasValue) cmdVentas.Parameters.AddWithValue("@inicio", fechaInicio.Value.Date);
                    if (fechaFin.HasValue) cmdVentas.Parameters.AddWithValue("@fin", fechaFin.Value.Date);

                    int ventasProductos = Convert.ToInt32(cmdVentas.ExecuteScalar());

                    // 2. Calcular dinero de tiempos (tabla TIEMPOS, columna COSTO)
                    string sqlTiempos;
                    if (string.IsNullOrEmpty(filtroTiempos))
                    {
                        sqlTiempos = @"
                            SELECT COALESCE(SUM(T.COSTO), 0) as TOTAL_TIEMPOS
                            FROM TIEMPOS T
                            WHERE T.COSTO > 0";
                    }
                    else
                    {
                        sqlTiempos = $@"
                            SELECT COALESCE(SUM(T.COSTO), 0) as TOTAL_TIEMPOS
                            FROM TIEMPOS T
                            {filtroTiempos}
                            AND T.COSTO > 0";
                    }

                    MySqlCommand cmdTiempos = new MySqlCommand(sqlTiempos, con);
                    if (fechaInicio.HasValue) cmdTiempos.Parameters.AddWithValue("@inicio", fechaInicio.Value.Date);
                    if (fechaFin.HasValue) cmdTiempos.Parameters.AddWithValue("@fin", fechaFin.Value.Date);

                    int ventasTiempos = Convert.ToInt32(cmdTiempos.ExecuteScalar());

                    // 3. Calcular TOTAL VENTAS (productos + tiempos)
                    int totalVentas = ventasProductos + ventasTiempos;
                    TxtVentasTotales.Text = $"${totalVentas:N0}";

                    // 4. Calcular gastos totales
                    string sqlGastos;
                    if (string.IsNullOrEmpty(filtroGastos))
                    {
                        sqlGastos = @"
                            SELECT COALESCE(SUM(G.TOTAL), 0) as TOTAL_GASTOS
                            FROM GASTOS G";
                    }
                    else
                    {
                        sqlGastos = $@"
                            SELECT COALESCE(SUM(G.TOTAL), 0) as TOTAL_GASTOS
                            FROM GASTOS G
                            {filtroGastos}";
                    }

                    MySqlCommand cmdGastos = new MySqlCommand(sqlGastos, con);
                    if (fechaInicio.HasValue) cmdGastos.Parameters.AddWithValue("@inicio", fechaInicio.Value.Date);
                    if (fechaFin.HasValue) cmdGastos.Parameters.AddWithValue("@fin", fechaFin.Value.Date);

                    int totalGastos = Convert.ToInt32(cmdGastos.ExecuteScalar());
                    TxtGastosTotales.Text = $"${totalGastos:N0}";

                    // 5. Calcular ganancias netas
                    int gananciasNetas = totalVentas - totalGastos;
                    TxtGananciasNetas.Text = $"${gananciasNetas:N0}";

                    // 6. Calcular número de cortes en el período
                    string sqlCortes;
                    if (string.IsNullOrEmpty(filtroCortes))
                    {
                        sqlCortes = @"
                            SELECT COUNT(*) as TOTAL_CORTES
                            FROM CORTES C
                            WHERE C.H_FINAL IS NOT NULL";
                    }
                    else
                    {
                        sqlCortes = $@"
                            SELECT COUNT(*) as TOTAL_CORTES
                            FROM CORTES C
                            {filtroCortes}";
                    }

                    MySqlCommand cmdCortes = new MySqlCommand(sqlCortes, con);
                    if (fechaInicio.HasValue) cmdCortes.Parameters.AddWithValue("@inicio", fechaInicio.Value.Date);
                    if (fechaFin.HasValue) cmdCortes.Parameters.AddWithValue("@fin", fechaFin.Value.Date);

                    int cortesCerrados = Convert.ToInt32(cmdCortes.ExecuteScalar());
                    TxtCortesCerrados.Text = cortesCerrados.ToString();

                    // 7. Calcular margen de ganancia
                    if (totalVentas > 0)
                    {
                        double margen = ((double)gananciasNetas / totalVentas) * 100;
                        TxtMargenGanancia.Text = $"{margen:F1}%";

                        // Colorear según el margen
                        if (margen >= 30)
                            TxtMargenGanancia.Foreground = Brushes.LimeGreen;
                        else if (margen >= 15)
                            TxtMargenGanancia.Foreground = Brushes.GreenYellow;
                        else if (margen >= 0)
                            TxtMargenGanancia.Foreground = Brushes.Yellow;
                        else
                            TxtMargenGanancia.Foreground = Brushes.Red;
                    }
                    else
                    {
                        TxtMargenGanancia.Text = "0%";
                        TxtMargenGanancia.Foreground = Brushes.White;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos financieros: {ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        // ========== EVENTOS DE FILTROS ==========

        private void BtnFiltrarHoy_Click(object sender, RoutedEventArgs e)
        {
            CargarDatosHoy();
            ActualizarBotonActivo(sender as Button);
        }

        private void BtnFiltrarAyer_Click(object sender, RoutedEventArgs e)
        {
            CargarDatosAyer();
            ActualizarBotonActivo(sender as Button);
        }

        private void BtnFiltrarSemana_Click(object sender, RoutedEventArgs e)
        {
            CargarDatosSemana();
            ActualizarBotonActivo(sender as Button);
        }

        private void BtnFiltrarMes_Click(object sender, RoutedEventArgs e)
        {
            CargarDatosMes();
            ActualizarBotonActivo(sender as Button);
        }

        private void BtnFiltrarAnio_Click(object sender, RoutedEventArgs e)
        {
            CargarDatosAnio();
            ActualizarBotonActivo(sender as Button);
        }

        private void BtnFiltrarTodo_Click(object sender, RoutedEventArgs e)
        {
            CargarDatosTodo();
            ActualizarBotonActivo(sender as Button);
        }

        private void ActualizarBotonActivo(Button botonActivo)
        {
            var botones = new[] { BtnTodo, BtnHoy, BtnAyer, BtnSemana, BtnMes, BtnAnio };
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

        // ========== BOTÓN REGRESAR ==========

        private void BtnRegresar_Click(object sender, RoutedEventArgs e)
        {
            // Regresar a la vista anterior (Resumen principal)
            if (Window.GetWindow(this) is MainWindow mainWindow)
                mainWindow.NavegarAResumen();
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