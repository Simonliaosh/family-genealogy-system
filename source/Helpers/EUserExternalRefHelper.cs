using FamilyTree.Models;

namespace FamilyTree.Helpers;

/// <summary>v1 外部身份字段与旧表单 CustomerID/PartnerID/SupplierID 互转。</summary>
public static class EUserExternalRefHelper
{
    public static int? GetCustomerId(EUser u) => GetRefId(u, "CUSTOMER");
    public static int? GetPartnerId(EUser u) => GetRefId(u, "PARTNER");
    public static int? GetSupplierId(EUser u) => GetRefId(u, "SUPPLIER");

    public static void ApplyCustomerId(EUser u, int? id) => ApplyRef(u, "CUSTOMER", id);
    public static void ApplyPartnerId(EUser u, int? id) => ApplyRef(u, "PARTNER", id);
    public static void ApplySupplierId(EUser u, int? id) => ApplyRef(u, "SUPPLIER", id);

    private static int? GetRefId(EUser u, string type)
    {
        if (!string.Equals((u.ExternalRefType ?? "").Trim(), type, StringComparison.OrdinalIgnoreCase))
            return null;
        return int.TryParse((u.ExternalRefId ?? "").Trim(), out var id) ? id : null;
    }

    private static void ApplyRef(EUser u, string type, int? id)
    {
        if (!id.HasValue || id.Value <= 0)
        {
            if (string.Equals((u.ExternalRefType ?? "").Trim(), type, StringComparison.OrdinalIgnoreCase))
            {
                u.ExternalRefType = null;
                u.ExternalRefId = null;
            }
            return;
        }
        u.ExternalRefType = type;
        u.ExternalRefId = id.Value.ToString();
    }
}
