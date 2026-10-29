using UnityEngine;

namespace Gumusdere.Companion.Sensing
{
    /// <summary>
    /// Ajanın çevresini algılaması için sensör sözleşmesi.
    /// Durumlar somut sensörü (RaycastSensor) değil bu arayüzü kullanır.
    /// </summary>
    public interface IObstacleSensor
    {
        /// <summary>En son yapılan okuma.</summary>
        SensorReading Latest { get; }

        /// <summary>Şimdi yeni bir okuma yapar.</summary>
        SensorReading Scan();

        /// <summary>Hedef, arada bir engel olmadan görülebiliyor mu.</summary>
        bool CanSee(Transform target, Vector3 point);
    }
}
