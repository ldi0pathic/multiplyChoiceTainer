namespace DAL.Model;

public class Migration
{
    public int Id { get; set; }
    public string Version { get; set; }
    public DateTime AppliedAt { get; set; }
}