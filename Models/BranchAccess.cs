using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Berca_Backend.Models
{
    /// <summary>
    /// Represents user permissions for specific branches
    /// Required for frontend multi-branch integration
    /// </summary>
    public class BranchAccess
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int BranchId { get; set; }

        public bool CanRead { get; set; } = true;
        public bool CanWrite { get; set; } = false;
        public bool CanApprove { get; set; } = false;
        public bool CanTransfer { get; set; } = false;

        [Required]
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int AssignedBy { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Branch Branch { get; set; } = null!;
        public virtual User AssignedByUser { get; set; } = null!;

        // Helper methods
        public bool HasPermission(BranchPermission permission)
        {
            return permission switch
            {
                BranchPermission.Read => CanRead,
                BranchPermission.Write => CanWrite,
                BranchPermission.Approve => CanApprove,
                BranchPermission.Transfer => CanTransfer,
                _ => false
            };
        }

        public void GrantPermission(BranchPermission permission)
        {
            switch (permission)
            {
                case BranchPermission.Read:
                    CanRead = true;
                    break;
                case BranchPermission.Write:
                    CanWrite = true;
                    break;
                case BranchPermission.Approve:
                    CanApprove = true;
                    break;
                case BranchPermission.Transfer:
                    CanTransfer = true;
                    break;
            }
        }

        public void RevokePermission(BranchPermission permission)
        {
            switch (permission)
            {
                case BranchPermission.Read:
                    CanRead = false;
                    break;
                case BranchPermission.Write:
                    CanWrite = false;
                    break;
                case BranchPermission.Approve:
                    CanApprove = false;
                    break;
                case BranchPermission.Transfer:
                    CanTransfer = false;
                    break;
            }
        }
    }

    public enum BranchPermission
    {
        Read,
        Write,
        Approve,
        Transfer
    }
}