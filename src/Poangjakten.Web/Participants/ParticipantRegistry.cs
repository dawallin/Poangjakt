using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Poangjakten.Web.Participants;

public sealed partial class ParticipantRegistry(
    IParticipantRepository repository,
    ILogger<ParticipantRegistry> logger) : IHostedService
{
    private readonly ConcurrentDictionary<string, Participant> _participants = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _idsByCode = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _mutationLock = new(1, 1);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var participants = await repository.LoadAllAsync(cancellationToken);
        foreach (var participant in participants)
        {
            _participants[participant.Id] = participant;
            if (!string.IsNullOrWhiteSpace(participant.LoginCode) &&
                !_idsByCode.TryAdd(participant.LoginCode, participant.Id))
            {
                logger.LogWarning("Participant {ParticipantId} has a duplicate login code and cannot log in", participant.Id);
            }
        }

        logger.LogInformation("Loaded {ParticipantCount} participants into memory", participants.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Participant? Find(string id) => _participants.GetValueOrDefault(id);

    public Participant? FindByCode(string? requestedCode)
    {
        var code = NormalizeCode(requestedCode);
        return code is not null &&
               _idsByCode.TryGetValue(code, out var id) &&
               _participants.TryGetValue(id, out var participant)
            ? participant
            : null;
    }

    public IReadOnlyList<Participant> List() => _participants.Values
        .OrderBy(participant => PartyTables.Find(participant.TableId)?.Number ?? int.MaxValue)
        .ThenBy(participant => participant.DisplayName, StringComparer.CurrentCultureIgnoreCase)
        .ToArray();

    public async Task<ParticipantMutationResult> CreateAsync(
        string? requestedName,
        string? requestedCode,
        string? requestedClue,
        string? requestedTableId,
        CancellationToken cancellationToken)
    {
        var validation = Validate(requestedName, requestedCode, requestedClue, requestedTableId);
        if (validation.Error is not null) return ParticipantMutationResult.Invalid(validation.Error);

        await _mutationLock.WaitAsync(cancellationToken);
        try
        {
            if (_idsByCode.ContainsKey(validation.Code!))
            {
                return ParticipantMutationResult.Conflict("Koden används redan av en annan deltagare.");
            }

            var now = DateTimeOffset.UtcNow;
            var participant = new Participant(
                Guid.NewGuid().ToString("N"),
                validation.Name!,
                validation.Code!,
                validation.Clue!,
                validation.TableId!,
                0,
                now,
                now);

            await repository.SaveAsync(participant, cancellationToken);
            _participants[participant.Id] = participant;
            _idsByCode[participant.LoginCode] = participant.Id;
            logger.LogInformation("Created participant {ParticipantId}", participant.Id);
            return ParticipantMutationResult.Success(participant);
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    public async Task<ParticipantMutationResult> UpdateAsync(
        string id,
        string? requestedName,
        string? requestedCode,
        string? requestedClue,
        string? requestedTableId,
        CancellationToken cancellationToken)
    {
        var validation = Validate(requestedName, requestedCode, requestedClue, requestedTableId);
        if (validation.Error is not null) return ParticipantMutationResult.Invalid(validation.Error);

        await _mutationLock.WaitAsync(cancellationToken);
        try
        {
            if (!_participants.TryGetValue(id, out var existing))
            {
                return ParticipantMutationResult.NotFound();
            }

            if (_idsByCode.TryGetValue(validation.Code!, out var codeOwnerId) && codeOwnerId != id)
            {
                return ParticipantMutationResult.Conflict("Koden används redan av en annan deltagare.");
            }

            var participant = existing with
            {
                DisplayName = validation.Name!,
                LoginCode = validation.Code!,
                Clue = validation.Clue!,
                TableId = validation.TableId!,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await repository.SaveAsync(participant, cancellationToken);
            _participants[id] = participant;
            if (!string.Equals(existing.LoginCode, participant.LoginCode, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(existing.LoginCode)) _idsByCode.TryRemove(existing.LoginCode, out _);
                _idsByCode[participant.LoginCode] = id;
            }

            logger.LogInformation("Updated participant {ParticipantId}", id);
            return ParticipantMutationResult.Success(participant);
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    public async Task<bool> RemoveAsync(string id, CancellationToken cancellationToken)
    {
        await _mutationLock.WaitAsync(cancellationToken);
        try
        {
            if (!_participants.TryGetValue(id, out var participant)) return false;

            await repository.DeleteAsync(id, cancellationToken);
            _participants.TryRemove(id, out _);
            if (!string.IsNullOrWhiteSpace(participant.LoginCode)) _idsByCode.TryRemove(participant.LoginCode, out _);
            logger.LogInformation("Removed participant {ParticipantId}", id);
            return true;
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    private static ParticipantValidation Validate(
        string? requestedName,
        string? requestedCode,
        string? requestedClue,
        string? requestedTableId)
    {
        var name = NormalizeText(requestedName);
        if (name.Length is < 2 or > 80)
            return ParticipantValidation.Invalid("Namnet måste vara 2–80 tecken.");

        var code = NormalizeCode(requestedCode);
        if (code is null)
            return ParticipantValidation.Invalid("Koden måste vara 3–20 tecken och bara innehålla bokstäver, siffror eller bindestreck.");

        var clue = NormalizeText(requestedClue);
        if (clue.Length is < 2 or > 240)
            return ParticipantValidation.Invalid("Ledtråden måste vara 2–240 tecken.");

        var tableId = requestedTableId?.Trim();
        if (PartyTables.Find(tableId) is null)
            return ParticipantValidation.Invalid("Välj ett av de nio fördefinierade borden.");

        return new(name, code, clue, tableId!, null);
    }

    private static string NormalizeText(string? value) => Whitespace().Replace(value?.Trim() ?? "", " ");

    private static string? NormalizeCode(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? "";
        return normalized.Length is >= 3 and <= 20 && CodeCharacters().IsMatch(normalized)
            ? normalized
            : null;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"^[A-Z0-9-]+$")]
    private static partial Regex CodeCharacters();
}

internal sealed record ParticipantValidation(string? Name, string? Code, string? Clue, string? TableId, string? Error)
{
    public static ParticipantValidation Invalid(string error) => new(null, null, null, null, error);
}

public sealed record ParticipantMutationResult(
    Participant? Participant,
    string? Error,
    bool WasConflict,
    bool WasNotFound)
{
    public static ParticipantMutationResult Success(Participant participant) => new(participant, null, false, false);
    public static ParticipantMutationResult Invalid(string error) => new(null, error, false, false);
    public static ParticipantMutationResult Conflict(string error) => new(null, error, true, false);
    public static ParticipantMutationResult NotFound() => new(null, null, false, true);
}
