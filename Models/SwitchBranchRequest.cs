using System.ComponentModel.DataAnnotations;

namespace Berca_Backend.Models
{
    /// <summary>
    /// Request model for switching branch context
    /// </summary>
    public class SwitchBranchRequest
    {
        [Required]
        public int BranchId { get; set; }
    }
}