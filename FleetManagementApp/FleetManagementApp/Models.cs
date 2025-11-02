using System;
using System.Collections.Generic;

// Простір імен вашого проекту
namespace FleetManagement.Data.Models
{
    // --- Моделі Сутностей (відображення таблиць) ---

    // Модель для таблиці Vehicles
    public class Vehicle
    {
        public int Id { get; set; }
        public string LicensePlate { get; set; }
        public string Vin { get; set; }
        public int ModelId { get; set; }
        public int? CurrentDriverId { get; set; }
        public int CurrentMileage { get; set; }
        public int StatusId { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }

    // Модель для таблиці Drivers
    public class Driver
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string LicenseNumber { get; set; }
        public DateTime LicenseExpiryDate { get; set; }
        public bool IsDeleted { get; set; }
        // ... і так далі для всіх полів
    }

    // Модель для таблиці Trips
    public class Trip
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public int DriverId { get; set; }
        public int? RouteId { get; set; }
        public DateTime TripStartTime { get; set; }
        public DateTime? TripEndTime { get; set; }
        public int StartMileage { get; set; }
        public int? EndMileage { get; set; }
        public string Purpose { get; set; }
        // ... і так далі
    }

    // --- Моделі для View (Розрізів) ---

    // Модель для View 'v_active_fleet'
    // Вона містить поля з кількох таблиць
    public class ActiveFleetVehicle
    {
        public int Id { get; set; }
        public string LicensePlate { get; set; }
        public string Vin { get; set; }
        public string ModelName { get; set; }
        public string ManufacturerName { get; set; }
        public int Year { get; set; }
        public int CurrentMileage { get; set; }
        public string CurrentStatus { get; set; }
        public int? DriverId { get; set; }
        public string DriverName { get; set; } // Може бути null
    }
}