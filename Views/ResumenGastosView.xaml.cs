using PuntoVenta2.Data;
using MySql.Data.MySqlClient;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace PuntoVenta2.Views
{
    public partial class ResumenGastosView : UserControl
    {
        public class Gasto
        {
            public int Id { get; set; }
            public string Concepto { get; set; }
            public int Cantidad { get; set; }          // De columna MONTO en BD
            public int CostoUnitario { get; set; }     // De columna CANTIDAD en BD
            public int Total { get; set; }             // De columna TOTAL en BD
            public DateTime Fecha { get; set; }
        }

        public ObservableCollection<Gasto> Gastos { get; set; }
        private DateTime _fechaFiltro;

        public ResumenGastosView()
        {
            InitializeComponent();
            Gastos = new ObservableCollection<Gasto>();
            GridGastos.ItemsSource = Gastos;

            CargarGastosTodo();
            ActualizarBotonActivo(BtnTodo);
        }

        // ========== MÉTODOS DE CARGA CON FILTROS ==========

        private void CargarGastosTodo()
        {
            _fechaFiltro = DateTime.MinValue;
            CargarGastosPorFecha(null, null);
            TxtPeriodo.Text = "Todo (histórico)";
        }

        private void CargarGastosHoy()
        {
            _fechaFiltro = DateTime.Today;
            CargarGastosPorFecha(_fechaFiltro, _fechaFiltro);
            TxtPeriodo.Text = "Hoy";
        }

        private void CargarGastosAyer()
        {
            _fechaFiltro = DateTime.Today.AddDays(-1);
            CargarGastosPorFecha(_fechaFiltro, _fechaFiltro);
            TxtPeriodo.Text = "Ayer";
        }

        private void CargarGastosSemana()
        {
            DateTime inicioSemana = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
            if (inicioSemana > DateTime.Today) inicioSemana = inicioSemana.AddDays(-7);

            CargarGastosPorFecha(inicioSemana, DateTime.Today);
            TxtPeriodo.Text = "Esta semana";
        }

        private void CargarGastosMes()
        {
            DateTime inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            CargarGastosPorFecha(inicioMes, DateTime.Today);
            TxtPeriodo.Text = "Este mes";
        }

        private void CargarGastosAnio()
        {
            DateTime inicioAnio = new DateTime(DateTime.Today.Year, 1, 1);
            CargarGastosPorFecha(inicioAnio, DateTime.Today);
            TxtPeriodo.Text = "Este año";
        }

        // ========== CONSULTA A LA BD ==========

        private void CargarGastosPorFecha(DateTime? fechaInicio, DateTime? fechaFin)
        {
            try
            {
                Gastos.Clear();

                using (MySqlConnection con = ConexionBD.ObtenerConexion())
                {
                    con.Open();

                    string sql;
                    if (fechaInicio.HasValue && fechaFin.HasValue)
                    {
                        if (fechaInicio == fechaFin)
                        {
                            sql = @"
                                SELECT ID, CONCEPTO, CANTIDAD, MONTO, TOTAL, FECHA
                                FROM GASTOS 
                                WHERE DATE(FECHA) = @fecha
                                ORDER BY FECHA DESC, ID DESC";
                        }
                        else
                        {
                            sql = @"
                                SELECT ID, CONCEPTO, CANTIDAD, MONTO, TOTAL, FECHA
                                FROM GASTOS 
                                WHERE DATE(FECHA) BETWEEN @inicio AND @fin
                                ORDER BY FECHA DESC, ID DESC";
                        }
                    }
                    else
                    {
                        sql = @"
                            SELECT ID, CONCEPTO, CANTIDAD, MONTO, TOTAL, FECHA
                            FROM GASTOS 
                            ORDER BY FECHA DESC, ID DESC";
                    }

                    MySqlCommand cmd = new MySqlCommand(sql, con);

                    if (fechaInicio.HasValue && fechaFin.HasValue)
                    {
                        if (fechaInicio == fechaFin)
                        {
                            cmd.Parameters.AddWithValue("@fecha", fechaInicio.Value.Date);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@inicio", fechaInicio.Value.Date);
                            cmd.Parameters.AddWithValue("@fin", fechaFin.Value.Date);
                        }
                    }

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var gasto = new Gasto
                            {
                                Id = reader.GetInt32("ID"),
                                Concepto = reader.GetString("CONCEPTO"),
                                Cantidad = reader.GetInt32("MONTO"),          // MONTO = Cantidad
                                CostoUnitario = reader.GetInt32("CANTIDAD"),  // CANTIDAD = Costo unitario
                                Total = reader.GetInt32("TOTAL"),
                                Fecha = reader.GetDateTime("FECHA")
                            };

                            Gastos.Add(gasto);
                        }
                    }
                }

                CalcularTotalGastos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar gastos: {ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void CalcularTotalGastos()
        {
            int totalGastos = Gastos.Sum(g => g.Total);
            TxtTotalGastos.Text = $"${totalGastos:N0}";
        }

        // ========== EVENTOS DE FILTROS ==========

        private void BtnFiltrarHoy_Click(object sender, RoutedEventArgs e)
        {
            CargarGastosHoy();
            ActualizarBotonActivo(sender as Button);
        }

        private void BtnFiltrarAyer_Click(object sender, RoutedEventArgs e)
        {
            CargarGastosAyer();
            ActualizarBotonActivo(sender as Button);
        }

        private void BtnFiltrarSemana_Click(object sender, RoutedEventArgs e)
        {
            CargarGastosSemana();
            ActualizarBotonActivo(sender as Button);
        }

        private void BtnFiltrarMes_Click(object sender, RoutedEventArgs e)
        {
            CargarGastosMes();
            ActualizarBotonActivo(sender as Button);
        }

        private void BtnFiltrarAnio_Click(object sender, RoutedEventArgs e)
        {
            CargarGastosAnio();
            ActualizarBotonActivo(sender as Button);
        }

        private void BtnFiltrarTodo_Click(object sender, RoutedEventArgs e)
        {
            CargarGastosTodo();
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

        // ========== MODAL AGREGAR GASTO ==========

        private void BtnAgregarGasto_Click(object sender, RoutedEventArgs e)
        {
            // Limpiar campos
            TxtConceptoGasto.Text = "";
            TxtCantidadGasto.Text = "";
            TxtCostoUnitario.Text = "";
            TxtTotalCalculado.Text = "$0";
            TxtMensajeError.Visibility = Visibility.Collapsed;

            // Mostrar modal
            ModalGasto.Visibility = Visibility.Visible;
        }

        private void BtnConfirmarGasto_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtConceptoGasto.Text))
            {
                MostrarError("Ingresa un concepto para el gasto.");
                return;
            }

            if (!int.TryParse(TxtCantidadGasto.Text, out int cantidad) || cantidad <= 0)
            {
                MostrarError("Ingresa una cantidad válida mayor a 0.");
                return;
            }

            if (!int.TryParse(TxtCostoUnitario.Text, out int costoUnitario) || costoUnitario <= 0)
            {
                MostrarError("Ingresa un costo unitario válido mayor a 0.");
                return;
            }

            // Calcular total
            int total = cantidad * costoUnitario;

            try
            {
                using (MySqlConnection con = ConexionBD.ObtenerConexion())
                {
                    con.Open();

                    string sql = @"
                        INSERT INTO GASTOS 
                        (CONCEPTO, CANTIDAD, MONTO, TOTAL, FECHA) 
                        VALUES 
                        (@concepto, @cantidad, @monto, @total, @fecha)";

                    MySqlCommand cmd = new MySqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@concepto", TxtConceptoGasto.Text);
                    cmd.Parameters.AddWithValue("@cantidad", costoUnitario);    // CANTIDAD en BD = Costo unitario
                    cmd.Parameters.AddWithValue("@monto", cantidad);           // MONTO en BD = Cantidad
                    cmd.Parameters.AddWithValue("@total", total);
                    cmd.Parameters.AddWithValue("@fecha", DateTime.Today);

                    cmd.ExecuteNonQuery();

                    MessageBox.Show($"Gasto registrado exitosamente\n" +
                                   $"Concepto: {TxtConceptoGasto.Text}\n" +
                                   $"Cantidad: {cantidad}\n" +
                                   $"Costo unitario: ${costoUnitario}\n" +
                                   $"Total: ${total}",
                                   "Gasto registrado",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Information);

                    ModalGasto.Visibility = Visibility.Collapsed;

                    // Recargar gastos según el filtro actual
                    if (_fechaFiltro == DateTime.MinValue)
                        CargarGastosTodo();
                    else if (_fechaFiltro == DateTime.Today)
                        CargarGastosHoy();
                    else if (_fechaFiltro == DateTime.Today.AddDays(-1))
                        CargarGastosAyer();
                    else
                        CargarGastosTodo(); // Por defecto
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al registrar gasto: {ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void BtnCancelarModal_Click(object sender, RoutedEventArgs e)
        {
            ModalGasto.Visibility = Visibility.Collapsed;
        }

        // ========== CÁLCULO EN TIEMPO REAL ==========

        private void TxtCantidad_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
            if (!e.Handled)
            {
                Dispatcher.BeginInvoke(new Action(CalcularTotalEnTiempoReal));
            }
        }

        private void TxtCosto_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
            if (!e.Handled)
            {
                Dispatcher.BeginInvoke(new Action(CalcularTotalEnTiempoReal));
            }
        }

        private void CalcularTotalEnTiempoReal()
        {
            if (int.TryParse(TxtCantidadGasto.Text, out int cantidad) &&
                int.TryParse(TxtCostoUnitario.Text, out int costoUnitario))
            {
                int total = cantidad * costoUnitario;
                TxtTotalCalculado.Text = $"${total}";
            }
            else
            {
                TxtTotalCalculado.Text = "$0";
            }
        }

        private void MostrarError(string mensaje)
        {
            TxtMensajeError.Text = mensaje;
            TxtMensajeError.Visibility = Visibility.Visible;
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