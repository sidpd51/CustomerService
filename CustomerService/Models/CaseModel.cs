using System.ComponentModel;

namespace CustomerService.Models
{
    public class CaseModel
    {
        public Guid CaseId { get; set; }

        public string Title { get; set; }

        [DisplayName("Case Number")]
        public string CaseNumber { get; set; }

        public string Description { get; set; }

        public string Priority { get; set; }

        public string Status { get; set; }

        public string Owner { get; set; }

        public DateTime CreatedOn { get; set; }

        public string Customer { get; set; }

    }
}
