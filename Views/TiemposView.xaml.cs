using PuntoVenta2.Data;
using PuntoVenta2.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace PuntoVenta2.Views
{
    public partial class TiemposView : UserControl
    {
        public ObservableCollection<TiempoConsola> Consolas { get; set; }
        private DispatcherTimer _timer;
        private TiempoConsola _consolaSeleccionada;
        private Dictionary<string, bool> _tiemposProcesando = new Dictionary<string, bool>();

        public TiemposView()
        {
            InitializeComponent();
            Consolas = new ObservableCollection<TiempoConsola>();
            DataContext = this;
            InicializarConsolas();
            CargarTiemposActivos();
            InicializarTimer();
        }

        private void InicializarConsolas()
        {
            // Consolas principales ($20 por hora)
            string[] consolasPrincipales = { "Xbox", "PS4", "Switch", "PC", "Oculus" };

            foreach (var nombre in consolasPrincipales)
            {
                var consola = new TiempoConsola
                {
                    Consola = nombre,
                    TarifaPorHora = 20  // $20 por hora
                };
                Consolas.Add(consola);
                _tiemposProcesando[nombre] = false;
            }

            // Controles extra ($10 por hora - mitad de precio)
            string[] controlesExtra =
            {
                "Control extra de Xbox",
                "Control extra de Play 4",
                "Control extra de Switch 1",
                "Control extra de Switch 2",
                "Control extra de Switch 3"
            };

            foreach (var nombre in controlesExtra)
            {
                var control = new TiempoConsola
                {
                    Consola = nombre,
                    TarifaPorHora = 10  // $10 por hora (mitad de precio)
                };
                Consolas.Add(control);
                _tiemposProcesando[nombre] = false;
            }
        }

        private void InicializarTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void CargarTiemposActivos()
        {
            try
            {
                foreach (var consola in Consolas)
                {
                    consola.Resetear();
                }

                using (MySqlConnection con = ConexionBD.ObtenerConexion())
                {
                    con.Open();

                    string sql = @"
                        SELECT CONSOLA, H_INICIO, 
                               COALESCE(PAGO_INICIAL, 0) AS PAGO_INICIAL,
                               ID_CORTE
                        FROM TIEMPOS 
                        WHERE H_FINAL IS NULL 
                        ORDER BY H_INICIO DESC";

                    MySqlCommand cmd = new MySqlCommand(sql, con);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string consolaNombre = reader.GetString("CONSOLA");
                            DateTime horaInicio = reader.GetDateTime("H_INICIO");
                            int pagoInicial = reader.GetInt32("PAGO_INICIAL");

                            var consola = Consolas.FirstOrDefault(c =>
                                string.Equals(c.Consola, consolaNombre, StringComparison.OrdinalIgnoreCase));

                            if (consola != null)
                            {
                                consola.HoraInicio = horaInicio;
                                consola.DineroPagado = pagoInicial;
                                consola.Activo = true;
                                consola.TiempoTranscurrido = DateTime.Now - horaInicio;
                                consola.TerminoAutomaticamente = false;
                            }
                        }
                    }
                }

                GridConsolas.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar tiempos activos:\n{ex.Message}",
                               "Error de Base de Datos",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        // NUEVO MÉTODO: Obtener ID del corte activo
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
                    else
                    {
                        MessageBox.Show("No hay un corte activo. Debes iniciar un corte primero.",
                                        "Corte no encontrado",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Warning);
                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener ID de corte: {ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return 0;
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            DateTime ahora = DateTime.Now;

            foreach (var consola in Consolas)
            {
                if (consola.Activo && consola.HoraInicio.HasValue)
                {
                    var nuevoTiempo = ahora - consola.HoraInicio.Value;
                    consola.TiempoTranscurrido = nuevoTiempo;

                    if (nuevoTiempo.TotalSeconds >= consola.TiempoPagado.TotalSeconds - 1 && consola.Activo)
                    {
                        if (_tiemposProcesando.ContainsKey(consola.Consola) && _tiemposProcesando[consola.Consola])
                            continue;

                        MostrarAlertaTiempoTerminado(consola);
                    }
                }
            }
        }

        private void MostrarAlertaTiempoTerminado(TiempoConsola consola)
        {
            if (_tiemposProcesando[consola.Consola])
                return;

            _tiemposProcesando[consola.Consola] = true;

            try
            {
                consola.TerminoAutomaticamente = true;
                int costoFinal = consola.DineroPagado;

                Dispatcher.Invoke(() =>
                {
                    MessageBoxResult result = MessageBox.Show(
                        $"¡Tiempo terminado!\n\n" +
                        $"Consola: {consola.Consola}\n" +
                        $"Se cobrará: ${costoFinal}\n\n" +
                        $"Presiona OK para registrar.",
                        "Alarma",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.OK)
                    {
                        bool actualizado = ActualizarYLimpiarConsola(consola, costoFinal);

                        if (actualizado)
                        {
                            MessageBox.Show(
                                $"Se cobró: ${costoFinal}",
                                "Listo",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                    }
                });
            }
            finally
            {
                _tiemposProcesando[consola.Consola] = false;
            }
        }

        private bool ActualizarYLimpiarConsola(TiempoConsola consola, int costoFinal)
        {
            try
            {
                bool bdActualizada = ActualizarTiempoEnBD(consola, costoFinal);

                if (!bdActualizada)
                    return false;

                consola.Resetear();

                Dispatcher.Invoke(() =>
                {
                    GridConsolas.Items.Refresh();
                });

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool ActualizarTiempoEnBD(TiempoConsola consola, int costoFinal)
        {
            try
            {
                using (MySqlConnection con = ConexionBD.ObtenerConexion())
                {
                    con.Open();

                    // Obtener ID del corte activo
                    int idCorte = ObtenerIdCorteActivo();
                    if (idCorte == 0) return false;

                    DateTime horaFin = DateTime.Now;
                    string tiempoStr = consola.TiempoTranscurrido.ToString(@"hh\:mm\:ss");

                    string sql = @"
                        UPDATE TIEMPOS 
                        SET H_FINAL = @horaFinal, 
                            T_TIEMPO = @tiempoTranscurrido,
                            COSTO = @costoFinal,
                            ID_CORTE = @idCorte
                        WHERE CONSOLA = @consola 
                          AND H_FINAL IS NULL
                        ORDER BY H_INICIO DESC 
                        LIMIT 1";

                    MySqlCommand cmd = new MySqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@horaFinal", horaFin);
                    cmd.Parameters.AddWithValue("@tiempoTranscurrido", tiempoStr);
                    cmd.Parameters.AddWithValue("@costoFinal", costoFinal);
                    cmd.Parameters.AddWithValue("@idCorte", idCorte);  // NUEVO
                    cmd.Parameters.AddWithValue("@consola", consola.Consola);

                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar tiempo: {ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return false;
            }
        }

        private void GridConsolas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridConsolas.SelectedItem is TiempoConsola consola)
            {
                _consolaSeleccionada = consola;
                MostrarModalConsola(consola);
            }
        }

        private void MostrarModalConsola(TiempoConsola consola)
        {
            ModalTitulo.Text = consola.Consola;

            if (consola.Activo)
            {
                PanelInactivo.Visibility = Visibility.Collapsed;
                PanelActivo.Visibility = Visibility.Visible;
                btnAccionPrincipal.Content = "Terminar Tiempo";

                if (consola.HoraInicio.HasValue)
                {
                    consola.TiempoTranscurrido = DateTime.Now - consola.HoraInicio.Value;
                }

                txtTiempoTranscurrido.Text = consola.TiempoTranscurridoStr;

                var tiempoRestante = consola.TiempoPagado - consola.TiempoTranscurrido;
                if (tiempoRestante.TotalSeconds < 0) tiempoRestante = TimeSpan.Zero;
                txtTiempoRestante.Text = tiempoRestante.ToString(@"hh\:mm\:ss");

                txtCostoActual.Text = $"${consola.CostoEstimado}";

                // Mostrar tarifa específica
                string tarifaInfo = consola.TarifaPorHora == 10 ? " ($10/hora - Control Extra)" : " ($20/hora - Consola)";
                txtEstado.Text = $"Pagó: ${consola.DineroPagado}{tarifaInfo}";
            }
            else
            {
                PanelInactivo.Visibility = Visibility.Visible;
                PanelActivo.Visibility = Visibility.Collapsed;
                btnAccionPrincipal.Content = "Comenzar Tiempo";

                // Establecer monto inicial basado en la tarifa
                if (consola.TarifaPorHora == 10)
                {
                    txtDinero.Text = "10";  // $10 para controles extra
                    txtEstado.Text = "Disponible ($10 por hora - Control Extra)";
                }
                else
                {
                    txtDinero.Text = "20";  // $20 para consolas principales
                    txtEstado.Text = "Disponible ($20 por hora - Consola)";
                }
            }

            ModalOverlay.Visibility = Visibility.Visible;
        }

        private void BtnAccionPrincipal_Click(object sender, RoutedEventArgs e)
        {
            if (_consolaSeleccionada == null) return;

            if (_consolaSeleccionada.Activo)
            {
                if (_consolaSeleccionada.HoraInicio.HasValue)
                {
                    _consolaSeleccionada.TiempoTranscurrido = DateTime.Now - _consolaSeleccionada.HoraInicio.Value;
                }

                _consolaSeleccionada.TerminoAutomaticamente = false;
                int costoFinal = _consolaSeleccionada.CostoEstimado;

                bool jugoMenos = _consolaSeleccionada.TiempoTranscurrido < _consolaSeleccionada.TiempoPagado;

                string mensaje = $"¿Terminar tiempo de {_consolaSeleccionada.Consola}?\n\n" +
                               $"Tiempo: {_consolaSeleccionada.TiempoTranscurrido:hh\\:mm\\:ss}\n" +
                               $"Se cobrará: ${costoFinal}";

                if (jugoMenos)
                {
                    mensaje += $"\n(Jugó menos del tiempo pagado)";
                }

                // Mostrar tarifa en el mensaje
                string tarifaInfo = _consolaSeleccionada.TarifaPorHora == 10 ? " (Control Extra - $10/hora)" : " (Consola - $20/hora)";
                mensaje += $"\n{tarifaInfo}";

                MessageBoxResult result = MessageBox.Show(
                    mensaje,
                    "Confirmar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    bool actualizado = ActualizarYLimpiarConsola(_consolaSeleccionada, costoFinal);

                    if (actualizado)
                    {
                        MessageBox.Show(
                            $"Se cobró: ${costoFinal}",
                            "Listo",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Error al actualizar", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                if (int.TryParse(txtDinero.Text, out int dinero) && dinero > 0)
                {
                    IniciarTiempo(_consolaSeleccionada, dinero);
                }
                else
                {
                    MessageBox.Show("Ingresa una cantidad válida", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            CerrarModal();
        }

        private void IniciarTiempo(TiempoConsola consola, int dinero)
        {
            try
            {
                // Obtener ID del corte activo
                int idCorte = ObtenerIdCorteActivo();
                if (idCorte == 0) return;

                using (MySqlConnection con = ConexionBD.ObtenerConexion())
                {
                    con.Open();

                    string sql = @"
                        INSERT INTO TIEMPOS 
                        (H_INICIO, PAGO_INICIAL, COSTO, CONSOLA, ID_CORTE) 
                        VALUES 
                        (@horaInicio, @pagoInicial, 0, @consola, @idCorte)";

                    MySqlCommand cmd = new MySqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@horaInicio", DateTime.Now);
                    cmd.Parameters.AddWithValue("@pagoInicial", dinero);
                    cmd.Parameters.AddWithValue("@consola", consola.Consola);
                    cmd.Parameters.AddWithValue("@idCorte", idCorte);  // NUEVO

                    cmd.ExecuteNonQuery();

                    consola.HoraInicio = DateTime.Now;
                    consola.DineroPagado = dinero;
                    consola.Activo = true;
                    consola.TiempoTranscurrido = TimeSpan.Zero;
                    consola.TerminoAutomaticamente = false;

                    GridConsolas.Items.Refresh();

                    // Mostrar mensaje con tarifa específica
                    string tarifaInfo = consola.TarifaPorHora == 10 ? " (Control Extra - $10/hora)" : " (Consola - $20/hora)";
                    MessageBox.Show(
                        $"Tiempo iniciado{tarifaInfo}\n" +
                        $"Consola: {consola.Consola}\n" +
                        $"Pago: ${dinero}\n" +
                        $"Corte: #{idCorte}",
                        "Listo",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    // Intentar versión alternativa sin ID_CORTE (para compatibilidad)
                    using (MySqlConnection con = ConexionBD.ObtenerConexion())
                    {
                        con.Open();

                        string sqlAlt = "INSERT INTO TIEMPOS (H_INICIO, PAGO_INICIAL, COSTO, CONSOLA) VALUES (@horaInicio, @pagoInicial, 0, @consola)";
                        MySqlCommand cmdAlt = new MySqlCommand(sqlAlt, con);
                        cmdAlt.Parameters.AddWithValue("@horaInicio", DateTime.Now);
                        cmdAlt.Parameters.AddWithValue("@pagoInicial", dinero);
                        cmdAlt.Parameters.AddWithValue("@consola", consola.Consola);

                        cmdAlt.ExecuteNonQuery();

                        consola.HoraInicio = DateTime.Now;
                        consola.DineroPagado = dinero;
                        consola.Activo = true;
                        consola.TiempoTranscurrido = TimeSpan.Zero;
                        consola.TerminoAutomaticamente = false;

                        GridConsolas.Items.Refresh();

                        string tarifaInfo = consola.TarifaPorHora == 10 ? " (Control Extra)" : "";
                        MessageBox.Show($"Tiempo iniciado{tarifaInfo}", "Listo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex2)
                {
                    MessageBox.Show($"Error: {ex2.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnCancelarModal_Click(object sender, RoutedEventArgs e)
        {
            CerrarModal();
        }

        private void CerrarModal()
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
            _consolaSeleccionada = null;
            GridConsolas.SelectedItem = null;
        }

        private void TxtDinero_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void BtnOrdenes_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
                mainWindow.NavegarAOrdenes();
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

        public void DetenerTimer()
        {
            _timer?.Stop();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            DetenerTimer();
        }
    }
}