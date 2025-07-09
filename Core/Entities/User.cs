using System.ComponentModel.DataAnnotations;

namespace SportsBetting.Api.Core.Entities
{
    public class User
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Password hash is required")]
        public string PasswordHash { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Full name is required")]
        [MinLength(2, ErrorMessage = "Full name must be at least 2 characters")]
        public string FullName { get; set; } = string.Empty;
        
        public decimal Balance { get; set; } = 1000.00m;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual ICollection<Bet> Bets { get; set; } = new List<Bet>();
        
        public bool HasSufficientBalance(decimal amount)
        {
            return Balance >= amount && amount > 0;
        }
        
        public bool DeductBalance(decimal amount)
        {
            if (!HasSufficientBalance(amount))
                return false;
                
            Balance -= amount;
            UpdatedAt = DateTime.UtcNow;
            return true;
        }
        
        public void AddToBalance(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be positive");
                
            Balance += amount;
            UpdatedAt = DateTime.UtcNow;
        }
        
        public bool IsValidEmail()
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(Email);
                return addr.Address == Email;
            }
            catch
            {
                return false;
            }
        }
    }
}
