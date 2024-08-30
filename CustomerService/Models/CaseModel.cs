using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CustomerService.Models
{
    public class CaseModel
    {
        public Guid CaseId { get; set; }

        [Required]
        [MaxLength(100)]
        
        public string Title { get; set; }

        [DisplayName("Case Number")]
        public string CaseNumber { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; }

        [Required]
        public string Priority { get; set; }

        public string Status { get; set; }

        public string Owner { get; set; }

        public DateTime CreatedOn { get; set; }

        [Required]
        public string Customer { get; set; }

    }
}
