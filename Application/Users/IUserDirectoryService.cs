namespace YourRhythmStudio.Application.Users;

public interface IUserDirectoryService
{
    Task<SchoolSummary> CreateSchoolAsync(CreateSchoolRequest request, CancellationToken cancellationToken = default);

    Task<TeacherSummary> CreateTeacherAsync(CreateTeacherRequest request, CancellationToken cancellationToken = default);

    Task<StudentSummary> CreateStudentAsync(CreateStudentRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SchoolSummary>> ListSchoolsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TeacherSummary>> ListTeachersAsync(Guid schoolId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StudentSummary>> ListStudentsAsync(Guid schoolId, CancellationToken cancellationToken = default);
}
