namespace PuntoVenta2.Models
{
    public class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public int Costo { get; set; }
        public int Inventario { get; set; }
    }

    public class ProductoSeleccionado
    {
        public string Nombre { get; set; }
        public int Costo { get; set; }
    }
}