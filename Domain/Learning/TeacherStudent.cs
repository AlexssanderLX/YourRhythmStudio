using YourRhythmStudio.Domain.Users;

namespace YourRhythmStudio.Domain.Learning
{
    public sealed class TeacherStudent
    {
        private TeacherStudent() { }
        
        public TeacherStudent(
            Guid schoolId,
            Guid teacherProfileId,
            Guid studentProfileId,
            DateTime utcNow)
        {
            if (schoolId == Guid.Empty)
                throw new ArgumentException("SchoolId is required.", nameof(schoolId));

            if (teacherProfileId == Guid.Empty)
                throw new ArgumentException("TeacherProfileId is required.", nameof(teacherProfileId));

            if (studentProfileId == Guid.Empty)
                throw new ArgumentException("StudentProfileId is required.", nameof(studentProfileId));
            
            Id = Guid.NewGuid();
            SchoolId = schoolId;
            TeacherProfileId = teacherProfileId;
            StudentProfileId= studentProfileId;
            IsActive = true;
            CreatedAtUtc = utcNow;
        }

        public Guid Id { get; private set; }
        public Guid SchoolId { get; private set; }
        public Guid TeacherProfileId { get; private set; }
        public Guid StudentProfileId { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }
        public DateTime? DeactivatedAtUtc { get; private set; }

        public void Deactivate(DateTime utcNow)
        {
            if (!IsActive)
                return;

             IsActive = false;
            DeactivatedAtUtc = utcNow;
        }
        public void Reactivate()
        {
            if (IsActive)
                return;

            IsActive = true;
            DeactivatedAtUtc = null;
        }

    }
}
