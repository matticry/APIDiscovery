namespace APIDiscovery.Models.DTOs;

public class SubscriptionDatesDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public char Status { get; set; }
    public bool IsActive => Status == 'A';
    public bool IsExpired => EndDate.HasValue && EndDate.Value < DateTime.Now;
    public int RemainingDays => EndDate.HasValue ? 
        (EndDate.Value > DateTime.Now ? (EndDate.Value - DateTime.Now).Days : 0) : 0;
}