using System.ComponentModel.DataAnnotations;

namespace CustomerService.Models
{
    public class ContactModel
    {
        public Guid Id { get; set; }

        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string Account { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Phone { get; set; }
        [Required]
        public DateTime CreatedOn { get; set; }

        public string ShowInterest { get; set; }
        
        public int []? Interest { get; set; }

    }
}
