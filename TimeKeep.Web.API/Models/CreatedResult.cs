namespace TimeKeep.Web.API.Models
{
    public sealed class CreatedResult
    {
        public TimeKeepEntry Modified { get; set; }
        public TimeKeepEntry New { get; set; }
    }
}