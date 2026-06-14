using System.Collections.Generic;

namespace PuntoVenta2.Models
{
    public class Orden
    {
        public int NumOrden { get; set; }
        public int Total { get; set; }
        public string Comentarios { get; set; }
        public List<ProductoOrden> Productos { get; set; }
    }

    public class ProductoOrden
    {
        public int Id { get; set; }  // ← AGREGAR ESTA PROPIEDAD
        public string Concepto { get; set; }
        public int Costo { get; set; }
    }
}