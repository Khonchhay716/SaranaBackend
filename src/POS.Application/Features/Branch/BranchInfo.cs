// POS.Application/Features/Branch/BranchInfo.cs
namespace POS.Application.Features.Branch
{
    public class BranchInfo
    {
        public int     Id          { get; set; }
        public string  BranchName  { get; set; } = string.Empty;
        public string? Logo        { get; set; }
        public string  Status      { get; set; } = "Active";
        public string? Description { get; set; }

        public bool              IsDeleted   { get; set; }
        public DateTimeOffset    CreatedDate { get; set; }
        public string?           CreatedBy   { get; set; }
        public DateTimeOffset?   UpdatedDate { get; set; }
        public string?           UpdatedBy   { get; set; }
        public DateTimeOffset?   DeletedDate { get; set; }
        public string?           DeletedBy   { get; set; }
    }
}