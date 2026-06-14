using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PuntoVenta2.Models
{
    public class TiempoConsola : INotifyPropertyChanged
    {
        private TimeSpan _tiempoTranscurrido;
        private DateTime? _horaInicio;
        private int _dineroPagado;
        private bool _activo;
        private bool _terminoAutomaticamente = false; // NUEVO: Para saber cómo terminó

        public string Consola { get; set; }
        public int TarifaPorHora { get; set; } = 20;

        // NUEVA PROPERTY: Para saber si terminó automáticamente
        public bool TerminoAutomaticamente
        {
            get => _terminoAutomaticamente;
            set
            {
                _terminoAutomaticamente = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CostoEstimado));
                OnPropertyChanged(nameof(CostoEstimadoStr));
            }
        }

        public DateTime? HoraInicio
        {
            get => _horaInicio;
            set
            {
                _horaInicio = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HoraInicioStr));
                OnPropertyChanged(nameof(HoraFinEstimadaStr));
            }
        }

        public string HoraInicioStr => HoraInicio?.ToString("HH:mm:ss") ?? "";

        public TimeSpan TiempoTranscurrido
        {
            get => _tiempoTranscurrido;
            set
            {
                _tiempoTranscurrido = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TiempoTranscurridoStr));
                OnPropertyChanged(nameof(CostoEstimado));
                OnPropertyChanged(nameof(CostoEstimadoStr));
                OnPropertyChanged(nameof(HoraFinEstimadaStr));
                OnPropertyChanged(nameof(TiempoUsadoPagado));
                OnPropertyChanged(nameof(TiempoExcedido));
            }
        }

        public string TiempoTranscurridoStr => TiempoTranscurrido.ToString(@"hh\:mm\:ss");

        // Tiempo que SÍ se usó del tiempo pagado
        public TimeSpan TiempoUsadoPagado
        {
            get
            {
                if (TiempoTranscurrido <= TiempoPagado)
                    return TiempoTranscurrido;
                else
                    return TiempoPagado;
            }
        }

        // Tiempo excedido
        public TimeSpan TiempoExcedido
        {
            get
            {
                if (TiempoTranscurrido > TiempoPagado)
                    return TiempoTranscurrido - TiempoPagado;
                return TimeSpan.Zero;
            }
        }

        public TimeSpan TiempoPagado
        {
            get
            {
                if (DineroPagado <= 0) return TimeSpan.Zero;
                double minutosPagados = DineroPagado * 3.0;
                return TimeSpan.FromMinutes(minutosPagados);
            }
        }

        public int DineroPagado
        {
            get => _dineroPagado;
            set
            {
                _dineroPagado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TiempoPagado));
                OnPropertyChanged(nameof(CostoEstimado));
                OnPropertyChanged(nameof(CostoEstimadoStr));
                OnPropertyChanged(nameof(HoraFinEstimadaStr));
                OnPropertyChanged(nameof(TiempoUsadoPagado));
                OnPropertyChanged(nameof(TiempoExcedido));
            }
        }

        public string HoraFinEstimadaStr
        {
            get
            {
                if (!Activo || !HoraInicio.HasValue)
                    return "-";

                DateTime horaFin = HoraInicio.Value.Add(TiempoPagado);
                return horaFin.ToString("HH:mm:ss");
            }
        }

        // MODIFICADO: Lógica de costo según cómo terminó
        public int CostoEstimado
        {
            get
            {
                if (!Activo || !HoraInicio.HasValue || TiempoTranscurrido.TotalMinutes <= 0)
                    return 0;

                // CASO 1: Terminó AUTOMÁTICAMENTE → Cobrar solo lo pagado
                if (TerminoAutomaticamente)
                {
                    Console.WriteLine($"💰 [AUTO] Cobrando solo pagado: ${DineroPagado}");
                    return DineroPagado;
                }

                // CASO 2: Terminó MANUALMENTE → Cobrar proporcional
                else
                {
                    // Calcular cuánto tiempo usó (en minutos)
                    double minutosUsados = TiempoTranscurrido.TotalMinutes;

                    // Cada 3 minutos cuesta $1 (mínimo $1)
                    double bloquesUsados = minutosUsados / 3.0;
                    int bloquesACobrar = (int)Math.Ceiling(bloquesUsados);

                    // Máximo cobrar lo pagado
                    int costo = Math.Min(bloquesACobrar, DineroPagado);

                    // Mínimo $1 si jugó al menos 1 minuto
                    costo = Math.Max(costo, 1);

                    Console.WriteLine($"💰 [MANUAL] Tiempo usado: {minutosUsados} min → Bloques: {bloquesUsados:F2} → Cobrar: ${costo} (Pagó: ${DineroPagado})");
                    return costo;
                }
            }
        }

        public string CostoEstimadoStr => $"${CostoEstimado}";

        public bool Activo
        {
            get => _activo;
            set
            {
                _activo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CostoEstimado));
                OnPropertyChanged(nameof(CostoEstimadoStr));
                OnPropertyChanged(nameof(HoraFinEstimadaStr));
            }
        }

        // MÉTODO PARA RESETEAR
        public void Resetear()
        {
            Activo = false;
            HoraInicio = null;
            TiempoTranscurrido = TimeSpan.Zero;
            DineroPagado = 0;
            TerminoAutomaticamente = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}