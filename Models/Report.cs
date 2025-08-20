using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Berca_Backend.Models
{
    /// <summary>
    /// Report model for managing custom and system reports
    /// Indonesian business context with proper error handling
    /// </summary>
    public class Report
    {
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// Report name in Indonesian
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public required string Name { get; set; }
        
        /// <summary>
        /// Report type classification
        /// </summary>
        [Required]
        [StringLength(50)]
        public required string ReportType { get; set; } // Sales, Inventory, Financial, Custom
        
        /// <summary>
        /// Optional report description
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }
        
        /// <summary>
        /// JSON configuration parameters for report generation
        /// </summary>
        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public required string Parameters { get; set; }
        
        /// <summary>
        /// Whether this report is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Whether this report is scheduled for automatic execution
        /// </summary>
        public bool IsScheduled { get; set; } = false;
        
        /// <summary>
        /// Cron expression for scheduled execution
        /// </summary>
        [StringLength(100)]
        public string? ScheduleExpression { get; set; }
        
        // ==================== BRANCH RELATIONSHIP ==================== //
        
        /// <summary>
        /// Branch ID for branch-specific reports (null = all branches)
        /// </summary>
        public int? BranchId { get; set; }
        
        /// <summary>
        /// Branch this report belongs to
        /// </summary>
        public virtual Branch? Branch { get; set; }
        
        // ==================== AUDIT FIELDS ==================== //
        
        /// <summary>
        /// User who created this report
        /// </summary>
        [Required]
        public int CreatedBy { get; set; }
        
        /// <summary>
        /// User who created this report (navigation property)
        /// </summary>
        public virtual User CreatedByUser { get; set; } = null!;
        
        /// <summary>
        /// User who last updated this report
        /// </summary>
        public int? UpdatedBy { get; set; }
        
        /// <summary>
        /// User who last updated this report (navigation property)
        /// </summary>
        public virtual User? UpdatedByUser { get; set; }
        
        /// <summary>
        /// When this report was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When this report was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // ==================== NAVIGATION PROPERTIES ==================== //
        
        /// <summary>
        /// Report execution history
        /// </summary>
        public virtual ICollection<ReportExecution> ReportExecutions { get; set; } = new List<ReportExecution>();
    }

    /// <summary>
    /// Report execution tracking model
    /// Tracks each time a report is generated
    /// </summary>
    public class ReportExecution
    {
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// Report that was executed
        /// </summary>
        [Required]
        public int ReportId { get; set; }
        
        /// <summary>
        /// Report navigation property
        /// </summary>
        public virtual Report Report { get; set; } = null!;
        
        /// <summary>
        /// Type of execution (Manual, Scheduled, API)
        /// </summary>
        [Required]
        [StringLength(50)]
        public required string ExecutionType { get; set; }
        
        /// <summary>
        /// JSON parameters used for this execution
        /// </summary>
        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public required string Parameters { get; set; }
        
        /// <summary>
        /// When execution started
        /// </summary>
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When execution completed (null if still running)
        /// </summary>
        public DateTime? CompletedAt { get; set; }
        
        /// <summary>
        /// Current execution status
        /// </summary>
        [StringLength(20)]
        public string Status { get; set; } = "Running"; // Running, Completed, Failed
        
        /// <summary>
        /// Error message if execution failed
        /// </summary>
        [StringLength(1000)]
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// File path where generated report is stored
        /// </summary>
        [StringLength(500)]
        public string? OutputPath { get; set; }
        
        /// <summary>
        /// Size of generated file in bytes
        /// </summary>
        public long? FileSizeBytes { get; set; }
        
        // ==================== USER TRACKING ==================== //
        
        /// <summary>
        /// User who executed this report
        /// </summary>
        public int? ExecutedBy { get; set; }
        
        /// <summary>
        /// User who executed this report (navigation property)
        /// </summary>
        public virtual User? ExecutedByUser { get; set; }
        
        // ==================== HELPER PROPERTIES ==================== //
        
        /// <summary>
        /// Duration of execution in milliseconds
        /// </summary>
        public long? ExecutionDurationMs => CompletedAt.HasValue && StartedAt != default
            ? (long)(CompletedAt.Value - StartedAt).TotalMilliseconds
            : null;
        
        /// <summary>
        /// Whether execution was successful
        /// </summary>
        public bool IsSuccessful => Status == "Completed" && string.IsNullOrEmpty(ErrorMessage);
        
        /// <summary>
        /// Formatted file size for display
        /// </summary>
        public string FileSizeDisplay => FileSizeBytes.HasValue
            ? FormatFileSize(FileSizeBytes.Value)
            : "N/A";
        
        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    /// <summary>
    /// Report template model for standardized report formats
    /// </summary>
    public class ReportTemplate
    {
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// Template name
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public required string Name { get; set; }
        
        /// <summary>
        /// Template category (Sales, Inventory, Financial)
        /// </summary>
        [Required]
        [StringLength(50)]
        public required string Category { get; set; }
        
        /// <summary>
        /// JSON template definition
        /// </summary>
        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public required string TemplateContent { get; set; }
        
        /// <summary>
        /// Template description
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }
        
        /// <summary>
        /// Whether this is a default system template
        /// </summary>
        public bool IsDefault { get; set; } = false;
        
        /// <summary>
        /// Whether this template is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        // ==================== AUDIT FIELDS ==================== //
        
        /// <summary>
        /// User who created this template
        /// </summary>
        [Required]
        public int CreatedBy { get; set; }
        
        /// <summary>
        /// User who created this template (navigation property)
        /// </summary>
        public virtual User CreatedByUser { get; set; } = null!;
        
        /// <summary>
        /// When this template was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When this template was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Report types enumeration for type safety
    /// </summary>
    public enum ReportType
    {
        Sales = 0,
        Inventory = 1,
        Financial = 2,
        Supplier = 3,
        Custom = 4,
        Analytics = 5
    }

    /// <summary>
    /// Export formats enumeration
    /// </summary>
    public enum ExportFormat
    {
        PDF = 0,
        Excel = 1,
        CSV = 2,
        JSON = 3
    }

    /// <summary>
    /// Report execution status enumeration
    /// </summary>
    public enum ReportExecutionStatus
    {
        Running = 0,
        Completed = 1,
        Failed = 2,
        Cancelled = 3
    }
}