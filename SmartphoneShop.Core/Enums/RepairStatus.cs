namespace SmartphoneShop.Core.Enums;

public enum RepairStatus
{
    New,                    // Новая
    DeliveryToCenter,      // Доставка в центр
    AcceptedAtCenter,       // Принят в центр
    InQueue,                // В очереди
    Diagnostics,           // Диагностика
    RepairApproval,        // Ожидает одобрения
    ReadyForRepair,         // Готов к ремонту (после одобрения)
    InRepair,               // В ремонте
    ReadyForPickup,        // Готов к выдаче
    Completed,             // Завершён
    Cancelled               // Отменён
}
