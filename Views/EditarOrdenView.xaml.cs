using PuntoVenta2.Data;
using PuntoVenta2.Models;
using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PuntoVenta2.Views
{
    public partial class EditarOrdenView : UserControl, INotifyPropertyChanged
    {
        // Evento para notificar cierre del modal
        public delegate void ModalCerradoHandler(bool cambiosGuardados);
        public event ModalCerradoHandler ModalCerrado;

        public ObservableCollection<Producto> Productos { get; set; }
        public ObservableCollection<ProductoOrden> ProductosOrdenActual { get; set; }
        public ObservableCollection<ProductoOrden> ProductosNuevos { get; set; }

        private int _total;
        public int Total
        {
            get => _total;
            set
            {
                _total = value;
                OnPropertyChanged();
            }
        }

        private string _comentarios;
        public string Comentarios
        {
            get => _comentarios;
            set
            {
                _comentarios = value;
                OnPropertyChanged();
            }
        }

        private int _numOrden;
        public int NumOrden
        {
            get => _numOrden;
            set
            {
                _numOrden = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TituloOrden));
            }
        }

        public string TituloOrden => $"Editar Orden #{NumOrden}";

        public EditarOrdenView(int numOrden)
        {
            InitializeComponent();

            NumOrden = numOrden;
            Productos = new ObservableCollection<Producto>();
            ProductosOrdenActual = new ObservableCollection<ProductoOrden>();
            ProductosNuevos = new ObservableCollection<ProductoOrden>();
            DataContext = this;

            CargarProductos();
            CargarOrdenActual();
        }

        // Cargar productos disponibles desde la base de datos
        private void CargarProductos()
        {
            Productos.Clear();

            using (MySqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                string sql = "SELECT * FROM PRODUCTOS WHERE INVENTARIO > 0 ORDER BY NOMBRE";
                MySqlCommand cmd = new MySqlCommand(sql, con);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Productos.Add(new Producto
                        {
                            Id = reader.GetInt32("ID"),
                            Nombre = reader.GetString("NOMBRE"),
                            Costo = reader.GetInt32("COSTO"),
                            Inventario = reader.GetInt32("INVENTARIO")
                        });
                    }
                }
            }
        }

        // Cargar los productos actuales de la orden
        private void CargarOrdenActual()
        {
            ProductosOrdenActual.Clear();
            Total = 0;

            using (MySqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();

                // 1. Cargar comentarios y total de PREORDEN
                string sqlOrden = "SELECT COMENTARIOS, TOTAL FROM PREORDEN WHERE NUM_ORDEN = @numOrden";
                MySqlCommand cmdOrden = new MySqlCommand(sqlOrden, con);
                cmdOrden.Parameters.AddWithValue("@numOrden", NumOrden);

                using (MySqlDataReader readerOrden = cmdOrden.ExecuteReader())
                {
                    if (readerOrden.Read())
                    {
                        Comentarios = readerOrden["COMENTARIOS"]?.ToString() ?? "";
                        Total = readerOrden.GetInt32("TOTAL");
                    }
                    readerOrden.Close();
                }

                // 2. Cargar productos de la orden
                string sqlProductos = "SELECT ID, CONCEPTO, COSTO FROM PRODUCTOS_ORDEN WHERE NUM_ORDEN = @numOrden";
                MySqlCommand cmdProductos = new MySqlCommand(sqlProductos, con);
                cmdProductos.Parameters.AddWithValue("@numOrden", NumOrden);

                using (MySqlDataReader reader = cmdProductos.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var producto = new ProductoOrden
                        {
                            Id = reader.GetInt32("ID"),
                            Concepto = reader.GetString("CONCEPTO"),
                            Costo = reader.GetInt32("COSTO")
                        };
                        ProductosOrdenActual.Add(producto);
                    }
                }
            }
        }

        // Agregar nuevo producto a la orden
        private void BtnAgregarProducto_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Producto producto)
            {
                var nuevoProducto = new ProductoOrden
                {
                    Concepto = producto.Nombre,
                    Costo = producto.Costo
                };

                ProductosOrdenActual.Add(nuevoProducto);
                ProductosNuevos.Add(nuevoProducto); // Para tracking
                Total += producto.Costo;
            }
        }

        // Eliminar producto de la orden (con DELETE en BD si ya existía)
        private void BtnEliminarDeOrden_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ProductoOrden producto)
            {
                // Si tiene Id, significa que ya existe en BD
                if (producto.Id > 0)
                {
                    // Eliminar de la base de datos
                    EliminarProductoDeBD(producto.Id);
                }

                // Eliminar de la lista local
                ProductosOrdenActual.Remove(producto);
                ProductosNuevos.Remove(producto); // Si estaba en nuevos
                Total -= producto.Costo;
            }
        }

        // Eliminar producto de la base de datos
        private void EliminarProductoDeBD(int idProducto)
        {
            try
            {
                using (MySqlConnection con = ConexionBD.ObtenerConexion())
                {
                    con.Open();
                    string sql = "DELETE FROM PRODUCTOS_ORDEN WHERE ID = @id";
                    MySqlCommand cmd = new MySqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@id", idProducto);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar producto: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Botón Cancelar
        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            ModalCerrado?.Invoke(false);
        }

        // Guardar cambios en la orden
        private void BtnGuardarCambios_Click(object sender, RoutedEventArgs e)
        {
            using (MySqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                MySqlTransaction transaction = con.BeginTransaction();

                try
                {
                    // 1. Actualizar comentarios y total en PREORDEN
                    string sqlActualizarOrden = @"
                        UPDATE PREORDEN 
                        SET COMENTARIOS = @comentarios, TOTAL = @total 
                        WHERE NUM_ORDEN = @numOrden";

                    MySqlCommand cmdActualizar = new MySqlCommand(sqlActualizarOrden, con, transaction);
                    cmdActualizar.Parameters.AddWithValue("@comentarios", Comentarios);
                    cmdActualizar.Parameters.AddWithValue("@total", Total);
                    cmdActualizar.Parameters.AddWithValue("@numOrden", NumOrden);
                    cmdActualizar.ExecuteNonQuery();

                    // 2. Insertar productos nuevos (los que no tienen Id)
                    foreach (var producto in ProductosNuevos)
                    {
                        if (producto.Id == 0) // Solo los nuevos
                        {
                            string sqlInsertar = @"
                                INSERT INTO PRODUCTOS_ORDEN (CONCEPTO, COSTO, NUM_ORDEN)
                                VALUES (@concepto, @costo, @numOrden)";

                            MySqlCommand cmdInsertar = new MySqlCommand(sqlInsertar, con, transaction);
                            cmdInsertar.Parameters.AddWithValue("@concepto", producto.Concepto);
                            cmdInsertar.Parameters.AddWithValue("@costo", producto.Costo);
                            cmdInsertar.Parameters.AddWithValue("@numOrden", NumOrden);
                            cmdInsertar.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();

                    MessageBox.Show($"Orden #{NumOrden} actualizada exitosamente", "Éxito",
                                    MessageBoxButton.OK, MessageBoxImage.Information);

                    ModalCerrado?.Invoke(true);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Error al actualizar la orden: {ex.Message}", "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}