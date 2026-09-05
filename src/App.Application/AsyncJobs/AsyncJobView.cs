using App.Domain;

namespace App.Application;

public sealed record AsyncJobView(Guid Id, JobStatus Status, string? Error, Guid? OutputId);
