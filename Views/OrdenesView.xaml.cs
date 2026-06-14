using PuntoVenta2.Data;
using PuntoVenta2.Models;
using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using System;
using System.Windows;
using System.Windows.Controls;

namespace PuntoVenta2.Views
{
    public partial class OrdenesView : UserControl
    {
        public ObservableCollection<Orden> Ordenes { get; set; }
        private AgregarOrdenesView _agregarOrdenesModal;
        private EditarOrdenView _editarOrdenModal;

        public OrdenesView()
        {
            InitializeComponent();
            Ordenes = new ObservableCollection<Orden>();
            DataContext = this;
            CargarOrdenes();
        }

        // =========================
        // CARGAR ÓRDENES
        // =========================
        private void CargarOrdenes()
        {
            Ordenes.Clear();

            using (MySqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                string sqlOrdenes = "SELECT * FROM PREORDEN WHERE ESTADO = 0 ORDER BY NUM_ORDEN";
                MySqlCommand cmd = new MySqlCommand(sqlOrdenes, con);
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Orden orden = new Orden
                    {
                        NumOrden = reader.GetInt32("NUM_ORDEN"),
                        Total = reader.GetInt32("TOTAL"),
                        Comentarios = reader["COMENTARIOS"]?.ToString(),
                        Productos = new System.Collections.Generic.List<ProductoOrden>()
                    };
                    Ordenes.Add(orden);
                }
                reader.Close();

                foreach (var orden in Ordenes)
                {
                    string sqlProductos = "SELECT ID, CONCEPTO, COSTO FROM PRODUCTOS_ORDEN WHERE NUM_ORDEN = @n ORDER BY ID";
                    MySqlCommand pCmd = new MySqlCommand(sqlProductos, con);
                    pCmd.Parameters.AddWithValue("@n", orden.NumOrden);

                    var r = pCmd.ExecuteReader();
                    while (r.Read())
                    {
                        orden.Productos.Add(new ProductoOrden
                        {
                            Id = r.GetInt32("ID"),
                            Concepto = r.GetString("CONCEPTO"),
                            Costo = r.GetInt32("COSTO")
                        });
                    }
                    r.Close();
                }
            }
        }

        // =========================
        // BOTÓN AGREGAR ORDEN (ABRIR MODAL)
        // =========================
        private void BtnAgregarOrden_Click(object sender, RoutedEventArgs e)
        {
            _agregarOrdenesModal = new AgregarOrdenesView();
            _agregarOrdenesModal.ModalCerrado += OnModalAgregarCerrado;
            ModalContentContainer.Child = _agregarOrdenesModal;
            ModalOverlay.Visibility = Visibility.Visible;
        }

        // =========================
        // BOTÓN CERRAR MODAL (X)
        // =========================
        private void BtnCerrarModal_Click(object sender, RoutedEventArgs e)
        {
            CerrarModal();
        }

        // =========================
        // MANEJADOR DE CIERRE DEL MODAL AGREGAR
        // =========================
        private void OnModalAgregarCerrado(bool ordenCreada)
        {
            CerrarModal();
            if (ordenCreada)
            {
                CargarOrdenes();
            }
        }

        // =========================
        // MODIFICAR ORDEN
        // =========================
        private void BtnEditarOrden_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                int numOrden = (int)btn.Tag;
                AbrirModalEditarOrden(numOrden);
            }
        }

        // Alias para el XAML
        private void ModificarOrden_Click(object sender, RoutedEventArgs e)
        {
            BtnEditarOrden_Click(sender, e);
        }

        // =========================
        // ABRIR MODAL EDITAR ORDEN
        // =========================
        private void AbrirModalEditarOrden(int numOrden)
        {
            _editarOrdenModal = new EditarOrdenView(numOrden);
            _editarOrdenModal.ModalCerrado += OnModalEditarCerrado;
            ModalContentContainer.Child = _editarOrdenModal;
            ModalOverlay.Visibility = Visibility.Visible;
        }

        // =========================
        // MANEJADOR DE CIERRE DEL MODAL EDITAR
        // =========================
        private void OnModalEditarCerrado(bool cambiosGuardados)
        {
            CerrarModal();
            if (cambiosGuardados)
            {
                CargarOrdenes();
            }
        }

        // =========================
        // CERRAR MODAL
        // =========================
        private void CerrarModal()
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
            ModalContentContainer.Child = null;

            if (_agregarOrdenesModal != null)
            {
                _agregarOrdenesModal.ModalCerrado -= OnModalAgregarCerrado;
                _agregarOrdenesModal = null;
            }

            if (_editarOrdenModal != null)
            {
                _editarOrdenModal.ModalCerrado -= OnModalEditarCerrado;
                _editarOrdenModal = null;
            }
        }

        // =========================
        // COBRAR ORDEN (ACTUALIZADO CON ID_CORTE)
        // =========================
        private void BtnCobrar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Orden orden)
            {
                if (MessageBox.Show($"¿Cobrar ${orden.Total} de la Orden #{orden.NumOrden}?", "Confirmar",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    using (MySqlConnection con = ConexionBD.ObtenerConexion())
                    {
                        con.Open();
                        MySqlTransaction transaction = con.BeginTransaction();

                        try
                        {
                            DateTime fechaVenta = DateTime.Now;

                            // Obtener el ID del corte activo
                            int idCorte = ObtenerIdCorteActivo(con, transaction);

                            // Registrar cada producto como venta CON ID_CORTE
                            foreach (var p in orden.Productos)
                            {
                                string venta = @"
                                    INSERT INTO VENTAS (CONCEPTO, MONTO, USUARIO, FECHA, ID_CORTE) 
                                    VALUES (@c, @m, @u, @fecha, @idCorte)";

                                MySqlCommand v = new MySqlCommand(venta, con, transaction);
                                v.Parameters.AddWithValue("@c", p.Concepto);
                                v.Parameters.AddWithValue("@m", p.Costo);
                                v.Parameters.AddWithValue("@u", Sesion.Usuario);
                                v.Parameters.AddWithValue("@fecha", fechaVenta);
                                v.Parameters.AddWithValue("@idCorte", idCorte); // NUEVO
                                v.ExecuteNonQuery();
                            }

                            // Marcar orden como completada
                            string cerrar = "UPDATE PREORDEN SET ESTADO = 1 WHERE NUM_ORDEN = @n";
                            MySqlCommand c = new MySqlCommand(cerrar, con, transaction);
                            c.Parameters.AddWithValue("@n", orden.NumOrden);
                            c.ExecuteNonQuery();

                            transaction.Commit();

                            // Recargar órdenes
                            CargarOrdenes();

                            MessageBox.Show($"Orden #{orden.NumOrden} cobrada exitosamente", "Éxito",
                                            MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show($"Error al cobrar la orden: {ex.Message}", "Error",
                                            MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        // Método para obtener el ID del corte activo
        private int ObtenerIdCorteActivo(MySqlConnection con, MySqlTransaction transaction)
        {
            string sql = @"
                SELECT ID 
                FROM CORTES 
                WHERE H_FINAL IS NULL 
                ORDER BY ID DESC 
                LIMIT 1";

            MySqlCommand cmd = new MySqlCommand(sql, con, transaction);
            object result = cmd.ExecuteScalar();

            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result);
            }
            else
            {
                throw new Exception("No hay un corte activo. Debes iniciar un corte primero.");
            }
        }

        // =========================
        // NAVEGACIÓN
        // =========================
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