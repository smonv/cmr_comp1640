namespace CMR.Models
{
    public class FacultyAssignmentManager
    {
        public FacultyAssignmentManager()
        {
        }

        public FacultyAssignmentManager(string role, ApplicationUser user, FacultyAssignment fa)
        {
            Role = role;
            User = user;
            FacultyAssignment = fa;
        }

        public int Id { get; set; }
        public string Role { get; set; }
        public virtual ApplicationUser User { get; set; }
        public virtual FacultyAssignment FacultyAssignment { get; set; }
    }
}