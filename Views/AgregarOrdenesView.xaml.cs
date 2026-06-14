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
    public partial class AgregarOrdenesView : UserControl, INotifyPropertyChanged
    {
        public delegate void ModalCerradoHandler(bool ordenCreada);
        public event ModalCerradoHandler ModalCerrado;

        public ObservableCollection<Producto> Productos { get; set; }
        public ObservableCollection<ProductoSeleccionado> ProductosSeleccionados { get; set; }

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

        public AgregarOrdenesView()
        {
            InitializeComponent();

            Productos = new ObservableCollection<Producto>();
            ProductosSeleccionados = new ObservableCollection<ProductoSeleccionado>();
            DataContext = this;

            CargarProductos();
        }

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

        private void BtnAgregarProducto_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Producto producto)
            {
                ProductosSeleccionados.Add(new ProductoSeleccionado
                {
                    Nombre = producto.Nombre,
                    Costo = producto.Costo
                });
                Total += producto.Costo;
            }
        }

        private void BtnEliminarProducto_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ProductoSeleccionado producto)
            {
                ProductosSeleccionados.Remove(producto);
                Total -= producto.Costo;
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            ModalCerrado?.Invoke(false);
        }

        private void BtnAgregarOrden_Click(object sender, RoutedEventArgs e)
        {
            if (ProductosSeleccionados.Count == 0)
            {
                MessageBox.Show("Agrega al menos un producto a la orden", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (MySqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                MySqlTransaction transaction = con.BeginTransaction();

                try
                {
                    string sqlOrden = @"
                        INSERT INTO PREORDEN (TOTAL, COMENTARIOS, ESTADO) 
                        VALUES (@total, @comentarios, 0);
                        SELECT LAST_INSERT_ID();";

                    MySqlCommand cmdOrden = new MySqlCommand(sqlOrden, con, transaction);
                    cmdOrden.Parameters.AddWithValue("@total", Total);
                    cmdOrden.Parameters.AddWithValue("@comentarios", txtComentarios.Text);

                    int numOrden = Convert.ToInt32(cmdOrden.ExecuteScalar());

                    foreach (var producto in ProductosSeleccionados)
                    {
                        string sqlProducto = @"
                            INSERT INTO PRODUCTOS_ORDEN (CONCEPTO, COSTO, NUM_ORDEN)
                            VALUES (@concepto, @costo, @numOrden)";

                        MySqlCommand cmdProducto = new MySqlCommand(sqlProducto, con, transaction);
                        cmdProducto.Parameters.AddWithValue("@concepto", producto.Nombre);
                        cmdProducto.Parameters.AddWithValue("@costo", producto.Costo);
                        cmdProducto.Parameters.AddWithValue("@numOrden", numOrden);

                        cmdProducto.ExecuteNonQuery();
                    }

                    transaction.Commit();

                    MessageBox.Show($"Orden #{numOrden} creada exitosamente", "Éxito",
                                    MessageBoxButton.OK, MessageBoxImage.Information);

                    ModalCerrado?.Invoke(true);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Error al crear la orden: {ex.Message}", "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}