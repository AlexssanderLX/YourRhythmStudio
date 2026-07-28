using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using YourRhythmStudio.Application.Learning;
using YourRhythmStudio.Application.Users;
using YourRhythmStudio.Domain;
using YourRhythmStudio.Domain.Learning;
using YourRhythmStudio.Domain.Learning.Enums;
using YourRhythmStudio.Domain.Users;
using YourRhythmStudio.Infrastructure.Data;

namespace YourRhythmStudio.Tests;

public sealed class RepertoireServiceTests
{
    // ── Add ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddRepertoireAsync_WithLink_CreatesItem()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var result = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Clair de Lune", null, "https://youtube.com", null));

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Clair de Lune", result.Title);
        Assert.Single(db.RepertoireItems);
    }

    [Fact]
    public async Task AddRepertoireAsync_WithNotesOnly_CreatesItem()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var result = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Escalas", "Praticar diariamente.", null, null));

        Assert.Equal("Escalas", result.Title);
        Assert.Single(db.RepertoireItems);
    }

    [Fact]
    public async Task AddRepertoireAsync_WithTitleOnly_Succeeds()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var result = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Titulo", null, null, null));

        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task AddRepertoireAsync_WithUnauthorizedStudent_Throws()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.AddRepertoireAsync(ctx.Teacher,
                new AddRepertoireRequest(ctx.OtherStudentProfile.Id, "Titulo", null, "https://example.com", null)));
    }

    [Fact]
    public async Task AddRepertoireAsync_WithComposerAndInstrument_SavesFields()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Fur Elise", "Praticar mao esquerda.", null, null, "Beethoven", "Piano"));

        var item = await db.RepertoireItems.SingleAsync();
        Assert.Equal("Beethoven", item.ComposerName);
        Assert.Equal("Piano", item.InstrumentName);
    }

    // ── Detail ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDetailForTeacherAsync_WithAuthorizedItem_ReturnsDetail()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var added = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Titulo", "Notas", "https://example.com", null));

        var detail = await svc.GetDetailForTeacherAsync(ctx.Teacher, ctx.StudentProfile.Id, added.Id);

        Assert.NotNull(detail);
        Assert.Equal(added.Id, detail!.Id);
        Assert.Equal("Titulo", detail.Title);
    }

    [Fact]
    public async Task GetDetailForTeacherAsync_WithOtherTeacher_Throws()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var added = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Titulo", "Notas", null, null));

        // other teacher cannot access this student's repertoire
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.GetDetailForTeacherAsync(ctx.OtherTeacher, ctx.StudentProfile.Id, added.Id));
    }

    [Fact]
    public async Task GetDetailForTeacherAsync_WithOtherSchool_ReturnsNull()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var added = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Titulo", "Notas", null, null));

        // other teacher (other school) cannot see items scoped to school1 — returns null (IDOR protection via schoolId filter)
        var detail = await svc.GetDetailForTeacherAsync(ctx.OtherTeacher, ctx.OtherStudentProfile.Id, added.Id);
        Assert.Null(detail);
    }

    // ── Update ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateRepertoireAsync_TextOnly_UpdatesItem()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var added = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Original", "Notas originais", null, null));

        var updated = await svc.UpdateRepertoireAsync(ctx.Teacher,
            new UpdateRepertoireRequest(ctx.StudentProfile.Id, added.Id, "Atualizado", "Novas notas", null, null));

        Assert.Equal("Atualizado", updated.Title);
        Assert.Equal("Novas notas", updated.Notes);
    }

    [Fact]
    public async Task UpdateRepertoireAsync_LinkOnly_UpdatesItem()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var added = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Titulo", null, "https://old.com", null));

        var updated = await svc.UpdateRepertoireAsync(ctx.Teacher,
            new UpdateRepertoireRequest(ctx.StudentProfile.Id, added.Id, "Titulo", null, "https://new.com", null));

        Assert.Equal("https://new.com", updated.ReferenceUrl);
    }

    [Fact]
    public async Task UpdateRepertoireAsync_WithComposerAndInstrument_SavesFields()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var added = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Titulo", "Notas", null, null));

        await svc.UpdateRepertoireAsync(ctx.Teacher,
            new UpdateRepertoireRequest(ctx.StudentProfile.Id, added.Id, "Titulo", "Notas", null, null, "Mozart", "Violino"));

        var item = await db.RepertoireItems.SingleAsync();
        Assert.Equal("Mozart", item.ComposerName);
        Assert.Equal("Violino", item.InstrumentName);
    }

    [Fact]
    public async Task UpdateRepertoireAsync_EmptyWithoutExistingContent_Throws()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var added = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Titulo", null, "https://example.com", null));

        // Trying to clear the link without providing anything else and no audio
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.UpdateRepertoireAsync(ctx.Teacher,
                new UpdateRepertoireRequest(ctx.StudentProfile.Id, added.Id, "Titulo", null, null, null)));
    }

    [Fact]
    public async Task UpdateRepertoireAsync_WithUnauthorizedStudent_Throws()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var added = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Titulo", null, "https://example.com", null));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.UpdateRepertoireAsync(ctx.Teacher,
                new UpdateRepertoireRequest(ctx.OtherStudentProfile.Id, added.Id, "Titulo", null, "https://example.com", null)));
    }

    // ── Material ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddMaterialAsync_ValidPdf_AddsMaterial()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var added = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Titulo", null, "https://example.com", null));

        var content = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF magic bytes
        var material = await svc.AddMaterialAsync(ctx.Teacher, ctx.StudentProfile.Id, added.Id,
            "partitura.pdf", "application/pdf", content.Length,
            () => new MemoryStream(content));

        Assert.NotEqual(Guid.Empty, material.Id);
        Assert.Equal(RepertoireMaterialType.Pdf, material.MaterialType);
        Assert.Single(db.RepertoireItemMaterials);
    }

    [Fact]
    public async Task AddMaterialAsync_ExceedsMaxSize_Throws()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var added = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Titulo", null, "https://example.com", null));

        var bigLength = 6 * 1024 * 1024; // 6 MB > 5 MB limit

        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.AddMaterialAsync(ctx.Teacher, ctx.StudentProfile.Id, added.Id,
                "grande.pdf", "application/pdf", bigLength,
                () => new MemoryStream(new byte[bigLength])));
    }

    [Fact]
    public async Task AddMaterialAsync_DangerousExtension_Throws()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var added = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Titulo", null, "https://example.com", null));

        var content = new byte[100];

        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.AddMaterialAsync(ctx.Teacher, ctx.StudentProfile.Id, added.Id,
                "virus.exe", "application/octet-stream", content.Length,
                () => new MemoryStream(content)));
    }

    [Fact]
    public async Task AddMaterialAsync_DoubleExtension_Throws()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var added = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Titulo", null, "https://example.com", null));

        var content = new byte[100];

        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.AddMaterialAsync(ctx.Teacher, ctx.StudentProfile.Id, added.Id,
                "script.php.jpg", "image/jpeg", content.Length,
                () => new MemoryStream(content)));
    }

    [Fact]
    public async Task AddMaterialAsync_WithUnauthorizedStudent_Throws()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var added = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Titulo", null, "https://example.com", null));

        var content = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.AddMaterialAsync(ctx.Teacher, ctx.OtherStudentProfile.Id, added.Id,
                "partitura.pdf", "application/pdf", content.Length,
                () => new MemoryStream(content)));
    }

    // ── Remove material ───────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveMaterialAsync_ExistingMaterial_RemovesMaterial()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var item = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Titulo", "Notas", null, null));

        var content = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var material = await svc.AddMaterialAsync(ctx.Teacher, ctx.StudentProfile.Id, item.Id,
            "partitura.pdf", "application/pdf", content.Length,
            () => new MemoryStream(content));

        // Item has Notes, so removing the material is allowed
        await svc.RemoveMaterialAsync(ctx.Teacher, ctx.StudentProfile.Id, material.Id);

        Assert.Empty(db.RepertoireItemMaterials);
    }

    [Fact]
    public async Task RemoveMaterialAsync_WouldLeaveEmpty_Throws()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        // Item with only a material (no URL, no notes, no audio)
        // First create with a link, then remove the link via update is complex — instead
        // seed directly with an item that only has a material
        var rawItem = new RepertoireItem(ctx.SchoolId, ctx.TeacherProfile.Id, ctx.StudentProfile.Id, "Titulo", DateTime.UtcNow);
        db.RepertoireItems.Add(rawItem);
        await db.SaveChangesAsync();

        var content = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var material = await svc.AddMaterialAsync(ctx.Teacher, ctx.StudentProfile.Id, rawItem.Id,
            "partitura.pdf", "application/pdf", content.Length,
            () => new MemoryStream(content));

        // Now the only content is this material — removing it should throw
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.RemoveMaterialAsync(ctx.Teacher, ctx.StudentProfile.Id, material.Id));
    }

    [Fact]
    public async Task RemoveMaterialAsync_MaterialFromAnotherRepertoire_Throws()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var item1 = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Item 1", "Notas", null, null));
        var item2 = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Item 2", "Notas", null, null));

        var content = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var material = await svc.AddMaterialAsync(ctx.Teacher, ctx.StudentProfile.Id, item1.Id,
            "partitura.pdf", "application/pdf", content.Length,
            () => new MemoryStream(content));

        // OtherTeacher (otherSchool) tries to remove material from school1 — IDOR blocked:
        // material not found in their schoolId scope → KeyNotFoundException
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            svc.RemoveMaterialAsync(ctx.OtherTeacher, ctx.StudentProfile.Id, material.Id));
    }

    // ── Delete ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteRepertoireAsync_AuthorizedItem_RemovesFromDb()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var added = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Titulo", null, "https://example.com", null));

        await svc.DeleteRepertoireAsync(ctx.Teacher, ctx.StudentProfile.Id, added.Id);

        Assert.Empty(db.RepertoireItems);
    }

    [Fact]
    public async Task DeleteRepertoireAsync_AlreadyDeleted_Throws()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var added = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Titulo", null, "https://example.com", null));

        await svc.DeleteRepertoireAsync(ctx.Teacher, ctx.StudentProfile.Id, added.Id);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            svc.DeleteRepertoireAsync(ctx.Teacher, ctx.StudentProfile.Id, added.Id));
    }

    [Fact]
    public async Task DeleteRepertoireAsync_WithUnauthorizedUser_Throws()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var added = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Titulo", null, "https://example.com", null));

        // Item belongs to school1; other teacher is in otherSchool — IDOR blocked: item not found in their school scope
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            svc.DeleteRepertoireAsync(ctx.OtherTeacher, ctx.OtherStudentProfile.Id, added.Id));
    }

    [Fact]
    public async Task DeleteRepertoireAsync_RemovesMaterialsAndItem()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var added = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Titulo", "Notas", null, null));

        var content = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        await svc.AddMaterialAsync(ctx.Teacher, ctx.StudentProfile.Id, added.Id,
            "partitura.pdf", "application/pdf", content.Length,
            () => new MemoryStream(content));

        await svc.DeleteRepertoireAsync(ctx.Teacher, ctx.StudentProfile.Id, added.Id);

        Assert.Empty(db.RepertoireItemMaterials);
        Assert.Empty(db.RepertoireItems);
    }

    // ── List excludes deleted ─────────────────────────────────────────────────

    [Fact]
    public async Task ListForTeacherStudentAsync_ExcludesDeletedItems()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var active = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Ativo", "Notas", null, null));
        var toDelete = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Excluido", null, "https://example.com", null));

        await svc.DeleteRepertoireAsync(ctx.Teacher, ctx.StudentProfile.Id, toDelete.Id);

        var list = await svc.ListForTeacherStudentAsync(ctx.Teacher, ctx.StudentProfile.Id);

        Assert.Single(list);
        Assert.Equal(active.Id, list.First().Id);
    }

    // ── File size limits ─────────────────────────────────────────────────────

    [Fact]
    public async Task AddMaterialAsync_ExactlyMaxFileSize_Succeeds()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var added = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Titulo", null, "https://example.com", null));

        var maxBytes = 5 * 1024 * 1024; // exactly 5 MB
        var content = new byte[4];       // SaveMaterialAsync reads from stream; length param is checked

        var material = await svc.AddMaterialAsync(ctx.Teacher, ctx.StudentProfile.Id, added.Id,
            "partitura.pdf", "application/pdf", maxBytes,
            () => new MemoryStream(content));

        Assert.NotEqual(Guid.Empty, material.Id);
    }

    [Fact]
    public async Task AddMaterialAsync_ExactlyMaxTotalSize_Succeeds()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var added = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Titulo", null, "https://example.com", null));

        // Add first file of 5 MB (max per file)
        var firstContent = new byte[4];
        await svc.AddMaterialAsync(ctx.Teacher, ctx.StudentProfile.Id, added.Id,
            "a.pdf", "application/pdf", 5 * 1024 * 1024,
            () => new MemoryStream(firstContent));

        // Add second file bringing total to exactly 10 MB
        var material = await svc.AddMaterialAsync(ctx.Teacher, ctx.StudentProfile.Id, added.Id,
            "b.pdf", "application/pdf", 5 * 1024 * 1024,
            () => new MemoryStream(firstContent));

        Assert.NotEqual(Guid.Empty, material.Id);
        Assert.Equal(2, db.RepertoireItemMaterials.Count());
    }

    [Fact]
    public async Task AddMaterialAsync_ExceedsTotalSize_Throws()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var added = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Titulo", null, "https://example.com", null));

        var content = new byte[4];

        // Add two files totalling 10 MB
        await svc.AddMaterialAsync(ctx.Teacher, ctx.StudentProfile.Id, added.Id,
            "a.pdf", "application/pdf", 5 * 1024 * 1024,
            () => new MemoryStream(content));
        await svc.AddMaterialAsync(ctx.Teacher, ctx.StudentProfile.Id, added.Id,
            "b.pdf", "application/pdf", 5 * 1024 * 1024,
            () => new MemoryStream(content));

        // Third file would push total to 10 MB + 1 byte
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.AddMaterialAsync(ctx.Teacher, ctx.StudentProfile.Id, added.Id,
                "c.pdf", "application/pdf", 1,
                () => new MemoryStream(content)));
    }

    [Fact]
    public async Task AddMaterialAsync_ExistingFilesCountedInTotal_Throws()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var added = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Titulo", null, "https://example.com", null));

        var content = new byte[4];

        // Seed existing material directly (bypassing service, simulating pre-existing file)
        var existingMaterial = new Domain.Learning.RepertoireItemMaterial(
            added.Id, ctx.SchoolId, Domain.Learning.Enums.RepertoireMaterialType.Pdf,
            "existing.pdf", 1)
        {
            SizeBytes = 9 * 1024 * 1024  // 9 MB existing
        };
        db.RepertoireItemMaterials.Add(existingMaterial);
        await db.SaveChangesAsync();

        // New file of 2 MB would push total to 11 MB
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.AddMaterialAsync(ctx.Teacher, ctx.StudentProfile.Id, added.Id,
                "novo.pdf", "application/pdf", 2 * 1024 * 1024,
                () => new MemoryStream(content)));
    }

    [Fact]
    public async Task AddMaterialAsync_ValidTxtFile_AddsMaterial()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var added = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Titulo", null, "https://example.com", null));

        var content = System.Text.Encoding.UTF8.GetBytes("Letra da musica aqui.");
        var material = await svc.AddMaterialAsync(ctx.Teacher, ctx.StudentProfile.Id, added.Id,
            "letra.txt", "text/plain", content.Length,
            () => new MemoryStream(content));

        Assert.NotEqual(Guid.Empty, material.Id);
        Assert.Equal(RepertoireMaterialType.Pdf, material.MaterialType);
    }

    // ── Overposting protection ────────────────────────────────────────────────

    [Fact]
    public async Task AddMaterialAsync_DisallowedMimeType_Throws()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var svc = CreateService(db);

        var added = await svc.AddRepertoireAsync(ctx.Teacher,
            new AddRepertoireRequest(ctx.StudentProfile.Id, "Titulo", null, "https://example.com", null));

        var content = new byte[100];

        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.AddMaterialAsync(ctx.Teacher, ctx.StudentProfile.Id, added.Id,
                "file.pdf", "application/x-msdownload", content.Length,
                () => new MemoryStream(content)));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static YourRhythmDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YourRhythmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YourRhythmDbContext(options);
    }

    private static RepertoireService CreateService(YourRhythmDbContext db)
    {
        var env = new FakeWebHostEnvironment();
        return new RepertoireService(db, env);
    }

    private static async Task<TestContext> SeedAsync(YourRhythmDbContext db)
    {
        var school = new School { Name = "YourRhythm", Slug = Guid.NewGuid().ToString("N"), PrimaryEmail = "school@example.test" };
        var otherSchool = new School { Name = "Outra", Slug = Guid.NewGuid().ToString("N"), PrimaryEmail = "other@example.test" };

        var teacherUser = new SchoolUser { SchoolId = school.Id, DisplayName = "Professor", Email = "prof@test.com", Role = YourRhythmRoles.Teacher };
        var teacher = new TeacherProfile { SchoolId = school.Id, SchoolUserId = teacherUser.Id, InstrumentFocus = "Piano" };
        var studentUser = new SchoolUser { SchoolId = school.Id, DisplayName = "Aluno", Email = "aluno@test.com", Role = YourRhythmRoles.Student };
        var student = new StudentProfile { SchoolId = school.Id, SchoolUserId = studentUser.Id, Instrument = "Piano", Level = "1" };
        var link = new TeacherStudent(school.Id, teacher.Id, student.Id, DateTime.UtcNow);

        var otherTeacherUser = new SchoolUser { SchoolId = otherSchool.Id, DisplayName = "Outro Prof", Email = "other-prof@test.com", Role = YourRhythmRoles.Teacher };
        var otherTeacher = new TeacherProfile { SchoolId = otherSchool.Id, SchoolUserId = otherTeacherUser.Id, InstrumentFocus = "Violao" };
        var otherStudentUser = new SchoolUser { SchoolId = otherSchool.Id, DisplayName = "Outro Aluno", Email = "other-aluno@test.com", Role = YourRhythmRoles.Student };
        var otherStudent = new StudentProfile { SchoolId = otherSchool.Id, SchoolUserId = otherStudentUser.Id, Instrument = "Violao", Level = "1" };
        var otherLink = new TeacherStudent(otherSchool.Id, otherTeacher.Id, otherStudent.Id, DateTime.UtcNow);

        db.AddRange(school, otherSchool, teacherUser, teacher, studentUser, student, link,
            otherTeacherUser, otherTeacher, otherStudentUser, otherStudent, otherLink);
        await db.SaveChangesAsync();

        var teacherProfile = new AuthenticatedUserProfile(Guid.NewGuid(), teacherUser.Email, teacherUser.DisplayName,
            YourRhythmRoles.Teacher, school.Id, teacherUser.Id, teacher.Id, null);

        var otherTeacherProfile = new AuthenticatedUserProfile(Guid.NewGuid(), otherTeacherUser.Email, otherTeacherUser.DisplayName,
            YourRhythmRoles.Teacher, otherSchool.Id, otherTeacherUser.Id, otherTeacher.Id, null);

        return new TestContext(teacherProfile, school.Id, teacher, student, otherTeacherProfile, otherTeacher, otherStudent);
    }

    private sealed record TestContext(
        AuthenticatedUserProfile Teacher,
        Guid SchoolId,
        TeacherProfile TeacherProfile,
        StudentProfile StudentProfile,
        AuthenticatedUserProfile OtherTeacher,
        TeacherProfile OtherTeacherProfile,
        StudentProfile OtherStudentProfile);

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "Test";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Test";
    }
}
