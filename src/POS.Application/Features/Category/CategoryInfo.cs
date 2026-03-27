namespace POS.Application.Features.Category
{
    public class CategoryInfo
    {
        public int     Id          { get; set; }
        public string  Name        { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Image       { get; set; } 
        public bool    IsActive    { get; set; }
    }
 
    public class CategoryInfoLookup
    {
        public int    Id   { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
 