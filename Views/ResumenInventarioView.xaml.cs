using PuntoVenta2.Data;
using PuntoVenta2.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PuntoVenta2.Views
{
    public partial class ResumenInventarioView : UserControl
    {
        public ObservableCollection<Producto> Productos { get; set; }
        private Producto _productoSeleccionado;

        public ResumenInventarioView()
        {
            InitializeComponent();
            Productos = new ObservableCollection<Producto>();
            GridInventario.ItemsSource = Productos;
            CargarInventario();
        }

        // ========== CARGAR INVENTARIO ==========

        private void CargarInventario()
        {
            try
            {
                Productos.Clear();

                using (MySqlConnection con = ConexionBD.ObtenerConexion())
                {
                    con.Open();

                    string sql = @"
                        SELECT ID, NOMBRE, COSTO, INVENTARIO
                        FROM PRODUCTOS 
                        ORDER BY NOMBRE ASC";

                    MySqlCommand cmd = new MySqlCommand(sql, con);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var producto = new Producto
                            {
                                Id = reader.GetInt32("ID"),
                                Nombre = reader.GetString("NOMBRE"),
                                Costo = reader.GetInt32("COSTO"),
                                Inventario = reader.GetInt32("INVENTARIO")
                            };
                            Productos.Add(producto);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar inventario: {ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        // ========== EDITAR PRODUCTO ==========

        private void BtnEditarProducto_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Producto producto)
            {
                _productoSeleccionado = producto;
                MostrarModalEditar(producto);
            }
        }

        private void MostrarModalEditar(Producto producto)
        {
            ModalTitulo.Text = $"Editar: {producto.Nombre}";
            TxtNombreProducto.Text = producto.Nombre;
            TxtCantidadActual.Text = producto.Inventario.ToString();
            TxtNuevaCantidad.Text = producto.Inventario.ToString();
            TxtMensajeError.Visibility = Visibility.Collapsed;

            ModalInventario.Visibility = Visibility.Visible;
            TxtNuevaCantidad.Focus();
            TxtNuevaCantidad.SelectAll();
        }

        private void BtnConfirmarInventario_Click(object sender, RoutedEventArgs e)
        {
            if (_productoSeleccionado == null) return;

            if (!int.TryParse(TxtNuevaCantidad.Text, out int nuevaCantidad) || nuevaCantidad < 0)
            {
                MostrarError("Ingresa una cantidad válida (número positivo).");
                return;
            }

            try
            {
                using (MySqlConnection con = ConexionBD.ObtenerConexion())
                {
                    con.Open();

                    string sql = @"
                        UPDATE PRODUCTOS 
                        SET INVENTARIO = @inventario 
                        WHERE ID = @id";

                    MySqlCommand cmd = new MySqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@inventario", nuevaCantidad);
                    cmd.Parameters.AddWithValue("@id", _productoSeleccionado.Id);

                    int rows = cmd.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        // Actualizar en la lista local
                        _productoSeleccionado.Inventario = nuevaCantidad;

                        // Refrescar el DataGrid
                        GridInventario.Items.Refresh();

                        MessageBox.Show($"Inventario actualizado exitosamente\n" +
                                       $"Producto: {_productoSeleccionado.Nombre}\n" +
                                       $"Nueva cantidad: {nuevaCantidad}",
                                       "Inventario actualizado",
                                       MessageBoxButton.OK,
                                       MessageBoxImage.Information);

                        ModalInventario.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        MostrarError("No se pudo actualizar el inventario.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar inventario: {ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void BtnCancelarModal_Click(object sender, RoutedEventArgs e)
        {
            ModalInventario.Visibility = Visibility.Collapsed;
            _productoSeleccionado = null;
        }

        // ========== VALIDACIÓN ==========

        private void TxtCantidad_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Permitir solo números
            e.Handled = !int.TryParse(e.Text, out _);
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