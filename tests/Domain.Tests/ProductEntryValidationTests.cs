using System.ComponentModel.DataAnnotations;
using Domain;

namespace Domain.Tests;

public class ProductEntryValidationTests
{
    [Fact]
    public void ProductEntry_WithValidData_PassesValidation()
    {
        var entry = new ProductEntry
        {
            Name = "Operator",
            ProductModel = "Model-X",
            PartNumber = "PN-123",
            Quantity = 5,
            Price = 19.95m,
            UserId = "user-1"
        };

        var results = Validate(entry);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ProductEntry_WithInvalidQuantity_FailsValidation(int quantity)
    {
        var entry = new ProductEntry
        {
            Name = "Operator",
            ProductModel = "Model-X",
            PartNumber = "PN-123",
            Quantity = quantity,
            Price = 10,
            UserId = "user-1"
        };

        var results = Validate(entry);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ProductEntry.Quantity)));
    }

    [Fact]
    public void ProductEntry_WithNegativePrice_FailsValidation()
    {
        var entry = new ProductEntry
        {
            Name = "Operator",
            ProductModel = "Model-X",
            PartNumber = "PN-123",
            Quantity = 1,
            Price = -1,
            UserId = "user-1"
        };

        var results = Validate(entry);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ProductEntry.Price)));
    }

    private static List<ValidationResult> Validate(object instance)
    {
        var context = new ValidationContext(instance);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
        return results;
    }
}
