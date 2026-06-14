using PuntoVenta2.Data;
using MySql.Data.MySqlClient;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Media;

namespace PuntoVenta2.Views
{
    public partial class ResumenProductosView : UserControl
    {
        public class ProductoResumen
        {
            public string Producto { get; set; }
            public int Cantidad { get; set; }
            public int CostoUnitario { get; set; }
            public int Total { get; set; }
            public DateTime Fecha { get; set; }
        }

        public class VentaDetalle
        {
            public string Concepto { get; set; }
            public int Monto { get; set; }
            public string Hora { get; set; }
            public DateTime Fecha { get; set; }
        }

        public ObservableCollection<ProductoResumen> ResumenProductos { get; set; }
        public ObservableCollection<VentaDetalle> DetalleVentas { get; set; }
        private DateTime _fechaFiltro;
        private int _idCorteActual;

        public ResumenProductosView()
        {
            InitializeComponent();
            ResumenProductos = new ObservableCollection<ProductoResumen>();
            DetalleVentas = new ObservableCollection<VentaDetalle>();

            GridResumenProductos.ItemsSource = ResumenProductos;
            GridDetalleVentas.ItemsSource = DetalleVentas;

            _idCorteActual = ObtenerIdCorteActivo();
            ConfigurarPermisos();
            CargarResumenHoy();
            ActualizarBotonActivo(BtnHoy);
        }

        private int ObtenerIdCorteActivo()
        {
            try
            {
                using (MySqlConnection con = ConexionBD.ObtenerConexion())
                {
                    con.Open();

                    string sql = @"
                        SELECT ID 
                        FROM CORTES 
                        WHERE H_FINAL IS NULL 
                        ORDER BY ID DESC 
                        LIMIT 1";

                    MySqlCommand cmd = new MySqlCommand(sql, con);
                    object result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception)
            {
                // Si hay error, usar valor por defecto
            }

            return 1;
        }

        private void ConfigurarPermisos()
        {
            bool esAdmin = Sesion.Rol == 0;
            PanelFiltrosAdmin.Visibility = esAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        // ========== MÉTODOS DE CARGA DE DATOS ==========

        private void CargarResumenHoy()
        {
            _fechaFiltro = DateTime.Today;
            CargarResumenPorFecha(_fechaFiltro);
            ColFechaResumen.Visibility = Visibility.Collapsed;
            ColFechaDetalle.Visibility = Visibility.Collapsed;
        }

        private void CargarResumenAyer()
        {
            _fechaFiltro = DateTime.Today.AddDays(-1);
            CargarResumenPorFecha(_fechaFiltro);
            ColFechaResumen.Visibility = Visibility.Collapsed;
            ColFechaDetalle.Visibility = Visibility.Collapsed;
        }

        private void CargarResumenSemana()
        {
            DateTime inicioSemana = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
            if (inicioSemana > DateTime.Today) inicioSemana = inicioSemana.AddDays(-7);

            CargarResumenPorRango(inicioSemana, DateTime.Today);
            ColFechaResumen.Visibility = Visibility.Visible;
            ColFechaDetalle.Visibility = Visibility.Visible;
        }

        private void CargarResumenMes()
        {
            DateTime inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            CargarResumenPorRango(inicioMes, DateTime.Today);
            ColFechaResumen.Visibility = Visibility.Visible;
            ColFechaDetalle.Visibility = Visibility.Visible;
        }

        private void CargarResumenAnio()
        {
            DateTime inicioAnio = new DateTime(DateTime.Today.Year, 1, 1);
            CargarResumenPorRango(inicioAnio, DateTime.Today);
            ColFechaResumen.Visibility = Visibility.Visible;
            ColFechaDetalle.Visibility = Visibility.Visible;
        }

        // ========== CONSULTAS A LA BD (ACTUALIZADAS) ==========

        private void CargarResumenPorFecha(DateTime fecha)
        {
            try
            {
                ResumenProductos.Clear();
                DetalleVentas.Clear();

                using (MySqlConnection con = ConexionBD.ObtenerConexion())
                {
                    con.Open();

                    // CONSULTA 1: Resumen por producto (agrupado) - ACTUALIZADA
                    string sqlResumen = @"
                        SELECT 
                            CONCEPTO as PRODUCTO,
                            COUNT(*) as CANTIDAD,
                            MIN(MONTO) as COSTO_UNITARIO,
                            SUM(MONTO) as TOTAL,
                            DATE(FECHA) as FECHA
                        FROM VENTAS 
                        WHERE DATE(FECHA) = @fecha
                          AND ID_CORTE = @idCorte  -- NUEVO FILTRO
                        GROUP BY CONCEPTO, DATE(FECHA)
                        ORDER BY CANTIDAD DESC, PRODUCTO ASC";

                    MySqlCommand cmdResumen = new MySqlCommand(sqlResumen, con);
                    cmdResumen.Parameters.AddWithValue("@fecha", fecha.Date);
                    cmdResumen.Parameters.AddWithValue("@idCorte", _idCorteActual);  // NUEVO

                    using (MySqlDataReader reader = cmdResumen.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var producto = new ProductoResumen
                            {
                                Producto = reader.GetString("PRODUCTO"),
                                Cantidad = reader.GetInt32("CANTIDAD"),
                                CostoUnitario = reader.GetInt32("COSTO_UNITARIO"),
                                Total = reader.GetInt32("TOTAL"),
                                Fecha = reader.GetDateTime("FECHA")
                            };
                            ResumenProductos.Add(producto);
                        }
                    }

                    // CONSULTA 2: Detalle individual de ventas - ACTUALIZADA
                    string sqlDetalle = @"
                        SELECT 
                            CONCEPTO,
                            MONTO,
                            TIME(FECHA) as HORA,
                            DATE(FECHA) as FECHA
                        FROM VENTAS 
                        WHERE DATE(FECHA) = @fecha
                          AND ID_CORTE = @idCorte  -- NUEVO FILTRO
                        ORDER BY FECHA DESC";

                    MySqlCommand cmdDetalle = new MySqlCommand(sqlDetalle, con);
                    cmdDetalle.Parameters.AddWithValue("@fecha", fecha.Date);
                    cmdDetalle.Parameters.AddWithValue("@idCorte", _idCorteActual);  // NUEVO

                    using (MySqlDataReader reader = cmdDetalle.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var venta = new VentaDetalle
                            {
                                Concepto = reader.GetString("CONCEPTO"),
                                Monto = reader.GetInt32("MONTO"),
                                Hora = reader.GetTimeSpan("HORA").ToString(@"hh\:mm"),
                                Fecha = reader.GetDateTime("FECHA")
                            };
                            DetalleVentas.Add(venta);
                        }
                    }
                }

                CalcularTotales();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar resumen de productos: {ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void CargarResumenPorRango(DateTime fechaInicio, DateTime fechaFin)
        {
            try
            {
                ResumenProductos.Clear();
                DetalleVentas.Clear();

                using (MySqlConnection con = ConexionBD.ObtenerConexion())
                {
                    con.Open();

                    // CONSULTA 1: Resumen por producto (agrupado por fecha) - ACTUALIZADA
                    string sqlResumen = @"
                        SELECT 
                            CONCEPTO as PRODUCTO,
                            COUNT(*) as CANTIDAD,
                            MIN(MONTO) as COSTO_UNITARIO,
                            SUM(MONTO) as TOTAL,
                            DATE(FECHA) as FECHA
                        FROM VENTAS 
                        WHERE DATE(FECHA) BETWEEN @inicio AND @fin
                          AND ID_CORTE = @idCorte  -- NUEVO FILTRO
                        GROUP BY CONCEPTO, DATE(FECHA)
                        ORDER BY FECHA DESC, CANTIDAD DESC";

                    MySqlCommand cmdResumen = new MySqlCommand(sqlResumen, con);
                    cmdResumen.Parameters.AddWithValue("@inicio", fechaInicio.Date);
                    cmdResumen.Parameters.AddWithValue("@fin", fechaFin.Date);
                    cmdResumen.Parameters.AddWithValue("@idCorte", _idCorteActual);  // NUEVO

                    using (MySqlDataReader reader = cmdResumen.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var producto = new ProductoResumen
                            {
                                Producto = reader.GetString("PRODUCTO"),
                                Cantidad = reader.GetInt32("CANTIDAD"),
                                CostoUnitario = reader.GetInt32("COSTO_UNITARIO"),
                                Total = reader.GetInt32("TOTAL"),
                                Fecha = reader.GetDateTime("FECHA")
                            };
                            ResumenProductos.Add(producto);
                        }
                    }

                    // CONSULTA 2: Detalle individual de ventas - ACTUALIZADA
                    string sqlDetalle = @"
                        SELECT 
                            CONCEPTO,
                            MONTO,
                            TIME(FECHA) as HORA,
                            DATE(FECHA) as FECHA
                        FROM VENTAS 
                        WHERE DATE(FECHA) BETWEEN @inicio AND @fin
                          AND ID_CORTE = @idCorte  -- NUEVO FILTRO
                        ORDER BY FECHA DESC";

                    MySqlCommand cmdDetalle = new MySqlCommand(sqlDetalle, con);
                    cmdDetalle.Parameters.AddWithValue("@inicio", fechaInicio.Date);
                    cmdDetalle.Parameters.AddWithValue("@fin", fechaFin.Date);
                    cmdDetalle.Parameters.AddWithValue("@idCorte", _idCorteActual);  // NUEVO

                    using (MySqlDataReader reader = cmdDetalle.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var venta = new VentaDetalle
                            {
                                Concepto = reader.GetString("CONCEPTO"),
                                Monto = reader.GetInt32("MONTO"),
                                Hora = reader.GetTimeSpan("HORA").ToString(@"hh\:mm"),
                                Fecha = reader.GetDateTime("FECHA")
                            };
                            DetalleVentas.Add(venta);
                        }
                    }
                }

                CalcularTotales();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar resumen de productos: {ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void CalcularTotales()
        {
            int totalProductos = ResumenProductos.Sum(p => p.Cantidad);
            TxtTotalProductos.Text = totalProductos.ToString();

            int totalRecaudado = ResumenProductos.Sum(p => p.Total);
            TxtTotalRecaudado.Text = $"${totalRecaudado}";
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