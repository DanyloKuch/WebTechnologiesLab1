using FleetManagement.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FleetManagement.Data.Abstractions
{
    // Ми не успадковуємо IGenericRepository, бо наше завдання - 
    // викликати *тільки* збережені процедури, а не generic-методи.

    public interface IVehicleRepository
    {
        // 1. Метод для ЧИТАННЯ (використовує View 'v_active_fleet')
        Task<IEnumerable<ActiveFleetVehicle>> GetActiveFleetAsync();

        // 2. Метод для СТВОРЕННЯ (використовує SP 'sp_add_vehicle')
        Task AddVehicleAsync(Vehicle vehicle, int createdByUserId);

        // 3. Метод для ВИДАЛЕННЯ (використовує SP 'sp_soft_delete_vehicle')
        Task SoftDeleteVehicleAsync(int vehicleId, int deletedByUserId);

        // ...Тут можна додати інші методи для інших SP
    }
}