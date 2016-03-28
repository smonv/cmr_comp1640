namespace CMR.Models
{
    public class CourseAssignmentManager
    {
        public CourseAssignmentManager()
        {
        }

        public CourseAssignmentManager(string role, ApplicationUser user, CourseAssignment ca)
        {
            Role = role;
            User = user;
            CourseAssignment = ca;
        }

        public int Id { get; set; }
        public string Role { get; set; }
        public ApplicationUser User { get; set; }
        public CourseAssignment CourseAssignment { get; set; }
    }
}