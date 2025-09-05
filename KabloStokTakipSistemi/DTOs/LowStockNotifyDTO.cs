// örnek DTO’lar (DTO/Alerts klasöründe),

namespace KabloStokTakipSistemi.DTOs;
    
public sealed record LowStockNotifyItem(string Kind, string Key, int Current, int Threshold);
// Kind: "Single" / "Multi"
// Key : Single için renk; Multi için MultiCableID veya isim

public sealed record LowStockNotifyResult(
    int ScannedCount,
    int BelowThresholdCount,
    int RecipientCount,
    IReadOnlyList<LowStockNotifyItem> Items
);

public sealed record AlertNotifyResult(
    int RecipientCount,
    int AlertId,
    DateTime NotifiedAt
);