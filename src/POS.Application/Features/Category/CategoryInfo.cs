// POS.Application/Features/Category/CategoryInfo.cs
namespace POS.Application.Features.Category
{
    public class CategoryInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int BookCount { get; set; }
    }
}