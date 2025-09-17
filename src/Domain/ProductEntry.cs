using System.ComponentModel.DataAnnotations;

namespace Domain;

public sealed class ProductEntry
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;

    [Required]
    [StringLength(80)]
    public string ProductModel { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string PartNumber { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue)]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    [StringLength(120)]
    public string? DeviceId { get; set; }

    public string UserId { get; set; } = string.Empty;
}
