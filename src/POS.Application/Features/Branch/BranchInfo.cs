namespace POS.Application.Features.Branch
{
    public class BranchInfo
    {
        public int Id { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string? Logo { get; set; }
        public string Status { get; set; } = "Active";
        public string? Description { get; set; }

    }
}