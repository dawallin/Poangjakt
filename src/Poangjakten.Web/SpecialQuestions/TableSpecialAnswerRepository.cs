using Azure.Data.Tables;
using Poangjakten.Web.Storage;

namespace Poangjakten.Web.SpecialQuestions;

public sealed class TableSpecialAnswerRepository(AzureStorageClients storage) : ISpecialAnswerRepository
{
    public async Task<IReadOnlyList<SpecialAnswer>> LoadAllAsync(CancellationToken cancellationToken)
    {
        var table = storage.SpecialAnswersTable();
        await table.CreateIfNotExistsAsync(cancellationToken);

        var answers = new List<SpecialAnswer>();
        await foreach (var entity in table.QueryAsync<TableEntity>(cancellationToken: cancellationToken))
        {
            if (SpecialQuestionDefinitions.Find(entity.PartitionKey) is not null)
            {
                answers.Add(ToAnswer(entity));
            }
        }

        return answers;
    }

    public async Task SaveAsync(SpecialAnswer answer, CancellationToken cancellationToken)
    {
        var entity = new TableEntity(answer.QuestionId, answer.ParticipantId)
        {
            ["Value"] = answer.Value,
            ["UpdatedAt"] = answer.UpdatedAt
        };
        await storage.SpecialAnswersTable()
            .UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
    }

    public async Task DeleteAsync(
        string participantId,
        string questionId,
        CancellationToken cancellationToken)
    {
        await storage.SpecialAnswersTable()
            .DeleteEntityAsync(questionId, participantId, cancellationToken: cancellationToken);
    }

    private static SpecialAnswer ToAnswer(TableEntity entity) => new(
        entity.RowKey,
        entity.PartitionKey,
        entity.GetInt32("Value") ?? 0,
        entity.GetDateTimeOffset("UpdatedAt") ?? entity.Timestamp ?? DateTimeOffset.UtcNow);
}
