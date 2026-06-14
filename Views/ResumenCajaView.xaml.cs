using PuntoVenta2.Data;
using MySql.Data.MySqlClient;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PuntoVenta2.Views
{
    public partial class ResumenCajaView : UserControl
    {
        private enum TipoMovimiento { Ingreso, Retiro }
        private TipoMovimiento _movimientoActual;
        private int _idCorteActual;
        private int _inicioCorte;

        public ResumenCajaView()
        {
            InitializeComponent();
            CargarDatosCaja();
        }

        private void CargarDatosCaja()
        {
            try
            {
                using (MySqlConnection con = ConexionBD.ObtenerConexion())
                {
                    con.Open();

                    // 1. Obtener el corte activo más reciente
                    string sqlCorte = @"
                        SELECT ID, INICIO_CORTE, H_INICIO 
                        FROM CORTES 
                        WHERE H_FINAL IS NULL 
                        ORDER BY ID DESC 
                        LIMIT 1";

                    MySqlCommand cmdCorte = new MySqlCommand(sqlCorte, con);
                    var readerCorte = cmdCorte.ExecuteReader();

                    if (readerCorte.Read())
                    {
                        _idCorteActual = readerCorte.GetInt32("ID");
                        _inicioCorte = readerCorte.GetInt32("INICIO_CORTE");
                        DateTime horaInicio = readerCorte.GetDateTime("H_INICIO");

                        TxtIdCorte.Text = _idCorteActual.ToString();
                        TxtHoraInicio.Text = horaInicio.ToString("HH:mm");
                        TxtInicioCorte.Text = $"${_inicioCorte}";

                        readerCorte.Close();

                        // 2. Calcular ventas de PRODUCTOS
                        string sqlVentas = @"
                            SELECT COALESCE(SUM(MONTO), 0) as TOTAL_VENTAS
                            FROM VENTAS 
                            WHERE ID_CORTE = @idCorte";

                        MySqlCommand cmdVentas = new MySqlCommand(sqlVentas, con);
                        cmdVentas.Parameters.AddWithValue("@idCorte", _idCorteActual);
                        int ventasProductos = Convert.ToInt32(cmdVentas.ExecuteScalar());

                        // 3. Calcular dinero de TIEMPOS
                        string sqlTiempos = @"
                            SELECT COALESCE(SUM(COSTO), 0) as TOTAL_TIEMPOS
                            FROM TIEMPOS 
                            WHERE ID_CORTE = @idCorte
                            AND COSTO > 0";

                        MySqlCommand cmdTiempos = new MySqlCommand(sqlTiempos, con);
                        cmdTiempos.Parameters.AddWithValue("@idCorte", _idCorteActual);
                        int ventasTiempos = Convert.ToInt32(cmdTiempos.ExecuteScalar());

                        // 4. Calcular VENTAS TOTALES (productos + tiempos)
                        int totalVentas = ventasProductos + ventasTiempos;
                        TxtVentasTotales.Text = $"${totalVentas}";

                        // 5. Calcular total retiros
                        string sqlRetiros = @"
                            SELECT COALESCE(SUM(TOTAL_RETIRO), 0) as TOTAL_RETIROS
                            FROM RETIROS 
                            WHERE ID_CORTE = @idCorte";

                        MySqlCommand cmdRetiros = new MySqlCommand(sqlRetiros, con);
                        cmdRetiros.Parameters.AddWithValue("@idCorte", _idCorteActual);
                        int totalRetiros = Convert.ToInt32(cmdRetiros.ExecuteScalar());
                        TxtRetiros.Text = $"${totalRetiros}";

                        // 6. Calcular total ingresos
                        string sqlIngresos = @"
                            SELECT COALESCE(SUM(TOTAL_INGRESO), 0) as TOTAL_INGRESOS
                            FROM INGRESOS 
                            WHERE ID_CORTE = @idCorte";

                        MySqlCommand cmdIngresos = new MySqlCommand(sqlIngresos, con);
                        cmdIngresos.Parameters.AddWithValue("@idCorte", _idCorteActual);
                        int totalIngresos = Convert.ToInt32(cmdIngresos.ExecuteScalar());
                        TxtIngresos.Text = $"${totalIngresos}";

                        // 7. Calcular total en caja
                        int totalCaja = _inicioCorte + totalVentas + totalIngresos - totalRetiros;
                        TxtTotalCaja.Text = $"${totalCaja}";
                    }
                    else
                    {
                        readerCorte.Close();
                        MessageBox.Show("No hay un corte activo. Debes iniciar un corte primero.",
                                        "Corte no encontrado",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Warning);

                        if (Window.GetWindow(this) is MainWindow mainWindow)
                            mainWindow.NavegarAMenuPrincipal();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos de caja: {ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        // ========== EVENTOS DE BOTONES DE ACCIÓN ==========

        private void BtnAgregarDinero_Click(object sender, RoutedEventArgs e)
        {
            _movimientoActual = TipoMovimiento.Ingreso;
            ModalTitulo.Text = "Agregar Dinero";
            TxtConcepto.Text = "";
            TxtCantidad.Text = "";
            TxtMensajeError.Visibility = Visibility.Collapsed;
            ModalDinero.Visibility = Visibility.Visible;
        }

        private void BtnSacarDinero_Click(object sender, RoutedEventArgs e)
        {
            _movimientoActual = TipoMovimiento.Retiro;
            ModalTitulo.Text = "Sacar Dinero";
            TxtConcepto.Text = "";
            TxtCantidad.Text = "";
            TxtMensajeError.Visibility = Visibility.Collapsed;
            ModalDinero.Visibility = Visibility.Visible;
        }

        private void BtnConfirmarDinero_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtConcepto.Text))
            {
                MostrarError("Ingresa un concepto para el movimiento.");
                return;
            }

            if (!int.TryParse(TxtCantidad.Text, out int cantidad) || cantidad <= 0)
            {
                MostrarError("Ingresa una cantidad válida mayor a 0.");
                return;
            }

            try
            {
                using (MySqlConnection con = ConexionBD.ObtenerConexion())
                {
                    con.Open();

                    if (_movimientoActual == TipoMovimiento.Ingreso)
                    {
                        string sql = @"
                            INSERT INTO INGRESOS 
                            (TOTAL_INGRESO, CONCEPTO, FECHA, ID_CORTE) 
                            VALUES 
                            (@total, @concepto, @fecha, @idCorte)";

                        MySqlCommand cmd = new MySqlCommand(sql, con);
                        cmd.Parameters.AddWithValue("@total", cantidad);
                        cmd.Parameters.AddWithValue("@concepto", TxtConcepto.Text);
                        cmd.Parameters.AddWithValue("@fecha", DateTime.Now);
                        cmd.Parameters.AddWithValue("@idCorte", _idCorteActual);

                        cmd.ExecuteNonQuery();

                        MessageBox.Show($"Se agregó ${cantidad} a la caja\nConcepto: {TxtConcepto.Text}",
                                        "Ingreso registrado",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Information);
                    }
                    else
                    {
                        string sql = @"
                            INSERT INTO RETIROS 
                            (TOTAL_RETIRO, CONCEPTO, FECHA, ID_CORTE) 
                            VALUES 
                            (@total, @concepto, @fecha, @idCorte)";

                        MySqlCommand cmd = new MySqlCommand(sql, con);
                        cmd.Parameters.AddWithValue("@total", cantidad);
                        cmd.Parameters.AddWithValue("@concepto", TxtConcepto.Text);
                        cmd.Parameters.AddWithValue("@fecha", DateTime.Now);
                        cmd.Parameters.AddWithValue("@idCorte", _idCorteActual);

                        cmd.ExecuteNonQuery();

                        MessageBox.Show($"Se retiró ${cantidad} de la caja\nConcepto: {TxtConcepto.Text}",
                                        "Retiro registrado",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Information);
                    }

                    ModalDinero.Visibility = Visibility.Collapsed;
                    CargarDatosCaja();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al registrar movimiento: {ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void BtnFinalizarCorte_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "¿Estás seguro de que deseas finalizar el corte?\n\n" +
                "Una vez finalizado se cerrará la sesión automáticamente.",
                "Confirmar finalización de corte",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                FinalizarCorte();
            }
        }

        private void FinalizarCorte()
        {
            try
            {
                using (MySqlConnection con = ConexionBD.ObtenerConexion())
                {
                    con.Open();

                    // 1. Calcular totales de ingresos, retiros, productos y tiempos
                    string sqlTotales = @"
                        SELECT 
                            (SELECT COALESCE(SUM(TOTAL_INGRESO), 0) FROM INGRESOS WHERE ID_CORTE = @idCorte) as TOTAL_INGRESOS,
                            (SELECT COALESCE(SUM(TOTAL_RETIRO), 0) FROM RETIROS WHERE ID_CORTE = @idCorte) as TOTAL_RETIROS,
                            (SELECT COALESCE(SUM(MONTO), 0) FROM VENTAS WHERE ID_CORTE = @idCorte) as TOTAL_VENTAS,
                            (SELECT COALESCE(SUM(COSTO), 0) FROM TIEMPOS WHERE ID_CORTE = @idCorte AND COSTO > 0) as TOTAL_TIEMPOS";

                    MySqlCommand cmdTotales = new MySqlCommand(sqlTotales, con);
                    cmdTotales.Parameters.AddWithValue("@idCorte", _idCorteActual);

                    var reader = cmdTotales.ExecuteReader();
                    reader.Read();

                    int totalIngresos = reader.GetInt32("TOTAL_INGRESOS");
                    int totalRetiros = reader.GetInt32("TOTAL_RETIROS");
                    int ventasProductos = reader.GetInt32("TOTAL_VENTAS");
                    int ventasTiempos = reader.GetInt32("TOTAL_TIEMPOS");

                    reader.Close();

                    // 2. Calcular total del corte (productos + tiempos)
                    int totalVentas = ventasProductos + ventasTiempos;
                    int totalCorte = _inicioCorte + totalVentas + totalIngresos - totalRetiros;

                    // 3. Actualizar corte con los totales
                    string sqlUpdate = @"
                        UPDATE CORTES 
                        SET H_FINAL = @horaFinal,
                            TOTAL_CORTE = @totalCorte,
                            INGRESOS = @ingresos,
                            RETIROS = @retiros
                        WHERE ID = @idCorte";

                    MySqlCommand cmdUpdate = new MySqlCommand(sqlUpdate, con);
                    cmdUpdate.Parameters.AddWithValue("@horaFinal", DateTime.Now);
                    cmdUpdate.Parameters.AddWithValue("@totalCorte", totalCorte);
                    cmdUpdate.Parameters.AddWithValue("@ingresos", totalIngresos);
                    cmdUpdate.Parameters.AddWithValue("@retiros", totalRetiros);
                    cmdUpdate.Parameters.AddWithValue("@idCorte", _idCorteActual);

                    int rows = cmdUpdate.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        MessageBox.Show($"Corte #{_idCorteActual} finalizado exitosamente\n" +
                                       $"Productos: ${ventasProductos}\n" +
                                       $"Tiempos: ${ventasTiempos}\n" +
                                       $"Ventas totales: ${totalVentas}\n" +
                                       $"Ingresos adicionales: ${totalIngresos}\n" +
                                       $"Retiros: ${totalRetiros}\n" +
                                       $"Total en caja: ${totalCorte}\n\n" +
                                       $"Se cerrará la sesión automáticamente.",
                                       "Corte finalizado",
                                       MessageBoxButton.OK,
                                       MessageBoxImage.Information);

                        if (Window.GetWindow(this) is MainWindow mainWindow)
                            mainWindow.NavegarALogin();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al finalizar corte: {ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        // ========== MÉTODOS AUXILIARES ==========

        private void MostrarError(string mensaje)
        {
            TxtMensajeError.Text = mensaje;
            TxtMensajeError.Visibility = Visibility.Visible;
        }

        private void BtnCancelarModal_Click(object sender, RoutedEventArgs e)
        {
            ModalDinero.Visibility = Visibility.Collapsed;
        }

        private void TxtCantidad_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
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